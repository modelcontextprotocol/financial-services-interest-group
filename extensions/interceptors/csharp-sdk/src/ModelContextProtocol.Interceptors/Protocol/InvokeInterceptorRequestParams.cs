using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the parameters used with an interceptor/invoke request
/// to invoke a specific interceptor.
/// </summary>
/// <remarks>
/// The server responds with an interceptor result type appropriate to the interceptor's type
/// (e.g., <see cref="ValidationInterceptorResult"/> for validation interceptors).
/// </remarks>
public sealed class InvokeInterceptorRequestParams
{
    /// <summary>
    /// Gets or sets metadata reserved by MCP for protocol-level metadata.
    /// </summary>
    /// <remarks>
    /// Implementations must not make assumptions about its contents.
    /// </remarks>
    [JsonPropertyName("_meta")]
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Gets or sets the name of the interceptor to invoke.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the event type being intercepted.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// </remarks>
    [JsonPropertyName("event")]
    public required string Event { get; set; }

    /// <summary>
    /// Gets or sets the execution phase.
    /// </summary>
    [JsonPropertyName("phase")]
    public InterceptorPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the payload to process.
    /// </summary>
    /// <remarks>
    /// This is the original request or response content to be validated, mutated, or observed.
    /// </remarks>
    [JsonPropertyName("payload")]
    public required JsonNode Payload { get; set; }

    /// <summary>
    /// Gets or sets optional interceptor-specific configuration for this invocation.
    /// </summary>
    [JsonPropertyName("config")]
    public JsonNode? Config { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for this invocation.
    /// </summary>
    /// <remarks>
    /// If exceeded, the interceptor execution is cancelled and returns a timeout error.
    /// </remarks>
    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets optional context information for this invocation.
    /// </summary>
    [JsonPropertyName("context")]
    public InvokeInterceptorContext? Context { get; set; }
}
