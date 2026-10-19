# Microservices and Architecture Examples

Small, runnable examples that explore common microservice and architecture patterns.

## Examples

| Example | Pattern | Description |
| --- | --- | --- |
| [01 - Retry and circuit breaker](01-retry-circuit-breaker/README.md) | Resilience | Retries transient failures with exponential backoff, opens a circuit after repeated failures, and uses a fallback while the dependency is unavailable. |
| [02 - gRPC with API gateway](02-api-gateway-grpc/README.md) | API gateway | Routes public HTTP requests to separate Catalog and Inventory services over gRPC. |
| [03 - gRPC with BFF gateway](03-bff-gateway-grpc/README.md) | Backend for frontend | Aggregates Catalog and Inventory gRPC responses into a frontend-specific product summary. |

## Requirements

- .NET 10 SDK

## Run an example

Each example has its own README with run commands, endpoints, and expected behavior. The gRPC examples require three terminal sessions to run the two services and the gateway concurrently.

## Debug

Open Run and Debug in VS Code to select an individual service or gateway. To start a complete example together, choose `Example 02 - API Gateway (all services)` or `Example 03 - BFF (all services)`. Each launch configuration builds its project before starting the debugger.