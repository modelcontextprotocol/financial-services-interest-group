namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Attribute used to mark a method as an MCP client interceptor.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a method, this attribute indicates that the method should be exposed as an
/// MCP interceptor that can validate, mutate, or observe messages on the client side.
/// </para>
/// <para>
/// Client interceptors are invoked when the client sends requests to a server or receives responses,
/// enabling validation and transformation at trust boundaries.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class McpClientInterceptorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the interceptor.
    /// </summary>
    /// <remarks>
    /// If not specified, a name will be derived from the method name.
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
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// This is a required property when using the attribute.
    /// </remarks>
    public string[] Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the interceptor type.
    /// </summary>
    public InterceptorType Type { get; set; } = InterceptorType.Validation;

    /// <summary>
    /// Gets or sets the execution phase for this interceptor.
    /// </summary>
    public InterceptorPhase Phase { get; set; } = InterceptorPhase.Request;

    /// <summary>
    /// Gets or sets the priority hint for mutation interceptor ordering.
    /// </summary>
    /// <remarks>
    /// Lower values execute first. Default is 0 if not specified.
    /// </remarks>
    public int PriorityHint { get; set; }
}
