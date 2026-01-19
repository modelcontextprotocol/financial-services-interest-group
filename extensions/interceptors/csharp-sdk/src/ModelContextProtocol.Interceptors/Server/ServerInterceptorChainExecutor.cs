using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Executes server interceptor chains for demonstration purposes.
/// This executor directly invokes interceptor methods to demonstrate the full
/// interceptor patterns including mutations and observability, bypassing the
/// MCP protocol layer which only returns ValidationInterceptorResult.
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
public class ServerInterceptorChainExecutor
{
    private readonly IReadOnlyList<McpServerInterceptor> _interceptors;
    private readonly IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerInterceptorChainExecutor"/> class.
    /// </summary>
    /// <param name="interceptors">The interceptors to execute.</param>
    /// <param name="services">Optional service provider for dependency injection.</param>
    public ServerInterceptorChainExecutor(IEnumerable<McpServerInterceptor> interceptors, IServiceProvider? services = null)
    {
        Throw.IfNull(interceptors);

        _interceptors = interceptors.ToList();
        _services = services;
    }

    /// <summary>
    /// Executes the interceptor chain for outgoing data (sending across trust boundary).
    /// </summary>
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

    private IEnumerable<McpServerInterceptor> GetApplicableInterceptors(string @event, InterceptorPhase phase)
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

    private static int GetPriority(McpServerInterceptor interceptor, InterceptorPhase phase)
    {
        var hint = interceptor.ProtocolInterceptor.PriorityHint;
        if (hint is null)
        {
            return 0;
        }

        return hint.Value.GetPriorityForPhase(phase);
    }

    private async Task<JsonNode?> ExecuteMutationsAsync(
        List<McpServerInterceptor> mutations,
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

            try
            {
                var interceptorResult = await InvokeInterceptorDirectlyAsync(interceptor, currentPayload, config, cancellationToken);
                chainResult.Results.Add(interceptorResult);

                if (interceptorResult is MutationInterceptorResult mutationResult)
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
                chainResult.FinalPayload = currentPayload;
                return currentPayload;
            }
        }

