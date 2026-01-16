namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Contains filter delegates for client-side interceptor operations.
/// </summary>
/// <remarks>
/// <para>
/// Filters provide a middleware-like mechanism to wrap interceptor handler invocations,
/// allowing for cross-cutting concerns like logging, timing, or additional validation.
/// </para>
/// <para>
/// Filters are applied in the order they are added, with each filter wrapping the next
/// handler in the chain.
/// </para>
/// </remarks>
public class InterceptorClientFilters
{
    /// <summary>
    /// Gets the list of filters for the list interceptors handler.
    /// </summary>
    public List<Func<ListInterceptorsRequestParams?, Func<ListInterceptorsRequestParams?, CancellationToken, ValueTask<ListInterceptorsResult>>, CancellationToken, ValueTask<ListInterceptorsResult>>> ListInterceptorsFilters { get; } = [];

    /// <summary>
    /// Gets the list of filters for the invoke interceptor handler.
    /// </summary>
    public List<Func<InvokeInterceptorRequestParams, Func<InvokeInterceptorRequestParams, CancellationToken, ValueTask<InterceptorResult>>, CancellationToken, ValueTask<InterceptorResult>>> InvokeInterceptorFilters { get; } = [];
}
