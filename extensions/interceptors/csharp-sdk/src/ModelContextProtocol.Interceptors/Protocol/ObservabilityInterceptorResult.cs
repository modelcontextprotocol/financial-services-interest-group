using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the result of invoking an observability interceptor.
/// </summary>
/// <remarks>
/// <para>
/// Observability interceptors observe message flow for logging, metrics collection, or auditing
/// without modifying data. They are fire-and-forget and never block execution.
/// </para>
/// <para>
/// Even if an observability interceptor fails, it should not affect the message pipeline.
/// Failures are logged internally but do not propagate to the caller.
/// </para>
/// </remarks>
public sealed class ObservabilityInterceptorResult : InterceptorResult
{
    /// <summary>
    /// Gets the type of interceptor (always "observability" for this result type).
    /// </summary>
    [JsonPropertyName("type")]
    public override InterceptorType Type => InterceptorType.Observability;

    /// <summary>
    /// Gets or sets whether the observation was recorded successfully.
    /// </summary>
    [JsonPropertyName("observed")]
    public bool Observed { get; set; }

    /// <summary>
    /// Gets or sets optional metrics collected during observation.
    /// </summary>
    [JsonPropertyName("metrics")]
    public IDictionary<string, double>? Metrics { get; set; }

    /// <summary>
    /// Gets or sets optional alerts or notifications triggered by this observation.
    /// </summary>
    [JsonPropertyName("alerts")]
    public IList<ObservabilityAlert>? Alerts { get; set; }

    /// <summary>
    /// Creates an observability result indicating successful observation.
    /// </summary>
    /// <returns>An observability result with <see cref="Observed"/> set to true.</returns>
    public static ObservabilityInterceptorResult Success() => new() { Observed = true };

    /// <summary>
    /// Creates an observability result with metrics.
    /// </summary>
    /// <param name="metrics">The metrics collected during observation.</param>
    /// <returns>An observability result with metrics.</returns>
    public static ObservabilityInterceptorResult WithMetrics(IDictionary<string, double> metrics) => new()
    {
        Observed = true,
        Metrics = metrics
    };
}

/// <summary>
/// Represents an alert or notification triggered by an observability interceptor.
/// </summary>
public sealed class ObservabilityAlert
{
    /// <summary>
    /// Gets or sets the severity level of the alert.
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets optional tags for categorizing the alert.
    /// </summary>
    [JsonPropertyName("tags")]
    public IList<string>? Tags { get; set; }
}
