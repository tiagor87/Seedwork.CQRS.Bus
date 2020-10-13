using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core.Consumers.Abstractions;
using Seedwork.CQRS.Bus.Core.Consumers.Options;
using Seedwork.CQRS.Bus.Core.RetryBehaviors;

namespace Seedwork.CQRS.Bus.Core.Consumers
{
    public class Consumer<T> : AsyncDefaultBasicConsumer, IConsumer
    {
        private bool _disposed;
        private readonly IBusConnection _connection;
        private readonly IBusLogger _logger;
        private readonly IRetryBehavior _retryBehavior;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConsumerOptions<T> _options;
        private readonly IModel _channel;
        private readonly Tasks _tasks;
        private readonly string _consumerTag;

        internal Consumer(BusConnection connection, IBusLogger logger, IRetryBehavior retryBehavior, IServiceScopeFactory scopeFactory, ConsumerOptions<T> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
            _retryBehavior = retryBehavior ?? throw new ArgumentNullException(nameof(retryBehavior));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _tasks = new Tasks(_options.ConsumerMaxParallelTasks);
            _channel = connection.ConsumerConnection.CreateModel();
            _channel.BasicQos(0, options.PrefetchCount, false);
            DeclareAndBind();
            _consumerTag = _channel.BasicConsume(_options.Queue.Name.Value, false, this);
        }

        ~Consumer()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _channel.BasicCancel(_consumerTag);
                _tasks.Dispose();
                _channel.Dispose();
            }

            _disposed = true;
        }

        public override Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            IBasicProperties properties, byte[] body)
        {
            var @event = new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            try
            {
                var task = new Task(() =>
                {
                    try
                    {
                        var message = Message<T>.Create(_channel, _options.Exchange, _options.Queue, RoutingKey.Create(routingKey), _options.Serializer, @event,
                            (OnDone, OnFail));
                        using var scope = _scopeFactory.CreateScope();
                        try
                        {
                            _options.OnNext(scope, message).GetAwaiter().GetResult();
                            if (_options.AutoAck) message.Complete();
                        }
                        catch (Exception exception)
                        {
                            message.Fail(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger?.WriteException(nameof(Consumer<T>), exception,
                            new KeyValuePair<string, object>("Event", Encoding.UTF8.GetString(@event.Body)));
                        var failedQueue = _options.Queue.CreateFailedQueue();
                        var failedRoutingKey = RoutingKey.Create(failedQueue.Name.Value);
                        _connection.Publish(Exchange.Default, failedQueue, failedRoutingKey,
                            ErrorMessage.Create(@event.Body, @event.BasicProperties));
                        _channel.BasicNack(@event.DeliveryTag, false, false);
                    }
                });
                _tasks.Add(task);
            }
            catch (Exception ex)
            {
                _logger?.WriteException(typeof(T).Name, ex,
                    new KeyValuePair<string, object>("args", @event));
                _channel.BasicNack(@event.DeliveryTag, false, true);
            }

            return Task.CompletedTask;
        }
        
        private void OnDone(Message<T> message) => message.Channel.BasicAck(message.DeliveryTag, false);

        private void OnFail(Exception exception, Message<T> message)
        {
            if (_retryBehavior.ShouldRetry(message.AttemptCount, message.MaxAttempts))
            {
                var retryQueue = message.Queue
                    .CreateRetryQueue(_retryBehavior.GetWaitTime(message.AttemptCount));
                var retryRoutingKey = RoutingKey.Create(retryQueue.Name.Value);
                _connection.Publish(Exchange.Default, retryQueue, retryRoutingKey, message);
            }
            else
            {
                var failedQueue = message.Queue
                    .CreateFailedQueue();
                var failedRoutingKey = RoutingKey.Create(failedQueue.Name.Value);
                _connection.Publish(Exchange.Default, failedQueue, failedRoutingKey, message);
            }

            message.Channel.BasicAck(message.DeliveryTag, false);

            _logger?.WriteException(typeof(T).Name, exception, new KeyValuePair<string, object>("Event", message));
        }

        private void DeclareAndBind()
        {
            _options.Exchange.Declare(_channel);
            _options.Queue.Declare(_channel);
            _options.Queue.Bind(_channel, _options.Exchange, _options.RoutingKeys);
        }
    }
}