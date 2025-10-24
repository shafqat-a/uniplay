using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace UniPlay.Infrastructure.Resilience;

/// <summary>
/// Resilience policies for external API calls using Polly v8
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy for transient failures (3 retries with exponential backoff)
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRetryPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response =>
                        (int)response.StatusCode >= 500 ||
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker policy (breaks after 5 failures in 30 seconds, breaks for 1 minute)
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromMinutes(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => (int)response.StatusCode >= 500)
            })
            .Build();
    }

    /// <summary>
    /// Rate limiting policy (placeholder - implement when needed)
    /// Note: Rate limiting can be added by installing Microsoft.Extensions.Http.Resilience
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRateLimitPolicy()
    {
        // Rate limiting would go here
        // For now, return an empty pipeline
        return new ResiliencePipelineBuilder<HttpResponseMessage>().Build();
    }

    /// <summary>
    /// Timeout policy (30 seconds for API calls)
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateTimeoutPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    /// <summary>
    /// Combined resilience pipeline with all policies
    /// Order: Timeout -> Retry -> Circuit Breaker
    /// Note: Rate limiting removed - can be added later with Microsoft.Extensions.Http.Resilience
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCombinedPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response =>
                        (int)response.StatusCode >= 500 ||
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromMinutes(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => (int)response.StatusCode >= 500)
            })
            .Build();
    }
}
