using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Wraps an <see cref="McpClient"/> and automatically executes interceptor chains for tool operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InterceptingMcpClient"/> provides a decorator pattern implementation that wraps an
/// existing <see cref="McpClient"/> and intercepts tool-related operations (CallToolAsync
/// and <see cref="ListToolsAsync"/>) to execute validation, mutation, and observability interceptors
/// according to the SEP-1763 specification.
/// </para>
/// <para>
/// <b>Execution Model (SEP-1763):</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Sending (outgoing request):</b> Mutations execute sequentially by priority, then validations and observability execute in parallel.</description></item>
/// <item><description><b>Receiving (incoming response):</b> Validations and observability execute in parallel, then mutations execute sequentially by priority.</description></item>
/// </list>
/// <para>
/// Only validation interceptors with <see cref="ValidationSeverity.Error"/> severity can block execution.
/// Info and warning severities are recorded but do not prevent the operation from proceeding.
/// </para>
/// </remarks>
/// <example>
/// Using InterceptingMcpClient with interceptors:
/// <code>
/// // Create MCP client normally
/// await using var client = await McpClient.CreateAsync(transport);
/// 
/// // Wrap with interceptors using extension method
/// var interceptedClient = client.WithInterceptors(new InterceptingMcpClientOptions
/// {
///     Interceptors =
///     [
///         McpClientInterceptor.Create(
///             name: "pii-validator",
///             events: [InterceptorEvents.ToolsCall],
///             type: InterceptorType.Validation,
///             handler: (ctx, ct) =&gt;
///             {
///                 // Validate no PII in arguments
///                 return ValueTask.FromResult(ValidationInterceptorResult.Success());
///             })
///     ]
/// });
/// 
/// // Use intercepted client - interceptors run automatically
/// try
/// {
///     var result = await interceptedClient.CallToolAsync("my-tool", args);
/// }
/// catch (McpInterceptorValidationException ex)
/// {
///     Console.WriteLine($"Blocked by: {ex.AbortedAt?.Interceptor}");
/// }
/// </code>
/// </example>
public sealed class InterceptingMcpClient : IAsyncDisposable
{
    private readonly McpClient _inner;
    private readonly InterceptorChainExecutor _executor;
    private readonly InterceptingMcpClientOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptingMcpClient"/> class.
    /// </summary>
    /// <param name="inner">The underlying <see cref="McpClient"/> to wrap.</param>
    /// <param name="options">Configuration options including interceptors and settings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public InterceptingMcpClient(McpClient inner, InterceptingMcpClientOptions options)
    {
        Throw.IfNull(inner);
        Throw.IfNull(options);

        _inner = inner;
        _options = options;
        _executor = new InterceptorChainExecutor(
            options.Interceptors,
            options.Services);
    }

    /// <summary>
    /// Gets the underlying <see cref="McpClient"/> instance.
    /// </summary>
    /// <remarks>
    /// Use this property to access non-intercepted operations or properties from the underlying client,
    /// such as prompts, resources, or other MCP features not currently supported by interception.
    /// </remarks>
    public McpClient Inner => _inner;

    /// <summary>
    /// Gets the capabilities supported by the connected server.
    /// </summary>
    public ServerCapabilities ServerCapabilities => _inner.ServerCapabilities;

    /// <summary>
    /// Gets the implementation information of the connected server.
    /// </summary>
    public Implementation ServerInfo => _inner.ServerInfo;

    /// <summary>
    /// Gets any instructions describing how to use the connected server and its features.
    /// </summary>
    public string? ServerInstructions => _inner.ServerInstructions;

    /// <summary>
    /// Gets the configuration options for this intercepting client.
    /// </summary>
    public InterceptingMcpClientOptions Options => _options;

    #region CallToolAsync

