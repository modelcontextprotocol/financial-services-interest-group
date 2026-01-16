using ModelContextProtocol.Server;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Contains filter collections for interceptor-related request pipelines.
/// </summary>
public class InterceptorServerFilters
{
    /// <summary>
    /// Gets the list of filters for the list interceptors handler.
    /// </summary>
    public IList<Func<RequestContext<ListInterceptorsRequestParams>, Func<RequestContext<ListInterceptorsRequestParams>, CancellationToken, ValueTask<ListInterceptorsResult>>, CancellationToken, ValueTask<ListInterceptorsResult>>> ListInterceptorsFilters { get; } = [];

    /// <summary>
    /// Gets the list of filters for the invoke interceptor handler.
    /// </summary>
    public IList<Func<RequestContext<InvokeInterceptorRequestParams>, Func<RequestContext<InvokeInterceptorRequestParams>, CancellationToken, ValueTask<ValidationInterceptorResult>>, CancellationToken, ValueTask<ValidationInterceptorResult>>> InvokeInterceptorFilters { get; } = [];
}
