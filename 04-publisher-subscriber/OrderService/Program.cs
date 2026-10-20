using System.Collections.Concurrent;
using System.Net.Mail;
using Prometheus;
using PubSub;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<EventBus>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventBus>());
var app = builder.Build();
var orders = new ConcurrentDictionary<Guid, OrderCreated>();
app.UseHttpMetrics();
app.MapMetrics();
app.MapGet("/health", (EventBus bus) => bus.IsReady ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.MapGet("/orders", () => orders.Values.OrderByDescending(o => o.CreatedAt));
app.MapGet("/orders/{id:guid}", (Guid id) => orders.TryGetValue(id, out var order) ? Results.Ok(order) : Results.NotFound());
app.MapPost("/orders", async (CreateOrder request, EventBus bus, ILogger<Program> logger, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.ProductId) || request.Quantity <= 0
        || !MailAddress.TryCreate(request.CustomerEmail, out _))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["order"] = ["Provide a productId, positive quantity, and valid customerEmail."]
        });
    var order = new OrderCreated(Guid.NewGuid(), request.ProductId.Trim(), request.Quantity,
        request.CustomerEmail!, DateTimeOffset.UtcNow);
    try { await bus.PublishAsync(order, ct); }
    catch (Exception ex)
    {
        logger.LogError(ex, "Could not confirm order {OrderId}", order.OrderId);
        return Results.Problem("Order publication could not be confirmed. Check RabbitMQ and logs.", statusCode: 503);
    }
    orders[order.OrderId] = order;
    logger.LogInformation("Published order {OrderId}", order.OrderId);
    return Results.Accepted($"/orders/{order.OrderId}", order);
});
app.Run();

sealed record CreateOrder(string? ProductId, int Quantity, string? CustomerEmail);
