using System.Collections.Concurrent;
using Prometheus;
using PubSub;

var builder = WebApplication.CreateBuilder(args);
var records = new ConcurrentDictionary<Guid, Notification>();
builder.Services.AddSingleton<EventBus>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationService");
    return new EventBus(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<EventBus>>())
    {
        Queue = "notifications.orders",
        Handler = order =>
        {
            // RabbitMQ may redeliver after a lost acknowledgement; deduplicate within this process.
            if (records.TryAdd(order.OrderId, new Notification(order.OrderId, order.CustomerEmail, $"Order {order.OrderId} received", DateTimeOffset.UtcNow)))
            {
                EventBus.Processed.Inc();
                logger.LogInformation("Recorded notification for order {OrderId}", order.OrderId);
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventBus>());
var app = builder.Build();
app.UseHttpMetrics();
app.MapMetrics();
app.MapGet("/health", (EventBus bus) => bus.IsReady ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.MapGet("/notifications", () => records.Values);
app.MapGet("/notifications/{id:guid}", (Guid id) => records.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());
app.Run();

sealed record Notification(Guid OrderId, string CustomerEmail, string Message, DateTimeOffset SentAt);
