using System.Collections.Concurrent;
using Prometheus;
using PubSub;

var builder = WebApplication.CreateBuilder(args);
var records = new ConcurrentDictionary<Guid, Reservation>();
builder.Services.AddSingleton<EventBus>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("InventoryService");
    return new EventBus(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<EventBus>>())
    {
        Queue = "inventory.orders",
        Handler = order =>
        {
            // RabbitMQ may redeliver after a lost acknowledgement; deduplicate within this process.
            if (records.TryAdd(order.OrderId, new Reservation(order.OrderId, order.ProductId, order.Quantity, DateTimeOffset.UtcNow)))
            {
                EventBus.Processed.Inc();
                logger.LogInformation("Reserved {Quantity} units of {ProductId} for order {OrderId}", order.Quantity, order.ProductId, order.OrderId);
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
app.MapGet("/reservations", () => records.Values);
app.MapGet("/reservations/{id:guid}", (Guid id) => records.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());
app.Run();

sealed record Reservation(Guid OrderId, string ProductId, int Quantity, DateTimeOffset ReservedAt);
