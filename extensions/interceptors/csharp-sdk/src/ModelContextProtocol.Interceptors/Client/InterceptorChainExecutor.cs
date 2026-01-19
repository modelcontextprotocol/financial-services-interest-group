using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Executes interceptor chains following the SEP-1763 execution model.
/// </summary>
/// <remarks>
/// <para>
/// The chain executor handles the ordering and execution of interceptors based on their type:
/// <list type="bullet">
///   <item><b>Mutations</b>: Executed sequentially by priority (lower first), alphabetically for ties</item>
///   <item><b>Validations</b>: Executed in parallel, errors block execution</item>
///   <item><b>Observability</b>: Fire-and-forget, executed in parallel, never block</item>
/// </list>
/// </para>
/// <para>
/// Execution order depends on data flow direction:
/// <list type="bullet">
///   <item><b>Sending</b>: Mutate → Validate &amp; Observe → Send</item>
///   <item><b>Receiving</b>: Receive → Validate &amp; Observe → Mutate</item>
/// </list>
/// </para>
/// </remarks>
public class InterceptorChainExecutor
{
    private readonly IReadOnlyList<McpClientInterceptor> _interceptors;
    private readonly IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorChainExecutor"/> class.
    /// </summary>
    /// <param name="interceptors">The interceptors to execute.</param>
    /// <param name="services">Optional service provider for dependency injection.</param>
    public InterceptorChainExecutor(IEnumerable<McpClientInterceptor> interceptors, IServiceProvider? services = null)
    {
        Throw.IfNull(interceptors);

        _interceptors = interceptors.ToList();
        _services = services;
    }

    /// <summary>
    /// Executes the interceptor chain for outgoing data (sending across trust boundary).
    /// </summary>
    /// <param name="event">The event type being intercepted.</param>
    /// <param name="payload">The payload to process.</param>
    /// <param name="config">Optional per-interceptor configuration.</param>
    /// <param name="timeoutMs">Optional timeout for the entire chain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain execution result.</returns>
    /// <remarks>
    /// Execution order for sending: Mutate (sequential) → Validate &amp; Observe (parallel) → Return
    /// </remarks>
    public Task<InterceptorChainResult> ExecuteForSendingAsync(
        string @event,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config = null,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteChainAsync(@event, InterceptorPhase.Request, payload, config, timeoutMs, isSending: true, cancellationToken);
    }

    /// <summary>
    /// Executes the interceptor chain for incoming data (receiving from trust boundary).
    /// </summary>
    /// <param name="event">The event type being intercepted.</param>
    /// <param name="payload">The payload to process.</param>
    /// <param name="config">Optional per-interceptor configuration.</param>
    /// <param name="timeoutMs">Optional timeout for the entire chain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain execution result.</returns>
    /// <remarks>
    /// Execution order for receiving: Validate &amp; Observe (parallel) → Mutate (sequential) → Return
    /// </remarks>
    public Task<InterceptorChainResult> ExecuteForReceivingAsync(
        string @event,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config = null,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteChainAsync(@event, InterceptorPhase.Response, payload, config, timeoutMs, isSending: false, cancellationToken);
    }

