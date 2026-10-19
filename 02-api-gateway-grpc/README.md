# gRPC Services with an API Gateway

This example has two independent microservices: Catalog and Inventory. Each exposes a gRPC contract and listens for HTTP/2 on its own local port. The API Gateway accepts ordinary HTTP requests and routes them to the corresponding service through generated gRPC clients.

## Run

Start each process in a separate terminal from the repository root:

```sh
dotnet run --project 02-api-gateway-grpc/CatalogService
```

```sh
dotnet run --project 02-api-gateway-grpc/InventoryService
```

```sh
dotnet run --project 02-api-gateway-grpc/ApiGateway
```

The Catalog service listens on port `5101`, Inventory on `5102`, and the HTTP gateway on `5100`.

## Try it

```sh
curl http://localhost:5100/api/products/keyboard-1
curl http://localhost:5100/api/inventory/keyboard-1
```

The gateway exposes separate HTTP routes for each backend capability. The browser or caller does not need to know about the internal gRPC endpoints.