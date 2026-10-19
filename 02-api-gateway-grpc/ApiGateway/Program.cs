using Microservices.Contracts.Catalog;
using Microservices.Contracts.Inventory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(5100));

builder.Services.AddGrpcClient<Catalog.CatalogClient>(options =>
    options.Address = new Uri("http://localhost:5101"));
builder.Services.AddGrpcClient<Inventory.InventoryClient>(options =>
    options.Address = new Uri("http://localhost:5102"));

var app = builder.Build();

app.MapGet("/api/products/{id}", async (string id, Catalog.CatalogClient catalog) =>
{
    var product = await catalog.GetProductAsync(new GetProductRequest { ProductId = id });
    return Results.Ok(new
    {
        product.ProductId,
        product.Name,
        product.Description,
        product.Price
    });
});

app.MapGet("/api/inventory/{id}", async (string id, Inventory.InventoryClient inventory) =>
{
    var stock = await inventory.GetStockAsync(new GetStockRequest { ProductId = id });
    return Results.Ok(new { stock.ProductId, stock.Quantity, stock.Available });
});

app.Run();