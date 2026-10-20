using System.Text.Json;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PubSub;

// Linked source keeps this teaching example to three independently deployable services.
public sealed class EventBus(IConfiguration configuration, ILogger<EventBus> logger) : IHostedService, IAsyncDisposable
{
    private const string Exchange = "orders.created";
    private static readonly string[] Queues = ["inventory.orders", "notifications.orders"];
    private readonly SemaphoreSlim publishLock = new(1, 1);
    private IConnection? connection;
    private IChannel? publisher;
    private IChannel? subscriber;
    private bool consuming;
    public string? Queue { get; set; }
    public Func<OrderCreated, Task>? Handler { get; set; }
    public bool IsReady => connection?.IsOpen == true && publisher?.IsOpen == true
        && (Queue is null || (subscriber?.IsOpen == true && consuming));

    public static readonly Counter Published = Metrics.CreateCounter("orders_published_total", "Orders confirmed by RabbitMQ.");
    public static readonly Counter Processed = Metrics.CreateCounter("orders_processed_total", "Unique orders processed by this subscriber.");
    public static readonly Counter Failures = Metrics.CreateCounter("order_event_failures_total", "Publish or consumer failures.");
    public static readonly Histogram Duration = Metrics.CreateHistogram("order_processing_seconds", "Subscriber processing duration.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "demo",
            Password = configuration["RabbitMQ:Password"] ?? "demo-password",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
        // Initial connection failures are not handled by automatic recovery.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < 15 && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RabbitMQ unavailable; startup attempt {Attempt}/15", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        publisher = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await publisher.ExchangeDeclareAsync(Exchange, ExchangeType.Fanout, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        // Declare both subscriptions before accepting orders, even if a subscriber is offline.
        foreach (var queue in Queues)
        {
            await publisher.QueueDeclareAsync(queue + ".dead", durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);
            await publisher.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-dead-letter-exchange"] = "",
                    ["x-dead-letter-routing-key"] = queue + ".dead"
                }, cancellationToken: cancellationToken);
            await publisher.QueueBindAsync(queue, Exchange, "", cancellationToken: cancellationToken);
        }
        if (Queue is not null)
        {
            subscriber = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await subscriber.BasicQosAsync(0, 1, false, cancellationToken);
            var consumer = new AsyncEventingBasicConsumer(subscriber);
            consumer.RegisteredAsync += (_, _) => { consuming = true; return Task.CompletedTask; };
            consumer.UnregisteredAsync += (_, _) => { consuming = false; return Task.CompletedTask; };
            consumer.ReceivedAsync += async (_, delivery) =>
            {
                using var timer = Duration.NewTimer();
                try
                {
                    var message = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span)
                        ?? throw new JsonException("Empty event.");
                    if (message.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(message.ProductId)
                        || message.Quantity <= 0 || string.IsNullOrWhiteSpace(message.CustomerEmail))
                        throw new JsonException("Invalid OrderCreated event.");
                    await Handler!(message);
                }
                catch (Exception ex)
                {
                    Failures.Inc();
                    logger.LogError(ex, "Rejecting event {MessageId} to {Queue}.dead", delivery.BasicProperties.MessageId, Queue);
                    await subscriber.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
                // Acknowledge only after processing. A connection failure here permits redelivery.
                await subscriber.BasicAckAsync(delivery.DeliveryTag, multiple: false);
            };
            await subscriber.BasicConsumeAsync(Queue, autoAck: false, consumer, cancellationToken);
        }
        logger.LogInformation("RabbitMQ ready; subscription: {Queue}", Queue ?? "publisher only");
    }

    public async Task PublishAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await publishLock.WaitAsync(timeout.Token);
        try
        {
            if (!IsReady) throw new InvalidOperationException("RabbitMQ is not ready.");
            await publisher!.BasicPublishAsync(Exchange, "", mandatory: true,
                basicProperties: new BasicProperties
                {
                    Persistent = true, ContentType = "application/json", MessageId = message.OrderId.ToString(),
                    Type = nameof(OrderCreated)
                }, body: JsonSerializer.SerializeToUtf8Bytes(message), cancellationToken: timeout.Token);
            Published.Inc();
        }
        catch
        {
            Failures.Inc();
            throw;
        }
        finally { publishLock.Release(); }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        consuming = false;
        if (subscriber?.IsOpen == true) await subscriber.CloseAsync(cancellationToken: cancellationToken);
        if (publisher?.IsOpen == true) await publisher.CloseAsync(cancellationToken: cancellationToken);
        if (connection?.IsOpen == true) await connection.CloseAsync(cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (subscriber is not null) await subscriber.DisposeAsync();
        if (publisher is not null) await publisher.DisposeAsync();
        if (connection is not null) await connection.DisposeAsync();
        publishLock.Dispose();
    }
}
