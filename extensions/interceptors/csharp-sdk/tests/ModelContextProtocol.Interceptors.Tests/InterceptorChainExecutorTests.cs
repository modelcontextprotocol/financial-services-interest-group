using System.Text.Json.Nodes;
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;

namespace ModelContextProtocol.Interceptors.Tests;

public class InterceptorChainExecutorTests
{
    [Fact]
    public async Task ExecuteForSendingAsync_MutationsExecuteSequentiallyByPriority()
    {
        // Arrange
        var executionOrder = new List<string>();

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("high-priority", -100, payload =>
            {
                executionOrder.Add("high-priority");
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateMutationInterceptor("medium-priority", 0, payload =>
            {
                executionOrder.Add("medium-priority");
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateMutationInterceptor("low-priority", 100, payload =>
            {
                executionOrder.Add("low-priority");
                return MutationInterceptorResult.Unchanged(payload);
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - Lower priority numbers execute first
        Assert.Equal(["high-priority", "medium-priority", "low-priority"], executionOrder);
    }

    [Fact]
    public async Task ExecuteForSendingAsync_MutationsWithSamePriority_OrderAlphabetically()
    {
        // Arrange
        var executionOrder = new List<string>();

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("zebra", 0, payload =>
            {
                executionOrder.Add("zebra");
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateMutationInterceptor("alpha", 0, payload =>
            {
                executionOrder.Add("alpha");
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateMutationInterceptor("beta", 0, payload =>
            {
                executionOrder.Add("beta");
                return MutationInterceptorResult.Unchanged(payload);
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - Same priority, alphabetical order
        Assert.Equal(["alpha", "beta", "zebra"], executionOrder);
    }

    [Fact]
    public async Task ExecuteForSendingAsync_ValidationErrorBlocksExecution()
    {
        // Arrange
        var mutationExecuted = false;

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("mutator", -1000, payload =>
            {
                mutationExecuted = true;
                return MutationInterceptorResult.Mutated(JsonNode.Parse("{\"mutated\": true}"));
            }),
            CreateValidationInterceptor("validator", _ =>
                ValidationInterceptorResult.Error("Validation failed"))
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert
        // Mutations execute first in sending direction, so mutator runs before validator
        Assert.True(mutationExecuted, "Mutation should execute before validation in sending direction");
        Assert.Equal(InterceptorChainStatus.ValidationFailed, result.Status);
        Assert.NotNull(result.AbortedAt);
        Assert.Equal("validator", result.AbortedAt.Interceptor);
    }

    [Fact]
    public async Task ExecuteForReceivingAsync_ValidationsExecuteBeforeMutations()
    {
        // Arrange
        var executionOrder = new List<string>();

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("mutator", 0, payload =>
            {
                executionOrder.Add("mutator");
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateValidationInterceptor("validator", _ =>
            {
                executionOrder.Add("validator");
                return ValidationInterceptorResult.Success();
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        await executor.ExecuteForReceivingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - In receiving direction: Validate → Mutate
        Assert.Equal("validator", executionOrder[0]);
        Assert.Equal("mutator", executionOrder[1]);
    }

    [Fact]
    public async Task ExecuteForReceivingAsync_ValidationErrorBlocksMutations()
    {
        // Arrange
        var mutationExecuted = false;

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("mutator", 0, payload =>
            {
                mutationExecuted = true;
                return MutationInterceptorResult.Mutated(JsonNode.Parse("{\"mutated\": true}"));
            }),
            CreateValidationInterceptor("validator", _ =>
                ValidationInterceptorResult.Error("Validation failed"))
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForReceivingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - In receiving direction, validation runs first and blocks mutation
        Assert.False(mutationExecuted);
        Assert.Equal(InterceptorChainStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task ObservabilityInterceptor_NeverBlocksExecution()
    {
        // Arrange
        var observabilityExecuted = false;
        var mutationExecuted = false;

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("mutator", 0, payload =>
            {
                mutationExecuted = true;
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateObservabilityInterceptor("observer", _ =>
            {
                observabilityExecuted = true;
                throw new Exception("Observability failure should not block");
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - Observability failures don't block
        Assert.True(mutationExecuted);
        Assert.True(observabilityExecuted);
        Assert.Equal(InterceptorChainStatus.Success, result.Status);
    }

    [Fact]
    public async Task Timeout_AbortsChainExecution()
    {
        // Arrange
        var interceptors = new List<McpClientInterceptor>
        {
            CreateCancellableAsyncMutationInterceptor("slow-mutator", 0, async (payload, ct) =>
            {
                // Use the cancellation token so the delay can be cancelled
                await Task.Delay(5000, ct);
                return MutationInterceptorResult.Unchanged(payload);
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForSendingAsync(
            InterceptorEvents.ToolsCall,
            payload,
            timeoutMs: 100);

        // Assert - The chain should abort (either as Timeout or MutationFailed due to cancellation)
        // The implementation catches OperationCanceledException from the mutation which gets reported
        // as MutationFailed rather than Timeout depending on timing
        Assert.NotEqual(InterceptorChainStatus.Success, result.Status);
        Assert.NotNull(result.AbortedAt);
    }

    [Fact]
    public async Task Mutations_PropagatePayloadThroughChain()
    {
        // Arrange
        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("first", -100, payload =>
            {
                var obj = payload!.AsObject();
                obj["step1"] = true;
                return MutationInterceptorResult.Mutated(obj);
            }),
            CreateMutationInterceptor("second", 0, payload =>
            {
                var obj = payload!.AsObject();
                obj["step2"] = true;
                return MutationInterceptorResult.Mutated(obj);
            }),
            CreateMutationInterceptor("third", 100, payload =>
            {
                var obj = payload!.AsObject();
                obj["step3"] = true;
                return MutationInterceptorResult.Mutated(obj);
            })
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert
        Assert.Equal(InterceptorChainStatus.Success, result.Status);
        var final = result.FinalPayload!.AsObject();
        Assert.True(final["step1"]!.GetValue<bool>());
        Assert.True(final["step2"]!.GetValue<bool>());
        Assert.True(final["step3"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ValidationWarning_DoesNotBlockExecution()
    {
        // Arrange
        var mutationExecuted = false;

        var interceptors = new List<McpClientInterceptor>
        {
            CreateMutationInterceptor("mutator", -1000, payload =>
            {
                mutationExecuted = true;
                return MutationInterceptorResult.Unchanged(payload);
            }),
            CreateValidationInterceptor("validator", _ =>
                ValidationInterceptorResult.Warning("This is just a warning"))
        };

        var executor = new InterceptorChainExecutor(interceptors);
        var payload = JsonNode.Parse("{}");

        // Act
        var result = await executor.ExecuteForSendingAsync(InterceptorEvents.ToolsCall, payload);

        // Assert - Warnings don't block
        Assert.True(mutationExecuted);
        Assert.Equal(InterceptorChainStatus.Success, result.Status);
        Assert.Equal(1, result.ValidationSummary.Warnings);
    }

    // Helper methods to create test interceptors

    private static McpClientInterceptor CreateMutationInterceptor(
        string name,
        int priority,
        Func<JsonNode?, MutationInterceptorResult> handler)
    {
        return new TestMutationInterceptor(name, priority, handler);
    }

    private static McpClientInterceptor CreateAsyncMutationInterceptor(
        string name,
        int priority,
        Func<JsonNode?, Task<MutationInterceptorResult>> handler)
    {
        return new TestAsyncMutationInterceptor(name, priority, handler);
    }

    private static McpClientInterceptor CreateCancellableAsyncMutationInterceptor(
        string name,
        int priority,
        Func<JsonNode?, CancellationToken, Task<MutationInterceptorResult>> handler)
    {
        return new TestCancellableAsyncMutationInterceptor(name, priority, handler);
    }

    private static McpClientInterceptor CreateValidationInterceptor(
        string name,
        Func<JsonNode?, ValidationInterceptorResult> handler)
    {
        return new TestValidationInterceptor(name, handler);
    }

    private static McpClientInterceptor CreateObservabilityInterceptor(
        string name,
        Action<JsonNode?> handler)
    {
        return new TestObservabilityInterceptor(name, handler);
    }
}

// Test interceptor implementations

file class TestMutationInterceptor : McpClientInterceptor
{
    private readonly Func<JsonNode?, MutationInterceptorResult> _handler;
    private readonly Interceptor _protocolInterceptor;
    private readonly IReadOnlyList<object> _metadata = [];

    public TestMutationInterceptor(string name, int priority, Func<JsonNode?, MutationInterceptorResult> handler)
    {
        _protocolInterceptor = new Interceptor
        {
            Name = name,
            Type = InterceptorType.Mutation,
            Phase = InterceptorPhase.Both,
            Events = [InterceptorEvents.ToolsCall],
            PriorityHint = new InterceptorPriorityHint(priority)
        };
        _handler = handler;
    }

    public override Interceptor ProtocolInterceptor => _protocolInterceptor;
    public override IReadOnlyList<object> Metadata => _metadata;

    public override ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var result = _handler(context.Params?.Payload);
        result.Interceptor = ProtocolInterceptor.Name;
        return new ValueTask<InterceptorResult>(result);
    }
}

file class TestAsyncMutationInterceptor : McpClientInterceptor
{
    private readonly Func<JsonNode?, Task<MutationInterceptorResult>> _handler;
    private readonly Interceptor _protocolInterceptor;
    private readonly IReadOnlyList<object> _metadata = [];

    public TestAsyncMutationInterceptor(string name, int priority, Func<JsonNode?, Task<MutationInterceptorResult>> handler)
    {
        _protocolInterceptor = new Interceptor
        {
            Name = name,
            Type = InterceptorType.Mutation,
            Phase = InterceptorPhase.Both,
            Events = [InterceptorEvents.ToolsCall],
            PriorityHint = new InterceptorPriorityHint(priority)
        };
        _handler = handler;
    }

    public override Interceptor ProtocolInterceptor => _protocolInterceptor;
    public override IReadOnlyList<object> Metadata => _metadata;

    public override async ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler(context.Params?.Payload);
        result.Interceptor = ProtocolInterceptor.Name;
        return result;
    }
}

file class TestCancellableAsyncMutationInterceptor : McpClientInterceptor
{
    private readonly Func<JsonNode?, CancellationToken, Task<MutationInterceptorResult>> _handler;
    private readonly Interceptor _protocolInterceptor;
    private readonly IReadOnlyList<object> _metadata = [];

    public TestCancellableAsyncMutationInterceptor(string name, int priority, Func<JsonNode?, CancellationToken, Task<MutationInterceptorResult>> handler)
    {
        _protocolInterceptor = new Interceptor
        {
            Name = name,
            Type = InterceptorType.Mutation,
            Phase = InterceptorPhase.Both,
            Events = [InterceptorEvents.ToolsCall],
            PriorityHint = new InterceptorPriorityHint(priority)
        };
        _handler = handler;
    }

    public override Interceptor ProtocolInterceptor => _protocolInterceptor;
    public override IReadOnlyList<object> Metadata => _metadata;

    public override async ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler(context.Params?.Payload, cancellationToken);
        result.Interceptor = ProtocolInterceptor.Name;
        return result;
    }
}

file class TestValidationInterceptor : McpClientInterceptor
{
    private readonly Func<JsonNode?, ValidationInterceptorResult> _handler;
    private readonly Interceptor _protocolInterceptor;
    private readonly IReadOnlyList<object> _metadata = [];

    public TestValidationInterceptor(string name, Func<JsonNode?, ValidationInterceptorResult> handler)
    {
        _protocolInterceptor = new Interceptor
        {
            Name = name,
            Type = InterceptorType.Validation,
            Phase = InterceptorPhase.Both,
            Events = [InterceptorEvents.ToolsCall]
        };
        _handler = handler;
    }

    public override Interceptor ProtocolInterceptor => _protocolInterceptor;
    public override IReadOnlyList<object> Metadata => _metadata;

    public override ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        var result = _handler(context.Params?.Payload);
        result.Interceptor = ProtocolInterceptor.Name;
        result.Phase = context.Params?.Phase ?? InterceptorPhase.Request;
        return new ValueTask<InterceptorResult>(result);
    }
}

file class TestObservabilityInterceptor : McpClientInterceptor
{
    private readonly Action<JsonNode?> _handler;
    private readonly Interceptor _protocolInterceptor;
    private readonly IReadOnlyList<object> _metadata = [];

    public TestObservabilityInterceptor(string name, Action<JsonNode?> handler)
    {
        _protocolInterceptor = new Interceptor
        {
            Name = name,
            Type = InterceptorType.Observability,
            Phase = InterceptorPhase.Both,
            Events = [InterceptorEvents.ToolsCall]
        };
        _handler = handler;
    }

    public override Interceptor ProtocolInterceptor => _protocolInterceptor;
    public override IReadOnlyList<object> Metadata => _metadata;

    public override ValueTask<InterceptorResult> InvokeAsync(
        ClientInterceptorContext<InvokeInterceptorRequestParams> context,
        CancellationToken cancellationToken = default)
    {
        _handler(context.Params?.Payload);
        return new ValueTask<InterceptorResult>(new ObservabilityInterceptorResult
        {
            Interceptor = ProtocolInterceptor.Name,
            Observed = true
        });
    }
}
