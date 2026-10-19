using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

var dependency = new SimulatedCatalogService();
var retryDelay = TimeSpan.FromMilliseconds(200);
var breakDuration = TimeSpan.FromSeconds(2);

var pipeline = new ResiliencePipelineBuilder<string>()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<string>
    {
        ShouldHandle = new PredicateBuilder<string>().Handle<HttpRequestException>(),
        FailureRatio = 1.0,
        MinimumThroughput = 2,
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = breakDuration,
        OnOpened = args =>
        {
            Console.WriteLine($"Circuit opened for {args.BreakDuration.TotalSeconds:0.#} seconds.");
            return default;
        },
        OnHalfOpened = _ =>
        {
            Console.WriteLine("Circuit half-open; allowing a probe request.");
            return default;
        },
        OnClosed = _ =>
        {
            Console.WriteLine("Circuit closed; requests are flowing again.");
            return default;
        }
    })
    .AddRetry(new RetryStrategyOptions<string>
    {
        ShouldHandle = new PredicateBuilder<string>().Handle<HttpRequestException>(),
        MaxRetryAttempts = 3,
        Delay = retryDelay,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = false,
        OnRetry = args =>
        {
            Console.WriteLine(
                $"Retry {args.AttemptNumber}/{3} in {args.RetryDelay.TotalMilliseconds:0} ms.");
            return default;
        }
    })
    .Build();

Console.WriteLine("Catalog dependency is unavailable.");
await RequestCatalogAsync("first");
await RequestCatalogAsync("second");
await RequestCatalogAsync("while circuit is open");

Console.WriteLine("\nWaiting for the circuit break to expire...");
await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(100));
dependency.IsAvailable = true;
await RequestCatalogAsync("recovery probe");

async Task RequestCatalogAsync(string requestName)
{
    Console.WriteLine($"\nRequest: {requestName}");

    try
    {
        var product = await pipeline.ExecuteAsync(
            token => dependency.GetProductAsync(token),
            CancellationToken.None);
        Console.WriteLine($"Service response: {product}");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Fallback response: serving the cached catalog.");
    }
    catch (BrokenCircuitException)
    {
        Console.WriteLine("Fallback response: circuit is open; serving the cached catalog.");
    }
}

sealed class SimulatedCatalogService
{
    public bool IsAvailable { get; set; }

    public async ValueTask<string> GetProductAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        if (!IsAvailable)
        {
            throw new HttpRequestException("The catalog service is unavailable.");
        }

        return "Catalog response from the recovered service.";
    }
}