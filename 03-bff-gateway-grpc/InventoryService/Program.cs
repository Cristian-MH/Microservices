using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microservices.Contracts.Inventory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
    options.ListenLocalhost(5202, endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<InventoryGrpcService>();
app.Run();

sealed class InventoryGrpcService : Inventory.InventoryBase
{
    public override Task<Stock> GetStock(GetStockRequest request, ServerCallContext context)
    {
        var stock = new Stock
        {
            ProductId = request.ProductId,
            Quantity = 12,
            Available = true
        };

        return Task.FromResult(stock);
    }
}