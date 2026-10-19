using Microservices.Contracts.Catalog;
using Microservices.Contracts.Inventory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(5200));

builder.Services.AddGrpcClient<Catalog.CatalogClient>(options =>
    options.Address = new Uri("http://localhost:5201"));
builder.Services.AddGrpcClient<Inventory.InventoryClient>(options =>
    options.Address = new Uri("http://localhost:5202"));

var app = builder.Build();

app.MapGet("/mobile/products/{id}/summary", async (
    string id,
    Catalog.CatalogClient catalog,
    Inventory.InventoryClient inventory) =>
{
    var productTask = catalog.GetProductAsync(new GetProductRequest { ProductId = id }).ResponseAsync;
    var stockTask = inventory.GetStockAsync(new GetStockRequest { ProductId = id }).ResponseAsync;
    await Task.WhenAll(productTask, stockTask);

    var product = await productTask;
    var stock = await stockTask;

    return Results.Ok(new
    {
        product.ProductId,
        product.Name,
        product.Price,
        stock.Available,
        stock.Quantity
    });
});

app.Run();