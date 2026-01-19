using ModelContextProtocol.Client;
using ModelContextProtocol.Interceptors.Client;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Provides extension methods for wrapping <see cref="McpClient"/> with interceptor support.
/// </summary>
public static class InterceptingMcpClientExtensions
{
    /// <summary>
    /// Wraps an <see cref="McpClient"/> with interceptor chain execution for tool operations.
    /// </summary>
    /// <param name="client">The <see cref="McpClient"/> to wrap.</param>
    /// <param name="options">Configuration options including interceptors and settings.</param>
    /// <returns>An <see cref="InterceptingMcpClient"/> that executes interceptor chains for tool operations.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This extension method creates an <see cref="InterceptingMcpClient"/> that wraps the provided
    /// <see cref="McpClient"/> and automatically executes interceptor chains for <c>tools/call</c>
    /// and <c>tools/list</c> operations according to SEP-1763.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await using var client = await McpClient.CreateAsync(transport);
    /// 
    /// var interceptedClient = client.WithInterceptors(new InterceptingMcpClientOptions
    /// {
    ///     Interceptors =
    ///     [
    ///         McpClientInterceptor.Create(
    ///             name: "audit-logger",
    ///             events: [InterceptorEvents.ToolsCall],
    ///             type: InterceptorType.Observability,
    ///             handler: async (ctx, ct) =&gt;
    ///             {
    ///                 await LogToolCallAsync(ctx.Params);
    ///                 return ObservabilityInterceptorResult.Success();
    ///             })
    ///     ],
    ///     DefaultTimeoutMs = 5000
    /// });
    /// 
    /// var result = await interceptedClient.CallToolAsync("my-tool", args);
    /// </code>
    /// </example>
    public static InterceptingMcpClient WithInterceptors(
        this McpClient client,
        InterceptingMcpClientOptions options)
    {
        Throw.IfNull(client);
        Throw.IfNull(options);

        return new InterceptingMcpClient(client, options);
    }

    /// <summary>
    /// Wraps an <see cref="McpClient"/> with interceptor chain execution using the specified interceptors.
    /// </summary>
    /// <param name="client">The <see cref="McpClient"/> to wrap.</param>
    /// <param name="interceptors">The interceptors to register.</param>
    /// <returns>An <see cref="InterceptingMcpClient"/> that executes interceptor chains for tool operations.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This is a convenience overload that creates an <see cref="InterceptingMcpClient"/> with the
    /// specified interceptors using default options (no timeout, throw on validation error, intercept responses).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await using var client = await McpClient.CreateAsync(transport);
    /// 
    /// var interceptedClient = client.WithInterceptors(
    ///     McpClientInterceptor.Create(
    ///         name: "rate-limiter",
    ///         events: [InterceptorEvents.ToolsCall],
    ///         type: InterceptorType.Validation,
    ///         handler: (ctx, ct) =&gt; ValueTask.FromResult(ValidationInterceptorResult.Success())),
    ///     McpClientInterceptor.Create(
    ///         name: "pii-filter",
    ///         events: [InterceptorEvents.ToolsCall],
    ///         type: InterceptorType.Validation,
    ///         handler: (ctx, ct) =&gt; ValueTask.FromResult(ValidatePii(ctx))));
    /// </code>
    /// </example>
    public static InterceptingMcpClient WithInterceptors(
        this McpClient client,
        params McpClientInterceptor[] interceptors)
    {
        Throw.IfNull(client);

        return new InterceptingMcpClient(client, new InterceptingMcpClientOptions
        {
            Interceptors = interceptors ?? []
        });
    }

    /// <summary>
    /// Wraps an <see cref="McpClient"/> with interceptor chain execution using the specified interceptors and service provider.
    /// </summary>
    /// <param name="client">The <see cref="McpClient"/> to wrap.</param>
    /// <param name="interceptors">The interceptors to register.</param>
    /// <param name="services">The service provider for dependency injection in interceptors.</param>
    /// <returns>An <see cref="InterceptingMcpClient"/> that executes interceptor chains for tool operations.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This overload allows you to provide a service provider for dependency injection support
    /// in interceptors that need to resolve services.
    /// </para>
    /// </remarks>
    public static InterceptingMcpClient WithInterceptors(
        this McpClient client,
        IEnumerable<McpClientInterceptor> interceptors,
        IServiceProvider? services = null)
    {
        Throw.IfNull(client);

        return new InterceptingMcpClient(client, new InterceptingMcpClientOptions
        {
            Interceptors = interceptors?.ToList() ?? [],
            Services = services
        });
    }
}
