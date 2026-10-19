# Retry and Circuit Breaker

This example simulates a catalog microservice that is initially unavailable. Polly retries each failed call with exponential backoff, then the circuit breaker opens after two calls have exhausted their retries. While the circuit is open, requests skip the dependency and use a cached fallback. After the break duration, a half-open probe reaches the recovered service and closes the circuit.

The circuit breaker is added before the retry strategy so it observes the outcome of each complete retry sequence, rather than counting every individual retry as a separate request.

## Run

From the repository root:

```sh
dotnet run --project examples/01-retry-circuit-breaker
```

The output shows retry attempts and delays, circuit state changes, fallback responses, and the successful recovery probe. The service is simulated in-process, so no external services are required.