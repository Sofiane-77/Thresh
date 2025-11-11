namespace Thresh.Abstractions;

public sealed class ThreshOptions
{
    /// <summary>Accept self-signed certificates from the local LCU (HTTP).</summary>
    public bool AcceptSelfSignedCertificates { get; set; } = true;

    /// <summary>Accept self-signed certificates for the LCU WebSocket (loopback only).</summary>
    public bool WsAcceptSelfSignedCertificates { get; set; } = true;

    /// <summary>Base delay used for retry backoff and WS reconnects.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    // HTTP — Polly
    /// <summary>Maximum number of retry attempts (excluding the initial try).</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Per-request HTTP timeout (HttpClient.Timeout).</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Duration the circuit stays open once tripped.</summary>
    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Sampling window for assessing failures.</summary>
    public TimeSpan CircuitSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Minimum throughput required for circuit state evaluation.</summary>
    public int CircuitMinimumThroughput { get; set; } = 20;

    /// <summary>Failure ratio threshold (0..1) to open the circuit.</summary>
    public double CircuitFailureRatio { get; set; } = 0.5;

    // WebSocket — robustness
    /// <summary>Maximum backoff used for WS reconnections.</summary>
    public TimeSpan WsMaxBackoff { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Silence threshold after which a WS reconnect is forced.</summary>
    public TimeSpan WsSilenceThreshold { get; set; } = TimeSpan.FromSeconds(45);
}
