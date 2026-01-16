using ModelContextProtocol.Server;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Contains handlers for interceptor-related requests.
/// </summary>
public class InterceptorServerHandlers
{
    /// <summary>
    /// Gets or sets the handler for listing interceptors.
    /// </summary>
    public Func<RequestContext<ListInterceptorsRequestParams>, CancellationToken, ValueTask<ListInterceptorsResult>>? ListInterceptorsHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for invoking interceptors.
    /// </summary>
    public Func<RequestContext<InvokeInterceptorRequestParams>, CancellationToken, ValueTask<ValidationInterceptorResult>>? InvokeInterceptorHandler { get; set; }
}
