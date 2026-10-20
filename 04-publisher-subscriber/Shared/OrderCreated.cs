namespace PubSub;

public sealed record OrderCreated(Guid OrderId, string ProductId, int Quantity,
    string CustomerEmail, DateTimeOffset CreatedAt);