    /// <summary>
    /// Invokes a tool on the server with interceptor chain execution.
    /// </summary>
    /// <param name="toolName">The name of the tool to call on the server.</param>
    /// <param name="arguments">An optional dictionary of arguments to pass to the tool.</param>
    /// <param name="progress">An optional progress reporter for server notifications.</param>
    /// <param name="options">Optional request options including metadata, serialization settings, and progress tracking.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The <see cref="CallToolResult"/> from the tool execution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toolName"/> is <see langword="null"/>.</exception>
    /// <exception cref="McpInterceptorValidationException">A validation interceptor returned error severity and <see cref="InterceptingMcpClientOptions.ThrowOnValidationError"/> is <see langword="true"/>.</exception>
    /// <exception cref="McpException">The request failed or the server returned an error response.</exception>
    /// <remarks>
    /// <para>
    /// This method executes the interceptor chain in two phases:
    /// </para>
    /// <list type="number">
    /// <item><description><b>Request interception:</b> Before sending the request to the server, interceptors run according to the sending order (mutations → validations/observability in parallel).</description></item>
    /// <item><description><b>Response interception:</b> After receiving the response, interceptors run according to the receiving order (validations/observability in parallel → mutations).</description></item>
    /// </list>
    /// <para>
    /// If a validation interceptor fails with error severity during request interception, the request
    /// is not sent to the server and <see cref="McpInterceptorValidationException"/> is thrown.
    /// </para>
    /// </remarks>
    public async ValueTask<CallToolResult> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments = null,
        IProgress<ProgressNotificationValue>? progress = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(toolName);

        // Phase 1: Intercept outgoing request
        var requestPayload = PayloadConverter.ToCallToolRequestPayload(toolName, arguments);
        
        var sendingResult = await _executor.ExecuteForSendingAsync(
            InterceptorEvents.ToolsCall,
            requestPayload,
            _options.DefaultConfig,
            _options.DefaultTimeoutMs,
            cancellationToken).ConfigureAwait(false);

