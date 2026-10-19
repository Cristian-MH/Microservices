# Microservices and Architecture Examples

Small, runnable examples that explore common microservice and architecture patterns.

## Examples

| Example | Pattern | Description |
| --- | --- | --- |
| [01 - Retry and circuit breaker](01-retry-circuit-breaker/README.md) | Resilience | Retries transient failures with exponential backoff, opens a circuit after repeated failures, and uses a fallback while the dependency is unavailable. |

## Requirements

- .NET 10 SDK

## Run an example

```sh
dotnet run --project 01-retry-circuit-breaker
```

Each example has its own README with details and expected behavior.