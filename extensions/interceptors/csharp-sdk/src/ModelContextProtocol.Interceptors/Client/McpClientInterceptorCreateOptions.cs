using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Options for creating an <see cref="McpClientInterceptor"/>.
/// </summary>
public sealed class McpClientInterceptorCreateOptions
{
    /// <summary>
    /// Gets or sets the name of the interceptor.
    /// </summary>
    /// <remarks>
    /// If not provided, the name will be derived from the method name.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the interceptor.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the description of the interceptor.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the events this interceptor handles.
    /// </summary>
    public string[]? Events { get; set; }

    /// <summary>
    /// Gets or sets the interceptor type.
    /// </summary>
    public InterceptorType? Type { get; set; }

    /// <summary>
    /// Gets or sets the execution phase.
    /// </summary>
    public InterceptorPhase? Phase { get; set; }

    /// <summary>
    /// Gets or sets the priority hint for mutation ordering.
    /// </summary>
    public int? PriorityHint { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for the interceptor's configuration.
    /// </summary>
    public JsonElement? ConfigSchema { get; set; }

    /// <summary>
    /// Gets or sets protocol-level metadata.
    /// </summary>
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Gets or sets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the metadata for the interceptor.
    /// </summary>
    public IReadOnlyList<object>? Metadata { get; set; }

    /// <summary>
    /// Creates a shallow copy of this options instance.
    /// </summary>
    public McpClientInterceptorCreateOptions Clone() => (McpClientInterceptorCreateOptions)MemberwiseClone();
}
