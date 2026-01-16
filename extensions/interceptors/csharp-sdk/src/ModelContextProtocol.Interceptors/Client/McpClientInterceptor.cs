using System.Reflection;
using System.Text.Json.Nodes;

namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Represents an invocable interceptor used by Model Context Protocol clients.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="McpClientInterceptor"/> is an abstract base class that represents an MCP interceptor for use in the client
/// (as opposed to <see cref="Interceptor"/>, which provides the protocol representation of an interceptor).
/// Client interceptors are invoked when processing outgoing requests or incoming responses.
/// </para>
/// <para>
/// Most commonly, <see cref="McpClientInterceptor"/> instances are created using the static <see cref="M:McpClientInterceptor.Create"/> methods.
/// These methods enable creating an <see cref="McpClientInterceptor"/> for a method, specified via a <see cref="Delegate"/> or
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
///       <see cref="CancellationToken"/> parameters are automatically bound to a <see cref="CancellationToken"/> provided by the caller.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IServiceProvider"/> parameters are bound from the <see cref="ClientInterceptorContext{InvokeInterceptorRequestParams}"/> for this request.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Return values from a method should be an interceptor result type appropriate for the interceptor's type:
/// <list type="bullet">
///   <item><see cref="ValidationInterceptorResult"/> for validation interceptors</item>
///   <item><see cref="MutationInterceptorResult"/> for mutation interceptors</item>
///   <item><see cref="ObservabilityInterceptorResult"/> for observability interceptors</item>
/// </list>
/// </para>
/// </remarks>
public abstract class McpClientInterceptor
{
    /// <summary>Initializes a new instance of the <see cref="McpClientInterceptor"/> class.</summary>
    protected McpClientInterceptor()
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

    /// <summary>Invokes the <see cref="McpClientInterceptor"/>.</summary>
    /// <param name="context">The context information for this interceptor invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result from invoking the interceptor.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public abstract ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an <see cref="McpClientInterceptor"/> instance for a method, specified via a <see cref="Delegate"/> instance.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="McpClientInterceptor"/>.</param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpClientInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpClientInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    public static McpClientInterceptor Create(
        Delegate method,
        McpClientInterceptorCreateOptions? options = null) =>
        ReflectionMcpClientInterceptor.Create(method, options);

    /// <summary>
    /// Creates an <see cref="McpClientInterceptor"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="McpClientInterceptor"/>.</param>
    /// <param name="target">The instance if <paramref name="method"/> is an instance method; otherwise, <see langword="null"/>.</param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpClientInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpClientInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> is an instance method but <paramref name="target"/> is <see langword="null"/>.</exception>
    public static McpClientInterceptor Create(
        MethodInfo method,
        object? target = null,
        McpClientInterceptorCreateOptions? options = null) =>
        ReflectionMcpClientInterceptor.Create(method, target, options);

    /// <summary>
    /// Creates an <see cref="McpClientInterceptor"/> instance for a method, specified via an <see cref="MethodInfo"/> for
    /// an instance method, along with a factory function to create the target object.
    /// </summary>
    /// <param name="method">The instance method to be represented via the created <see cref="McpClientInterceptor"/>.</param>
    /// <param name="createTargetFunc">
    /// Callback used on each invocation to create an instance of the type on which the instance method <paramref name="method"/>
    /// will be invoked. If the returned instance is <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>, it will
    /// be disposed of after the method completes its invocation.
    /// </param>
    /// <param name="options">Optional options used in the creation of the <see cref="McpClientInterceptor"/> to control its behavior.</param>
    /// <returns>The created <see cref="McpClientInterceptor"/> for invoking <paramref name="method"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> or <paramref name="createTargetFunc"/> is <see langword="null"/>.</exception>
    public static McpClientInterceptor Create(
        MethodInfo method,
        Func<ClientInterceptorContext<InvokeInterceptorRequestParams>, object> createTargetFunc,
        McpClientInterceptorCreateOptions? options = null) =>
        ReflectionMcpClientInterceptor.Create(method, createTargetFunc, options);

    /// <inheritdoc />
    public override string ToString() => ProtocolInterceptor.Name;
}