    private async Task<InterceptorChainResult> ExecuteChainAsync(
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        int? timeoutMs,
        bool isSending,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new InterceptorChainResult
        {
            Event = @event,
            Phase = phase,
            Status = InterceptorChainStatus.Success
        };

        using var timeoutCts = timeoutMs.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        if (timeoutCts is not null)
        {
            timeoutCts.CancelAfter(timeoutMs!.Value);
        }

        var effectiveCt = timeoutCts?.Token ?? cancellationToken;

        try
        {
            // Get interceptors that handle this event and phase
            var applicableInterceptors = GetApplicableInterceptors(@event, phase);

            // Separate by type
            var mutations = applicableInterceptors
                .Where(i => i.ProtocolInterceptor.Type == InterceptorType.Mutation)
                .OrderBy(i => GetPriority(i, phase))
                .ThenBy(i => i.ProtocolInterceptor.Name)
                .ToList();

            var validations = applicableInterceptors
                .Where(i => i.ProtocolInterceptor.Type == InterceptorType.Validation)
                .ToList();

            var observability = applicableInterceptors
                .Where(i => i.ProtocolInterceptor.Type == InterceptorType.Observability)
                .ToList();

            JsonNode? currentPayload = payload;

            if (isSending)
            {
                // Sending: Mutate → Validate & Observe
                currentPayload = await ExecuteMutationsAsync(mutations, @event, phase, currentPayload, config, result, effectiveCt);
                if (result.Status != InterceptorChainStatus.Success)
                {
                    result.TotalDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                await ExecuteValidationsAndObservabilityAsync(validations, observability, @event, phase, currentPayload, config, result, effectiveCt);
            }
            else
            {
                // Receiving: Validate & Observe → Mutate
                await ExecuteValidationsAndObservabilityAsync(validations, observability, @event, phase, currentPayload, config, result, effectiveCt);
                if (result.Status != InterceptorChainStatus.Success)
                {
                    result.TotalDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                currentPayload = await ExecuteMutationsAsync(mutations, @event, phase, currentPayload, config, result, effectiveCt);
            }

            result.FinalPayload = currentPayload;
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
        {
            result.Status = InterceptorChainStatus.Timeout;
            result.AbortedAt = new ChainAbortInfo
            {
                Interceptor = "chain",
                Reason = "Chain execution timed out",
                Type = "timeout"
            };
        }

        result.TotalDurationMs = stopwatch.ElapsedMilliseconds;
        return result;
    }

    private IEnumerable<McpClientInterceptor> GetApplicableInterceptors(string @event, InterceptorPhase phase)
    {
        return _interceptors.Where(i =>
        {
            var proto = i.ProtocolInterceptor;

            // Check phase
            if (proto.Phase != InterceptorPhase.Both && proto.Phase != phase)
            {
                return false;
            }

            // Check event
            if (proto.Events.Count == 0)
            {
                return true; // No events specified means all events
            }

            return proto.Events.Any(e =>
                e == @event ||
                e == "*" ||
                (e == "*/request" && phase == InterceptorPhase.Request) ||
                (e == "*/response" && phase == InterceptorPhase.Response));
        });
    }

    private static int GetPriority(McpClientInterceptor interceptor, InterceptorPhase phase)
    {
        var hint = interceptor.ProtocolInterceptor.PriorityHint;
        if (hint is null)
        {
            return 0;
        }

        return hint.Value.GetPriorityForPhase(phase);
    }

    private async Task<JsonNode?> ExecuteMutationsAsync(
        List<McpClientInterceptor> mutations,
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        InterceptorChainResult chainResult,
        CancellationToken cancellationToken)
    {
        var currentPayload = payload;

        foreach (var interceptor in mutations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = CreateContext(interceptor, @event, phase, currentPayload, config);

            try
            {
                var result = await interceptor.InvokeAsync(context, cancellationToken);
                chainResult.Results.Add(result);

                if (result is MutationInterceptorResult mutationResult)
                {
                    if (mutationResult.Modified)
                    {
                        currentPayload = mutationResult.Payload;
                    }
                }
            }
            catch (Exception ex)
            {
                chainResult.Status = InterceptorChainStatus.MutationFailed;
                chainResult.AbortedAt = new ChainAbortInfo
                {
                    Interceptor = interceptor.ProtocolInterceptor.Name,
                    Reason = ex.Message,
                    Type = "mutation"
                };
                chainResult.FinalPayload = currentPayload; // Return last valid state
                return currentPayload;
            }
        }

        return currentPayload;
    }

    private async Task ExecuteValidationsAndObservabilityAsync(
        List<McpClientInterceptor> validations,
        List<McpClientInterceptor> observability,
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        InterceptorChainResult chainResult,
        CancellationToken cancellationToken)
    {
        // Execute validations and observability in parallel
        var allTasks = new List<Task<(McpClientInterceptor Interceptor, InterceptorResult Result, bool IsObservability)>>();

        foreach (var interceptor in validations)
        {
            allTasks.Add(ExecuteInterceptorAsync(interceptor, @event, phase, payload, config, isObservability: false, cancellationToken));
        }

        foreach (var interceptor in observability)
        {
            allTasks.Add(ExecuteInterceptorAsync(interceptor, @event, phase, payload, config, isObservability: true, cancellationToken));
        }

        var results = await Task.WhenAll(allTasks);

        foreach (var (interceptor, result, isObservability) in results)
        {
            chainResult.Results.Add(result);

            if (result is ValidationInterceptorResult validationResult)
            {
                // Update validation summary
                if (validationResult.Messages is not null)
                {
                    foreach (var msg in validationResult.Messages)
                    {
                        switch (msg.Severity)
                        {
                            case ValidationSeverity.Error:
                                chainResult.ValidationSummary.Errors++;
                                break;
                            case ValidationSeverity.Warn:
                                chainResult.ValidationSummary.Warnings++;
                                break;
                            case ValidationSeverity.Info:
                                chainResult.ValidationSummary.Infos++;
                                break;
                        }
                    }
                }

                // Check for blocking errors
                if (!validationResult.Valid && validationResult.Severity == ValidationSeverity.Error)
                {
                    chainResult.Status = InterceptorChainStatus.ValidationFailed;
                    chainResult.AbortedAt = new ChainAbortInfo
                    {
                        Interceptor = interceptor.ProtocolInterceptor.Name,
                        Reason = validationResult.Messages?.FirstOrDefault()?.Message ?? "Validation failed",
                        Type = "validation"
                    };
                }
            }

            // Observability failures are logged but never block (fire-and-forget behavior)
        }
    }

    private async Task<(McpClientInterceptor Interceptor, InterceptorResult Result, bool IsObservability)> ExecuteInterceptorAsync(
        McpClientInterceptor interceptor,
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        bool isObservability,
        CancellationToken cancellationToken)
    {
        var context = CreateContext(interceptor, @event, phase, payload, config);

        try
        {
            var result = await interceptor.InvokeAsync(context, cancellationToken);
            return (interceptor, result, isObservability);
        }
        catch (Exception ex)
        {
            // For observability, failures are logged but don't affect the result
            if (isObservability)
            {
                return (interceptor, new ObservabilityInterceptorResult
                {
                    Interceptor = interceptor.ProtocolInterceptor.Name,
                    Phase = phase,
                    Observed = false,
                    Info = new JsonObject { ["error"] = ex.Message }
                }, true);
            }

            // For validations, return an error result
            return (interceptor, new ValidationInterceptorResult
            {
                Interceptor = interceptor.ProtocolInterceptor.Name,
                Phase = phase,
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = ex.Message, Severity = ValidationSeverity.Error }]
            }, false);
        }
    }

    private ClientInterceptorContext<InvokeInterceptorRequestParams> CreateContext(
        McpClientInterceptor interceptor,
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config)
    {
        return new ClientInterceptorContext<InvokeInterceptorRequestParams>
        {
            Services = _services,
            MatchedInterceptor = interceptor,
            Params = new InvokeInterceptorRequestParams
            {
                Name = interceptor.ProtocolInterceptor.Name,
                Event = @event,
                Phase = phase,
                Payload = payload!,
                Config = config?.TryGetValue(interceptor.ProtocolInterceptor.Name, out var interceptorConfig) == true
                    ? interceptorConfig
                    : null
            }
        };
    }
}
