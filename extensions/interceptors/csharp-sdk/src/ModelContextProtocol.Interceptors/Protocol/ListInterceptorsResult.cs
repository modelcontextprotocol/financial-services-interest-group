using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents a server's response to a interceptors/list request,
/// containing available interceptors.
/// </summary>
/// <remarks>
/// This result is returned when a client sends a interceptors/list request
/// to discover available interceptors on the server.
/// </remarks>
public sealed class ListInterceptorsResult
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
    /// Gets or sets an opaque token representing the pagination position after the last returned result.
    /// </summary>
    /// <remarks>
    /// When a paginated result has more data available, the <see cref="NextCursor"/>
    /// property will contain a non-<see langword="null"/> token that can be used in subsequent requests
    /// to fetch the next page. When there are no more results to return, the <see cref="NextCursor"/> property
    /// will be <see langword="null"/>.
    /// </remarks>
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    /// <summary>
    /// Gets or sets the list of available interceptors.
    /// </summary>
    [JsonPropertyName("interceptors")]
    public IList<Interceptor> Interceptors { get; set; } = [];
}
