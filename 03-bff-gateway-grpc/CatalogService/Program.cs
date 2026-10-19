using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microservices.Contracts.Catalog;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
    options.ListenLocalhost(5201, endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<CatalogGrpcService>();
app.Run();

sealed class CatalogGrpcService : Catalog.CatalogBase
{
    public override Task<Product> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var product = new Product
        {
            ProductId = request.ProductId,
            Name = "Mechanical Keyboard",
            Description = "Compact wireless keyboard",
            Price = 79.99
        };

        return Task.FromResult(product);
    }
}