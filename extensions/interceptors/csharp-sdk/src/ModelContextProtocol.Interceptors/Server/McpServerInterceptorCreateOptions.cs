using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Provides options for controlling the creation of an <see cref="McpServerInterceptor"/>.
/// </summary>
/// <remarks>
/// <para>
/// These options allow for customizing the behavior and metadata of interceptors created with
/// <see cref="M:McpServerInterceptor.Create"/>. They provide control over naming, description,
/// events, phase, and dependency injection integration.
/// </para>
/// <para>
/// When creating interceptors programmatically rather than using attributes, these options
/// provide the same level of configuration flexibility.
/// </para>
/// </remarks>
public sealed class McpServerInterceptorCreateOptions
{
    /// <summary>
    /// Gets or sets optional services used in the construction of the <see cref="McpServerInterceptor"/>.
    /// </summary>
    /// <remarks>
    /// These services will be used to determine which parameters should be satisfied from dependency injection.
    /// </remarks>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets or sets the name to use for the <see cref="McpServerInterceptor"/>.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, but an <see cref="McpServerInterceptorAttribute"/> is applied to the method,
    /// the name from the attribute is used. If that's not present, a name based on the method's name is used.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the version to use for the <see cref="McpServerInterceptor"/>.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the description to use for the <see cref="McpServerInterceptor"/>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the events this interceptor handles.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// </remarks>
    public IList<string>? Events { get; set; }

    /// <summary>
    /// Gets or sets the execution phase for this interceptor.
    /// </summary>
    public InterceptorPhase? Phase { get; set; }

    /// <summary>
    /// Gets or sets the priority hint for mutation interceptor ordering.
    /// </summary>
    public InterceptorPriorityHint? PriorityHint { get; set; }

    /// <summary>
    /// Gets or sets the JSON Schema for interceptor configuration.
    /// </summary>
    public JsonElement? ConfigSchema { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options to use when marshalling data to/from JSON.
    /// </summary>
    /// <value>
    /// The default is <see cref="McpJsonUtilities.DefaultOptions"/>.
    /// </value>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the metadata associated with the interceptor.
    /// </summary>
    /// <remarks>
    /// Metadata includes information such as attributes extracted from the method and its declaring class.
    /// If not provided, metadata will be automatically generated for methods created via reflection.
    /// </remarks>
    public IReadOnlyList<object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets metadata reserved by MCP for protocol-level metadata.
    /// </summary>
    /// <remarks>
    /// Implementations must not make assumptions about its contents.
    /// </remarks>
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Creates a shallow clone of the current <see cref="McpServerInterceptorCreateOptions"/> instance.
    /// </summary>
    internal McpServerInterceptorCreateOptions Clone() =>
        new()
        {
            Services = Services,
            Name = Name,
            Version = Version,
            Description = Description,
            Events = Events,
            Phase = Phase,
            PriorityHint = PriorityHint,
            ConfigSchema = ConfigSchema,
            SerializerOptions = SerializerOptions,
            Metadata = Metadata,
            Meta = Meta,
        };
}
