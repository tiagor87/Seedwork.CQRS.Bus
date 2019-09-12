[![Build status](https://ci.appveyor.com/api/projects/status/x0jxj81p2demnsw6/branch/master?svg=true)](https://ci.appveyor.com/project/tiagor87/seedwork-cqrs-bus/branch/master)
[![Coverage Status](https://coveralls.io/repos/github/tiagor87/Seedwork.CQRS.Bus/badge.svg)](https://coveralls.io/github/tiagor87/Seedwork.CQRS.Bus)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tiagor87_Seedwork.CQRS.Bus&metric=alert_status)](https://sonarcloud.io/dashboard?id=tiagor87_Seedwork.CQRS.Bus)
[![NuGet](https://buildstats.info/nuget/Seedwork.CQRS.Bus.Core)](http://www.nuget.org/packages/Seedwork.CQRS.Bus.Core)

# Seedwork.CQRS.Bus

__Seedwork.CQRS.Bus__ is a project to make RabbitMQ easier to use and control some basic flows.

## How to use?

Register __BusConnection__ as __singleton__ in your project.

__BusConnection__ requires:
- BusConnectionString: RabbitMQ connection string;
- IBusSerializer: Serializador padrão;
- IServiceScopeFactory: Fábrica de escopo para injeção de dependência.

```c#
services
    .AddSingleton(BusConnectionString.Create("amqp://guest:guest@localhost/"))
    .AddSingleton<IBusSerializer, BusSerializer>()
    .AddSingleton<BusConnection>()
    .Configure<BusConnectionOptions>(configuration.GetSection("BusConnectionOptions"))
```

## Publish

All the publish happens in batch, so you will have to wait until **PublisherBufferSize** achieved or **PublisherBufferTtlInMilliseconds** runs out.

### How to publish a simple message?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
string message = "Message";

await _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);
```

### How to publish a list of messages?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
string[] messages = new [] {
    "Message 1",
    ...
    "Message N"
};

await _connectionFixture.Connection.PublishBatch(exchange, queue, routingKey, messages);
```

## How to process a message?

```c#
_connectionFixture.Connection.Subscribe<string>(
    exchange,
    queue,
    routingKey,
    prefetchCount,
    async (scope, message) =>
    {
        var mediator = scope.GetService<IMediator>();
        var command = Command.Create(message);
        await mediator.SendAsync(command);
    });
```

### When fails to process a message, whats happens?

* When a message fails to be processed, the application will requeue it to the retry-queue with a message expiration, to send it back to the main queue later;
* When the max attempts is achivied, the application will route it to failed-queue;
* When system fails to requeue, it will nack the message.

#### How can I configure the max attempts?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
var message = Message.Create(
    "Message", // data
    10);       // max attempts

await _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);
```

* When publisher not set max attempts, a __default__ value will be set.
* When publisher set max attempts to __zero__, it will retry forever.

## What can I configure?

* **PublisherBufferSize** (default: 1000): Message's limit to publish at once;
* **PublisherBufferTtlInMilliseconds** (default: 5000): Publish messages when limit is not achieved; 
* **ConnectionMaxRetry** (default: 10): Max attempts to connect to bus before fails; 
* **ConnectionRetryDelayInMilliseconds** (default: 500): Delay between connection attempts;
* **ConsumerMaxParallelTasks** (default: 500): Thread's limit to process; 
* **MessageMaxRetry** (default: 5): Max attempts to process a message; 
* **PublishMaxRetry** (default: 5): Max attempts to publish messages;
* **PublishRetryDelayInMilliseconds** (default: 100): Delay between publish attempts.

## Events

* **PublishSuccessed**: When publish success, the system will dispatch this event with messages sent.

```c#
Connection.PublishSuccessed += items => 
{
    ...
};
```

* **PublishFailed**: When publish fails after all attempts, the system will dispatch this event with messages and exception.

```c#
Connection.PublishFailed += (items, exception) => 
{
    ...
};
```