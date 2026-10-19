# gRPC Services with a BFF Gateway

This example has separate Catalog and Inventory gRPC microservices, plus a Backend for Frontend (BFF). The BFF is tailored for a mobile product-detail screen: it calls both services concurrently and returns only the combined fields that screen needs.

Unlike the API gateway in example 02, the BFF presents a client-specific contract rather than exposing one gateway route per backend service. This lets the mobile client make one request and keeps backend composition out of the client.

## Run

Start each process in a separate terminal from the repository root:

```sh
dotnet run --project 03-bff-gateway-grpc/CatalogService
```

```sh
dotnet run --project 03-bff-gateway-grpc/InventoryService
```

```sh
dotnet run --project 03-bff-gateway-grpc/BffGateway
```

The Catalog service listens on port `5201`, Inventory on `5202`, and the BFF on `5200`.

## Try it

```sh
curl http://localhost:5200/mobile/products/keyboard-1/summary
```

The response combines product details and stock information into one mobile-focused payload. The services are independent processes; the gateway communicates with them over gRPC and HTTP/2.