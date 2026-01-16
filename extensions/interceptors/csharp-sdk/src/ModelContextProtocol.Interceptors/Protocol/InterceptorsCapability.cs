using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the interceptors capability configuration.
/// </summary>
/// <remarks>
/// This capability indicates that a server supports the interceptor framework
/// as defined in SEP-1763.
/// </remarks>
public sealed class InterceptorsCapability
{
    /// <summary>
    /// Gets or sets the events that this server's interceptors can handle.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// </remarks>
    [JsonPropertyName("supportedEvents")]
    public IList<string>? SupportedEvents { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether this server supports notifications
    /// for changes to the interceptor list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; set; }
}