        // Check if validation failed
        if (sendingResult.Status == InterceptorChainStatus.ValidationFailed)
        {
            if (_options.ThrowOnValidationError)
            {
                throw new McpInterceptorValidationException(
                    $"Interceptor validation failed for tools/call '{toolName}': {sendingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                    sendingResult);
            }

            // Return an error result if not throwing
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = sendingResult.AbortedAt?.Reason ?? "Validation failed" }]
            };
        }

        // Check for timeout or mutation failure
        if (sendingResult.Status == InterceptorChainStatus.Timeout)
        {
            throw new McpInterceptorValidationException(
                $"Interceptor chain timed out for tools/call '{toolName}'",
                sendingResult);
        }

        if (sendingResult.Status == InterceptorChainStatus.MutationFailed)
        {
            throw new McpInterceptorValidationException(
                $"Interceptor mutation failed for tools/call '{toolName}': {sendingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                sendingResult);
        }

        // Extract potentially mutated request parameters
        var (mutatedToolName, mutatedArguments) = PayloadConverter.FromCallToolRequestPayload(sendingResult.FinalPayload);

        // Phase 2: Call the underlying client
        CallToolResult result;
        if (progress is not null)
        {
            // Use the high-level overload which handles progress registration
            result = await _inner.CallToolAsync(
                mutatedToolName,
                ConvertToObjectDictionary(mutatedArguments),
                progress,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Use the low-level overload for better performance
            var serializerOptions = options?.JsonSerializerOptions ?? McpJsonUtilities.DefaultOptions;
            result = await _inner.CallToolAsync(
                new CallToolRequestParams
                {
                    Name = mutatedToolName,
                    Arguments = mutatedArguments,
                    Meta = options?.GetMetaForRequest(),
                },
                cancellationToken).ConfigureAwait(false);
        }

        // Phase 3: Intercept incoming response (if enabled)
        if (_options.InterceptResponses)
        {
            var responsePayload = PayloadConverter.ToCallToolResultPayload(result);
            
            var receivingResult = await _executor.ExecuteForReceivingAsync(
                InterceptorEvents.ToolsCall,
                responsePayload,
                _options.DefaultConfig,
                _options.DefaultTimeoutMs,
                cancellationToken).ConfigureAwait(false);

            // Check if validation failed on response
            if (receivingResult.Status == InterceptorChainStatus.ValidationFailed)
            {
                if (_options.ThrowOnValidationError)
                {
                    throw new McpInterceptorValidationException(
                        $"Interceptor validation failed for tools/call response from '{toolName}': {receivingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                        receivingResult);
                }

                // Return an error result if not throwing
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = receivingResult.AbortedAt?.Reason ?? "Response validation failed" }]
                };
            }

            // Extract potentially mutated response
            var mutatedResult = PayloadConverter.FromCallToolResultPayload(receivingResult.FinalPayload);
            if (mutatedResult is not null)
            {
                result = mutatedResult;
            }
        }

        return result;
    }

    /// <summary>
    /// Invokes a tool on the server with interceptor chain execution.
    /// </summary>
    /// <param name="requestParams">The request parameters to send in the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requestParams"/> is <see langword="null"/>.</exception>
    /// <exception cref="McpInterceptorValidationException">A validation interceptor returned error severity.</exception>
    /// <exception cref="McpException">The request failed or the server returned an error response.</exception>
    public async ValueTask<CallToolResult> CallToolAsync(
        CallToolRequestParams requestParams,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(requestParams);

        // Phase 1: Intercept outgoing request
        var requestPayload = PayloadConverter.ToCallToolRequestParamsPayload(requestParams);
        
        var sendingResult = await _executor.ExecuteForSendingAsync(
            InterceptorEvents.ToolsCall,
            requestPayload,
            _options.DefaultConfig,
            _options.DefaultTimeoutMs,
            cancellationToken).ConfigureAwait(false);

        // Check if validation failed
        if (sendingResult.Status == InterceptorChainStatus.ValidationFailed)
        {
            if (_options.ThrowOnValidationError)
            {
                throw new McpInterceptorValidationException(
                    $"Interceptor validation failed for tools/call '{requestParams.Name}': {sendingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                    sendingResult);
            }

            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = sendingResult.AbortedAt?.Reason ?? "Validation failed" }]
            };
        }

        if (sendingResult.Status != InterceptorChainStatus.Success)
        {
            throw new McpInterceptorValidationException(
                $"Interceptor chain failed for tools/call '{requestParams.Name}': {sendingResult.AbortedAt?.Reason ?? sendingResult.Status.ToString()}",
                sendingResult);
        }

        // Extract potentially mutated request parameters
        var mutatedParams = PayloadConverter.FromCallToolRequestParamsPayload(sendingResult.FinalPayload)
            ?? requestParams;

        // Phase 2: Call the underlying client
        var result = await _inner.CallToolAsync(mutatedParams, cancellationToken).ConfigureAwait(false);

        // Phase 3: Intercept incoming response (if enabled)
        if (_options.InterceptResponses)
        {
            var responsePayload = PayloadConverter.ToCallToolResultPayload(result);
            
            var receivingResult = await _executor.ExecuteForReceivingAsync(
                InterceptorEvents.ToolsCall,
                responsePayload,
                _options.DefaultConfig,
                _options.DefaultTimeoutMs,
                cancellationToken).ConfigureAwait(false);

            if (receivingResult.Status == InterceptorChainStatus.ValidationFailed)
            {
                if (_options.ThrowOnValidationError)
                {
                    throw new McpInterceptorValidationException(
                        $"Interceptor validation failed for tools/call response: {receivingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                        receivingResult);
                }

                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = receivingResult.AbortedAt?.Reason ?? "Response validation failed" }]
                };
            }

            var mutatedResult = PayloadConverter.FromCallToolResultPayload(receivingResult.FinalPayload);
            if (mutatedResult is not null)
            {
                result = mutatedResult;
            }
        }

        return result;
    }

    #endregion

    #region ListToolsAsync

    /// <summary>
    /// Retrieves a list of available tools from the server with interceptor chain execution.
    /// </summary>
    /// <param name="options">Optional request options including metadata, serialization settings, and progress tracking.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A list of all available tools as <see cref="McpClientTool"/> instances.</returns>
    /// <exception cref="McpInterceptorValidationException">A validation interceptor returned error severity.</exception>
    /// <exception cref="McpException">The request failed or the server returned an error response.</exception>
    /// <remarks>
    /// <para>
    /// This method handles pagination automatically, executing interceptors for each page of results.
    /// The returned tools are associated with this <see cref="InterceptingMcpClient"/> instance (via the inner client),
    /// so invoking them through their InvokeAsync method will NOT execute interceptors.
    /// </para>
    /// <para>
    /// To ensure interceptors are executed for tool calls, use <see cref="CallToolAsync(string, IReadOnlyDictionary{string, object?}?, IProgress{ProgressNotificationValue}?, RequestOptions?, CancellationToken)"/>
    /// directly on this <see cref="InterceptingMcpClient"/> instance instead of calling tools through <see cref="McpClientTool"/>.
    /// </para>
    /// </remarks>
    public async ValueTask<IList<McpClientTool>> ListToolsAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        List<McpClientTool>? tools = null;
        string? cursor = null;

        do
        {
            // Phase 1: Intercept outgoing request
            var requestPayload = PayloadConverter.ToListToolsRequestPayload(cursor);
            
            var sendingResult = await _executor.ExecuteForSendingAsync(
                InterceptorEvents.ToolsList,
                requestPayload,
                _options.DefaultConfig,
                _options.DefaultTimeoutMs,
                cancellationToken).ConfigureAwait(false);

            if (sendingResult.Status == InterceptorChainStatus.ValidationFailed)
            {
                if (_options.ThrowOnValidationError)
                {
                    throw new McpInterceptorValidationException(
                        $"Interceptor validation failed for tools/list: {sendingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                        sendingResult);
                }

                // Return empty list if not throwing
                return [];
            }

            if (sendingResult.Status != InterceptorChainStatus.Success)
            {
                throw new McpInterceptorValidationException(
                    $"Interceptor chain failed for tools/list: {sendingResult.AbortedAt?.Reason ?? sendingResult.Status.ToString()}",
                    sendingResult);
            }

            // Extract potentially mutated cursor
            var mutatedCursor = PayloadConverter.FromListToolsRequestPayload(sendingResult.FinalPayload);

            // Phase 2: Call the underlying client
            var requestParams = new ListToolsRequestParams
            {
                Cursor = mutatedCursor,
                Meta = options?.GetMetaForRequest()
            };

            var toolResults = await _inner.ListToolsAsync(requestParams, cancellationToken).ConfigureAwait(false);

            // Phase 3: Intercept incoming response (if enabled)
            if (_options.InterceptResponses)
            {
                var responsePayload = PayloadConverter.ToListToolsResultPayload(toolResults);
                
                var receivingResult = await _executor.ExecuteForReceivingAsync(
                    InterceptorEvents.ToolsList,
                    responsePayload,
                    _options.DefaultConfig,
                    _options.DefaultTimeoutMs,
                    cancellationToken).ConfigureAwait(false);

                if (receivingResult.Status == InterceptorChainStatus.ValidationFailed)
                {
                    if (_options.ThrowOnValidationError)
                    {
                        throw new McpInterceptorValidationException(
                            $"Interceptor validation failed for tools/list response: {receivingResult.AbortedAt?.Reason ?? "Unknown reason"}",
                            receivingResult);
                    }

                    return tools ?? [];
                }

                var mutatedResults = PayloadConverter.FromListToolsResultPayload(receivingResult.FinalPayload);
                if (mutatedResults is not null)
                {
                    toolResults = mutatedResults;
                }
            }

            // Add tools to the result list
            tools ??= new(toolResults.Tools.Count);
            foreach (var tool in toolResults.Tools)
            {
                // Note: Tools are associated with the inner client, not this intercepting wrapper
                // This means calling tool.InvokeAsync() will bypass interceptors
                tools.Add(new McpClientTool(_inner, tool, options?.JsonSerializerOptions));
            }

            cursor = toolResults.NextCursor;
        }
        while (cursor is not null);

        return tools ?? [];
    }

    #endregion

    #region IAsyncDisposable

    /// <summary>
    /// Disposes the underlying <see cref="McpClient"/> instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts a dictionary of JsonElement values to object values for compatibility with the high-level CallToolAsync overload.
    /// </summary>
    private static IReadOnlyDictionary<string, object?>? ConvertToObjectDictionary(Dictionary<string, JsonElement>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, object?>(arguments.Count);
        foreach (var kvp in arguments)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    #endregion
}
