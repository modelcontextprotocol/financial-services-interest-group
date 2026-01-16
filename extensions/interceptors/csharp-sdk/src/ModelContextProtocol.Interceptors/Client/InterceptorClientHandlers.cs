namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Contains handler delegates for client-side interceptor operations.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the handler functions that are invoked when clients need to
/// list available interceptors or invoke specific interceptors.
/// </para>
/// <para>
/// Handlers can be configured through the McpClientInterceptorExtensions
/// extension methods in the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
/// </para>
/// </remarks>
public class InterceptorClientHandlers
{
    /// <summary>
    /// Gets or sets the handler for listing available interceptors.
    /// </summary>
    public Func<ListInterceptorsRequestParams?, CancellationToken, ValueTask<ListInterceptorsResult>>? ListInterceptorsHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for invoking an interceptor.
    /// </summary>
    public Func<InvokeInterceptorRequestParams, CancellationToken, ValueTask<InterceptorResult>>? InvokeInterceptorHandler { get; set; }
}
