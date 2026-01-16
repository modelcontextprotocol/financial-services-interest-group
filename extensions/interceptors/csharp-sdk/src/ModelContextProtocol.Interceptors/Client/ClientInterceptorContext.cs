using ModelContextProtocol.Client;

namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Provides a context container for client-side interceptor invocations.
/// </summary>
/// <typeparam name="TParams">Type of the request parameters.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="ClientInterceptorContext{TParams}"/> encapsulates all contextual information for
/// invoking a client-side interceptor. Unlike server-side <c>RequestContext</c>, this context is
/// designed for intercepting outgoing requests and incoming responses at the client level.
/// </para>
/// <para>
/// Client interceptors operate at trust boundaries when:
/// <list type="bullet">
///   <item>Sending requests to servers (request phase)</item>
///   <item>Receiving responses from servers (response phase)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ClientInterceptorContext<TParams>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientInterceptorContext{TParams}"/> class.
    /// </summary>
    /// <param name="client">The MCP client associated with this context, if any.</param>
    public ClientInterceptorContext(McpClient? client = null)
    {
        Client = client;
    }

    /// <summary>
    /// Gets or sets the MCP client associated with this context.
    /// </summary>
    /// <remarks>
    /// May be null for interceptors that operate independently of a specific client session.
    /// </remarks>
    public McpClient? Client { get; set; }

    /// <summary>
    /// Gets or sets the services associated with this invocation.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets or sets the parameters for this interceptor invocation.
    /// </summary>
    public TParams? Params { get; set; }

    /// <summary>
    /// Gets or sets a key/value collection for sharing data within the scope of this invocation.
    /// </summary>
    public IDictionary<string, object?> Items
    {
        get => _items ??= new Dictionary<string, object?>();
        set => _items = value;
    }
    private IDictionary<string, object?>? _items;

    /// <summary>
    /// Gets or sets the matched interceptor primitive, if any.
    /// </summary>
    public McpClientInterceptor? MatchedInterceptor { get; set; }
}
