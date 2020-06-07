[![Build status](https://tiagor87.visualstudio.com/OpenSource/_apis/build/status/Seedwork.Cqrs.Bus)](https://tiagor87.visualstudio.com/OpenSource/_build/latest?definitionId=9)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=tiagor87_Seedwork.CQRS.Bus&metric=coverage)](https://sonarcloud.io/dashboard?id=tiagor87_Seedwork.CQRS.Bus)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tiagor87_Seedwork.CQRS.Bus&metric=alert_status)](https://sonarcloud.io/dashboard?id=tiagor87_Seedwork.CQRS.Bus)
[![NuGet](https://buildstats.info/nuget/Seedwork.CQRS.Bus.Core)](http://www.nuget.org/packages/Seedwork.CQRS.Bus.Core)

# Seedwork.CQRS.Bus [EN]/[[BR](README-PTBR.md)]

__Seedwork.CQRS.Bus__ is a project to make RabbitMQ easier to use and control some basic flows.

## How to use?

Register __BusConnection__ as __singleton__ in your project.

__BusConnection__ requires:
- BusConnectionString: RabbitMQ connection string;
- IBusSerializer: The default serializer;
- IServiceScopeFactory: Scope factory for DI.

```csharp
services
    .AddBusCore(
        configuration,
        options =>
        {
            options
                .SetOptions("BusConnectionOptions")
                .SetConnectionString("amqp://guest:guest@localhost/")
                .SetSerializer<BusSerializer>();
        });
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

* When a message fails to be processed, the application will re-queue it to the retry-queue with a message expiration, to send it back to the main queue later;
* When the max attempts is achivied, the application will route it to failed-queue;
* When the system fails to re-queue, it will nack the message.

#### How long it takes to requeue message to the main queue?

There are two options:

* __ArithmeticProgressionRetryBehavior__: T[final] = T[initial] + (attempt - 1) * coeficient;
* __GeometricProgressionRetryBehavior__: T[final] = T[initial] * pow(coeficient, attempt - 1);


* You can set your own method too.

#### How can I set the retry behavior?

```c#
services
    .AddBusCore(
        configuration,
        options =>
        {
            options
                // Aritmethic progression 
                .UseArithmeticProgressionRetryBehavior(<coeficient>, <initialValue> = 1)
                // Geometric progression
                .UseGeometricProgressionRetryBehavior(<coeficient>, <initialValue>)
                // Custom behavior
                UseRetryBehabior(IRetryBehavior);
        });
```

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
* **PublisherBufferTtlInMilliseconds** (default: 5000): Publish messages when the limit is not achieved; 
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
