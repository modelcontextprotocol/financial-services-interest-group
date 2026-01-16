using System.Reflection;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Represents an invocable interceptor used by Model Context Protocol servers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="McpServerInterceptor"/> is an abstract base class that represents an MCP interceptor for use in the server
/// (as opposed to <see cref="Interceptor"/>, which provides the protocol representation of an interceptor).
/// Instances of <see cref="McpServerInterceptor"/> can be added into a <see cref="McpServerPrimitiveCollection{McpServerInterceptor}"/>.
/// </para>
/// <para>
/// Most commonly, <see cref="McpServerInterceptor"/> instances are created using the static <see cref="M:McpServerInterceptor.Create"/> methods.
/// These methods enable creating an <see cref="McpServerInterceptor"/> for a method, specified via a <see cref="Delegate"/> or
/// <see cref="MethodInfo"/>.
/// </para>
/// <para>
/// By default, parameters are bound from the <see cref="InvokeInterceptorRequestParams"/>:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="JsonNode"/> parameters named "payload" are bound to <see cref="InvokeInterceptorRequestParams.Payload"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="InvokeInterceptorContext"/> parameters are bound to <see cref="InvokeInterceptorRequestParams.Context"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="JsonNode"/> parameters named "config" are bound to <see cref="InvokeInterceptorRequestParams.Config"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="CancellationToken"/> parameters are automatically bound to a <see cref="CancellationToken"/> provided by the
///       <see cref="McpServer"/> and that respects any cancellation requests.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IServiceProvider"/> parameters are bound from the <see cref="RequestContext{InvokeInterceptorRequestParams}"/> for this request.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="McpServer"/> parameters are bound directly to the <see cref="McpServer"/> instance associated with this request.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Return values from a method should be <see cref="ValidationInterceptorResult"/> (or convertible to it).
/// </para>
/// </remarks>
public abstract class McpServerInterceptor : IMcpServerPrimitive
{
    /// <summary>Initializes a new instance of the <see cref="McpServerInterceptor"/> class.</summary>
    protected McpServerInterceptor()
    {
    }

    /// <summary>Gets the protocol <see cref="Interceptor"/> type for this instance.</summary>
    public abstract Interceptor ProtocolInterceptor { get; }

    /// <summary>
    /// Gets the metadata for this interceptor instance.
    /// </summary>
    /// <remarks>
    /// Contains attributes from the associated MethodInfo and declaring class (if any),
    /// with class-level attributes appearing before method-level attributes.
    /// </remarks>
    public abstract IReadOnlyList<object> Metadata { get; }

    /// <summary>Invokes the <see cref="McpServerInterceptor"/>.</summary>
    /// <param name="request">The request information resulting in the invocation of this interceptor.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result from invoking the interceptor.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public abstract ValueTask<ValidationInterceptorResult> InvokeAsync(
        RequestContext<InvokeInterceptorRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via a <see cref="Delegate"/> instance.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="McpServerInterceptor"/>.</param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpServerInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpServerInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    public static McpServerInterceptor Create(
        Delegate method,
        McpServerInterceptorCreateOptions? options = null) =>
        ReflectionMcpServerInterceptor.Create(method, options);

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="McpServerInterceptor"/>.</param>
    /// <param name="target">The instance if <paramref name="method"/> is an instance method; otherwise, <see langword="null"/>.</param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpServerInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpServerInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> is an instance method but <paramref name="target"/> is <see langword="null"/>.</exception>
    public static McpServerInterceptor Create(
        MethodInfo method,
        object? target = null,
        McpServerInterceptorCreateOptions? options = null) =>
        ReflectionMcpServerInterceptor.Create(method, target, options);

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via an <see cref="MethodInfo"/> for
    /// an instance method, along with a factory function to create the target object.
    /// </summary>
    /// <param name="method">The instance method to be represented via the created <see cref="McpServerInterceptor"/>.</param>
    /// <param name="createTargetFunc">
    /// Callback used on each invocation to create an instance of the type on which the instance method <paramref name="method"/>
    /// will be invoked. If the returned instance is <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>, it will
    /// be disposed of after the method completes its invocation.
    /// </param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpServerInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpServerInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> or <paramref name="createTargetFunc"/> is <see langword="null"/>.</exception>
    public static McpServerInterceptor Create(
        MethodInfo method,
        Func<RequestContext<InvokeInterceptorRequestParams>, object> createTargetFunc,
        McpServerInterceptorCreateOptions? options = null) =>
        ReflectionMcpServerInterceptor.Create(method, createTargetFunc, options);

    /// <inheritdoc />
    public override string ToString() => ProtocolInterceptor.Name;

    /// <inheritdoc />
    string IMcpServerPrimitive.Id => ProtocolInterceptor.Name;
}
