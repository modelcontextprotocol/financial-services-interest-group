using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents context information passed to an interceptor invocation.
/// </summary>
public sealed class InvokeInterceptorContext
{
    /// <summary>
    /// Gets or sets the identity information for the request.
    /// </summary>
    [JsonPropertyName("principal")]
    public InvokeInterceptorPrincipal? Principal { get; set; }

    /// <summary>
    /// Gets or sets the trace ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the span ID for distributed tracing.
    /// </summary>
    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }

    /// <summary>
    /// Gets or sets the ISO 8601 timestamp of the request.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>
/// Represents identity information for an interceptor invocation.
/// </summary>
public sealed class InvokeInterceptorPrincipal
{
    /// <summary>
    /// Gets or sets the type of principal.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the principal identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets additional claims about the principal.
    /// </summary>
    [JsonPropertyName("claims")]
    public JsonObject? Claims { get; set; }
}
