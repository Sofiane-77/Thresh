using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Thresh.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Thresh.Core.Http;

internal static class PollyPolicies
{
    public static ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(ThreshOptions opts, ILogger logger)
    {
        // Define what to handle: HTTP exceptions and transient status codes.
        var shouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(res => IsTransient(res.StatusCode));

        // Exponential retry with jitter.
        var retry = new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = shouldHandle,
            MaxRetryAttempts = Math.Max(0, opts.MaxRetryAttempts),
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                logger.LogWarning("HTTP retry #{Attempt} (reason: {Reason})",
                    args.AttemptNumber, args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                return default;
            }
        };

        // Failure rate circuit breaker.
        var circuit = new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = shouldHandle,
            FailureRatio = Math.Clamp(opts.CircuitFailureRatio, 0.01, 0.99),
            MinimumThroughput = Math.Max(2, opts.CircuitMinimumThroughput),
            SamplingDuration = opts.CircuitSamplingDuration,
            BreakDuration = opts.CircuitBreakDuration,
            OnOpened = _ => { logger.LogWarning("HTTP circuit OPENED"); return default; },
            OnClosed = _ => { logger.LogInformation("HTTP circuit CLOSED"); return default; },
            OnHalfOpened = _ => { logger.LogInformation("HTTP circuit HALF-OPEN"); return default; }
        };

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(retry)
            .AddCircuitBreaker(circuit)
            .Build();
    }

    private static bool IsTransient(HttpStatusCode code)
        => code == (HttpStatusCode)429 || (int)code >= 500;
}