        return currentPayload;
    }

    private async Task ExecuteValidationsAndObservabilityAsync(
        List<McpServerInterceptor> validations,
        List<McpServerInterceptor> observability,
        string @event,
        InterceptorPhase phase,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        InterceptorChainResult chainResult,
        CancellationToken cancellationToken)
    {
        var allTasks = new List<Task<(McpServerInterceptor Interceptor, InterceptorResult Result, bool IsObservability)>>();

        foreach (var interceptor in validations)
        {
            allTasks.Add(ExecuteInterceptorAsync(interceptor, payload, config, isObservability: false, cancellationToken));
        }

        foreach (var interceptor in observability)
        {
            allTasks.Add(ExecuteInterceptorAsync(interceptor, payload, config, isObservability: true, cancellationToken));
        }

        var results = await Task.WhenAll(allTasks);

        foreach (var (interceptor, interceptorResult, isObservability) in results)
        {
            chainResult.Results.Add(interceptorResult);

            if (interceptorResult is ValidationInterceptorResult validationResult)
            {
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
        }
    }

    private async Task<(McpServerInterceptor Interceptor, InterceptorResult Result, bool IsObservability)> ExecuteInterceptorAsync(
        McpServerInterceptor interceptor,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        bool isObservability,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await InvokeInterceptorDirectlyAsync(interceptor, payload, config, cancellationToken);
            return (interceptor, result, isObservability);
        }
        catch (Exception ex)
        {
            if (isObservability)
            {
                return (interceptor, new ObservabilityInterceptorResult
                {
                    Interceptor = interceptor.ProtocolInterceptor.Name,
                    Phase = interceptor.ProtocolInterceptor.Phase,
                    Observed = false,
                    Info = new JsonObject { ["error"] = ex.Message }
                }, true);
            }

            return (interceptor, new ValidationInterceptorResult
            {
                Interceptor = interceptor.ProtocolInterceptor.Name,
                Phase = interceptor.ProtocolInterceptor.Phase,
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = ex.Message, Severity = ValidationSeverity.Error }]
            }, false);
        }
    }

    /// <summary>
    /// Invokes the interceptor's underlying method directly, bypassing the MCP protocol layer.
    /// This allows getting the actual result type (Mutation, Observability, Validation).
    /// </summary>
    private async Task<InterceptorResult> InvokeInterceptorDirectlyAsync(
        McpServerInterceptor interceptor,
        JsonNode? payload,
        IDictionary<string, JsonNode>? config,
        CancellationToken cancellationToken)
    {
        // Get the underlying method via reflection from the McpServerInterceptor
        // The McpServerInterceptor has its method stored - we need to access it
        var interceptorType = interceptor.GetType();
        
        // Try to get the method field from ReflectionMcpServerInterceptor
        var methodField = interceptorType.GetField("_method", BindingFlags.NonPublic | BindingFlags.Instance);
        var targetField = interceptorType.GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (methodField is null || targetField is null)
        {
            // Cannot access underlying method - return a valid but minimal result
            return new ValidationInterceptorResult
            {
                Interceptor = interceptor.ProtocolInterceptor.Name,
                Phase = interceptor.ProtocolInterceptor.Phase,
                Valid = true
            };
        }

        var method = (MethodInfo?)methodField.GetValue(interceptor);
        var target = targetField.GetValue(interceptor);

        if (method is null)
        {
            // Cannot access underlying method - return a valid but minimal result
            return new ValidationInterceptorResult
            {
                Interceptor = interceptor.ProtocolInterceptor.Name,
                Phase = interceptor.ProtocolInterceptor.Phase,
                Valid = true
            };
        }

        // Build parameters
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            args[i] = BindParameter(param, payload, config, cancellationToken);
        }

        // Invoke the method
        var result = method.Invoke(target, args);

        // Handle async results
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            result = GetTaskResult(task);
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            result = null;
        }
        else if (result is ValueTask<ValidationInterceptorResult> vtValidation)
        {
            result = await vtValidation.ConfigureAwait(false);
        }
        else if (result is ValueTask<MutationInterceptorResult> vtMutation)
        {
            result = await vtMutation.ConfigureAwait(false);
        }
        else if (result is ValueTask<ObservabilityInterceptorResult> vtObservability)
        {
            result = await vtObservability.ConfigureAwait(false);
        }

        // Convert to InterceptorResult
        if (result is InterceptorResult interceptorResult)
        {
            interceptorResult.Interceptor ??= interceptor.ProtocolInterceptor.Name;
            return interceptorResult;
        }

        if (result is bool isValid)
        {
            return new ValidationInterceptorResult
            {
                Interceptor = interceptor.ProtocolInterceptor.Name,
                Phase = interceptor.ProtocolInterceptor.Phase,
                Valid = isValid,
                Severity = isValid ? null : ValidationSeverity.Error
            };
        }

        return new ValidationInterceptorResult
        {
            Interceptor = interceptor.ProtocolInterceptor.Name,
            Phase = interceptor.ProtocolInterceptor.Phase,
            Valid = true
        };
    }

    private object? BindParameter(ParameterInfo param, JsonNode? payload, IDictionary<string, JsonNode>? config, CancellationToken cancellationToken)
    {
        var paramType = param.ParameterType;
        var paramName = param.Name?.ToLowerInvariant();

        if (paramType == typeof(CancellationToken))
        {
            return cancellationToken;
        }

        if (paramType == typeof(IServiceProvider))
        {
            return _services;
        }

        if (paramType == typeof(JsonNode) && paramName is "payload")
        {
            return payload;
        }

        if (paramType == typeof(JsonNode) && paramName is "config")
        {
            return config is not null ? JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(config)) : null;
        }

        if (_services is not null)
        {
            var service = _services.GetService(paramType);
            if (service is not null)
            {
                return service;
            }
        }

        if (param.HasDefaultValue)
        {
            return param.DefaultValue;
        }

        return null;
    }

    private static object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();
        if (taskType.IsGenericType)
        {
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        return null;
    }

}
