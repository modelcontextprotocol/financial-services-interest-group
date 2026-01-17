using System.Text.Json.Nodes;
using ModelContextProtocol.Interceptors.Client;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Configuration options for <see cref="InterceptingMcpClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use these options to configure how the <see cref="InterceptingMcpClient"/> executes interceptor
/// chains for MCP tool operations. You can register interceptors, configure timeouts, and control
/// error handling behavior.
/// </para>
/// </remarks>
/// <example>
/// Creating options with interceptors:
/// <code>
/// var options = new InterceptingMcpClientOptions
/// {
///     Interceptors =
///     [
///         McpClientInterceptor.Create(
///             name: "logging",
///             events: [InterceptorEvents.ToolsCall],
///             type: InterceptorType.Observability,
///             handler: (ctx, ct) => { /* log */ })
///     ],
///     DefaultTimeoutMs = 5000,
///     ThrowOnValidationError = true
/// };
/// </code>
/// </example>
public sealed class InterceptingMcpClientOptions
{
    /// <summary>
    /// Gets or sets the collection of interceptors to execute for MCP operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Interceptors are executed according to SEP-1763 ordering rules:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Sending (outgoing):</b> Mutations run sequentially by priority, then validations and observability run in parallel.</description></item>
    /// <item><description><b>Receiving (incoming):</b> Validations and observability run in parallel, then mutations run sequentially by priority.</description></item>
    /// </list>
    /// <para>
    /// Only interceptors whose <see cref="Interceptor.Events"/> match the current operation
    /// (e.g., "tools/call") will be executed.
    /// </para>
    /// </remarks>
    public IList<McpClientInterceptor> Interceptors { get; set; } = [];

    /// <summary>
    /// Gets or sets the service provider for dependency injection.
    /// </summary>
    /// <remarks>
    /// If provided, this service provider will be passed to interceptors via the
    /// <see cref="ClientInterceptorContext{TParams}.Services"/> property, allowing
    /// interceptors to resolve dependencies.
    /// </remarks>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets or sets the default timeout in milliseconds for interceptor chain execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the interceptor chain takes longer than this timeout, execution will be aborted
    /// and the chain result will have status <see cref="InterceptorChainStatus.Timeout"/>.
    /// </para>
    /// <para>
    /// Set to <c>null</c> for no timeout (default). A reasonable production value might be
    /// 5000-30000ms depending on interceptor complexity.
    /// </para>
    /// </remarks>
    public int? DefaultTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the default configuration passed to interceptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This configuration dictionary is merged with any per-call configuration and passed
    /// to interceptors. Keys should match interceptor names, and values should be JSON
    /// objects containing interceptor-specific configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// Setting default interceptor configuration:
    /// <code>
    /// options.DefaultConfig = new Dictionary&lt;string, JsonNode&gt;
    /// {
    ///     ["pii-filter"] = JsonNode.Parse("""{"sensitivity": "high", "regions": ["US", "EU"]}"""),
    ///     ["rate-limiter"] = JsonNode.Parse("""{"maxRequestsPerMinute": 100}""")
    /// };
    /// </code>
    /// </example>
    public IDictionary<string, JsonNode>? DefaultConfig { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to throw <see cref="McpInterceptorValidationException"/>
    /// when a validation interceptor fails with error severity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), the <see cref="InterceptingMcpClient"/> will throw
    /// <see cref="McpInterceptorValidationException"/> if any validation interceptor returns
    /// <see cref="ValidationSeverity.Error"/>. The exception contains the full
    /// <see cref="InterceptorChainResult"/> for inspection.
    /// </para>
    /// <para>
    /// When <c>false</c>, validation errors will not throw exceptions. Instead, the operation
    /// will proceed without calling the underlying MCP client, and the caller is responsible
    /// for checking results. This mode is primarily useful for testing or scenarios where
    /// you want to handle validation failures differently.
    /// </para>
    /// </remarks>
    /// <value><c>true</c> to throw on validation errors; <c>false</c> to continue silently. Default is <c>true</c>.</value>
    public bool ThrowOnValidationError { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to execute interceptors for response/result payloads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), interceptors will be executed both when sending requests
    /// (via <see cref="InterceptorChainExecutor.ExecuteForSendingAsync"/>) and when receiving
    /// responses (via <see cref="InterceptorChainExecutor.ExecuteForReceivingAsync"/>).
    /// </para>
    /// <para>
    /// When <c>false</c>, only request interception is performed. This can be useful when you
    /// only need to validate/transform outgoing requests and want to minimize overhead on responses.
    /// </para>
    /// </remarks>
    /// <value><c>true</c> to intercept responses; <c>false</c> to only intercept requests. Default is <c>true</c>.</value>
    public bool InterceptResponses { get; set; } = true;
}
