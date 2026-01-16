using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the parameters used with a interceptors/list request
/// to discover available interceptors from a server.
/// </summary>
/// <remarks>
/// The server responds with a <see cref="ListInterceptorsResult"/> containing the available interceptors.
/// </remarks>
public sealed class ListInterceptorsRequestParams
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
    /// Gets or sets an opaque token representing the current pagination position.
    /// </summary>
    /// <remarks>
    /// If provided, the server should return results starting after this cursor.
    /// This value should be obtained from the <see cref="ListInterceptorsResult.NextCursor"/>
    /// property of a previous request's response.
    /// </remarks>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    /// <summary>
    /// Gets or sets an optional event filter to list only interceptors that handle the specified event.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// If not specified, all interceptors are returned.
    /// </remarks>
    [JsonPropertyName("event")]
    public string? Event { get; set; }
}
