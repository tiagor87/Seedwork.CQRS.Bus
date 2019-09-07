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
```

## How to public a simple message?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
string message = "Message";

await _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);
```

## How to public a list of messages?

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

## How to register a consumer?

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

* When a message fails to be processed, the application will requeue it to the retry-queue with a message expiration, to send it back to the main queue later.
* When the max attempts is achivied, the application will route it to failed-queue.

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