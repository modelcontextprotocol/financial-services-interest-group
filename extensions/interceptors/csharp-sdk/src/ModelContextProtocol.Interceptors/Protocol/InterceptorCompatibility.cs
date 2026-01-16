using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Specifies the MCP protocol version compatibility for an interceptor.
/// </summary>
public sealed class InterceptorCompatibility
{
    /// <summary>
    /// Gets or sets the minimum MCP protocol version required for this interceptor.
    /// </summary>
    [JsonPropertyName("minProtocol")]
    public required string MinProtocol { get; set; }

    /// <summary>
    /// Gets or sets the maximum MCP protocol version supported by this interceptor.
    /// </summary>
    /// <remarks>
    /// If not specified, the interceptor is compatible with all versions at or above <see cref="MinProtocol"/>.
    /// </remarks>
    [JsonPropertyName("maxProtocol")]
    public string? MaxProtocol { get; set; }
}
