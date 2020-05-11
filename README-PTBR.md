[![Build status](https://tiagor87.visualstudio.com/OpenSource/_apis/build/status/Seedwork.Cqrs.Bus)](https://tiagor87.visualstudio.com/OpenSource/_build/latest?definitionId=9)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=tiagor87_Seedwork.CQRS.Bus&metric=coverage)](https://sonarcloud.io/dashboard?id=tiagor87_Seedwork.CQRS.Bus)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tiagor87_Seedwork.CQRS.Bus&metric=alert_status)](https://sonarcloud.io/dashboard?id=tiagor87_Seedwork.CQRS.Bus)
[![NuGet](https://buildstats.info/nuget/Seedwork.CQRS.Bus.Core)](http://www.nuget.org/packages/Seedwork.CQRS.Bus.Core)

 # Seedwork.CQRS.Bus [[EN](README.md)]/[BR]

__Seedwork.CQRS.Bus__ é um projeto para facilitar a utilização do RabbitMQ no uso e controle de fluxos básicos.

## Como usar?

```csharp
services
    .AddBusCore(
        options =>
        {
            options
                .SetOptions("BusConnectionOptions")
                .SetConnectionString("amqp://guest:guest@localhost/")
                .SetSerializer<BusSerializer>();
        });
```

ou

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

## Publicações

Toda a publicação ocorre em lote, portanto, você terá que esperar até que **PublisherBufferSize** seja alcançado ou **PublisherBufferTtlInMilliseconds** acabe.

### Como publicar uma mensagem simples?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
string message = "Message";

await _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);
```

### Como publicar uma lista de mensagens?

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

## Como processar uma mensagem?

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

### O que acontece quando ocorre um erro no processamento da mensagem?

* Quando ocorre uma falha no processamento da mensagem, a aplicação re-enfileira a mensagem na fila de retentativa com um tempo de atraso, para enviá-la a fila principal ao fim deste tempo;
* Quando o número máximo de tentativas de processamento é atingida, a aplicação move a mensagem para a fila de falha;
* Quando o sistema falha ao re-enfileirar, é realizado _Nack_ da mensagem.

#### Como posso configurar o número máximo de tentativas?

```c#
var exchange = Exchange.Create("exchange", ExchangeType.Direct);
var queue = Queue.Create($"exchange.queue-created");
var routingKey = RoutingKey.Create(queue.Name.Value);
var message = Message.Create(
    "Message", // data
    10);       // max attempts

await _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);
```

* Quando não definido o número máximo de tentantivas, um valor __padrão__ será definido.
* Quando se defino __zero__ para o número máximo de tentantivas, a mensagem será retentada eternamente.

## O que eu posso configurar?

* **PublisherBufferSize** (default: 1000): Limite de mensagens para publicar no lote;
* **PublisherBufferTtlInMilliseconds** (default: 5000): Tempo limite para publicação do lote de mensagens (quando o limite de quantidade não for atingido); 
* **ConnectionMaxRetry** (default: 10): Máximo de tentativas de conexão com a fila antes de falhar; 
* **ConnectionRetryDelayInMilliseconds** (default: 500): Delay entre as tentantivas de conexão;
* **ConsumerMaxParallelTasks** (default: 500): Limite de threads em paralelo; 
* **MessageMaxRetry** (default: 5): Máximo de tentativas de processamento de uma mensagem; 
* **PublishMaxRetry** (default: 5): Máximo de tentativas de publicação de uma mensagem;
* **PublishRetryDelayInMilliseconds** (default: 100): Tempo entre as tentativas de publicação da mensagem.

## Eventos

* **PublishSuccessed**: Quando a publicação for bem-sucedida, o sistema enviará esse evento com as mensagens que obtiveram sucesso.

```c#
Connection.PublishSuccessed += items => 
{
    ...
};
```

* **PublishFailed**: 103/5000Quando a publicação falha após todas as tentativas, o sistema despacha esse evento com as mensagens e exceção.

```c#
Connection.PublishFailed += (items, exception) => 
{
    ...
};
```
