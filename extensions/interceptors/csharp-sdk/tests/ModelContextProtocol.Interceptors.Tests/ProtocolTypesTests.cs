using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Protocol.Llm;

namespace ModelContextProtocol.Interceptors.Tests;

public class ProtocolTypesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region InterceptorType Serialization

    [Theory]
    [InlineData(InterceptorType.Validation, "\"validation\"")]
    [InlineData(InterceptorType.Mutation, "\"mutation\"")]
    [InlineData(InterceptorType.Observability, "\"observability\"")]
    public void InterceptorType_SerializesToCorrectJsonString(InterceptorType type, string expected)
    {
        var json = JsonSerializer.Serialize(type, JsonOptions);
        Assert.Equal(expected, json);
    }

    [Theory]
    [InlineData("\"validation\"", InterceptorType.Validation)]
    [InlineData("\"mutation\"", InterceptorType.Mutation)]
    [InlineData("\"observability\"", InterceptorType.Observability)]
    public void InterceptorType_DeserializesFromJsonString(string json, InterceptorType expected)
    {
        var type = JsonSerializer.Deserialize<InterceptorType>(json, JsonOptions);
        Assert.Equal(expected, type);
    }

    #endregion

    #region InterceptorPhase Serialization

    [Theory]
    [InlineData(InterceptorPhase.Request, "\"request\"")]
    [InlineData(InterceptorPhase.Response, "\"response\"")]
    [InlineData(InterceptorPhase.Both, "\"both\"")]
    public void InterceptorPhase_SerializesToCorrectJsonString(InterceptorPhase phase, string expected)
    {
        var json = JsonSerializer.Serialize(phase, JsonOptions);
        Assert.Equal(expected, json);
    }

    #endregion

    #region InterceptorPriorityHint Serialization

    [Fact]
    public void InterceptorPriorityHint_SerializesAsNumber_WhenBothPhasesEqual()
    {
        var hint = new InterceptorPriorityHint(100);
        var json = JsonSerializer.Serialize(hint, JsonOptions);
        Assert.Equal("100", json);
    }

    [Fact]
    public void InterceptorPriorityHint_SerializesAsObject_WhenPhasesDiffer()
    {
        var hint = new InterceptorPriorityHint(-100, 50);
        var json = JsonSerializer.Serialize(hint, JsonOptions);

        var obj = JsonSerializer.Deserialize<JsonObject>(json);
        Assert.NotNull(obj);
        Assert.Equal(-100, obj["request"]!.GetValue<int>());
        Assert.Equal(50, obj["response"]!.GetValue<int>());
    }

    [Fact]
    public void InterceptorPriorityHint_DeserializesFromNumber()
    {
        var hint = JsonSerializer.Deserialize<InterceptorPriorityHint>("100", JsonOptions);
        Assert.Equal(100, hint.GetPriorityForPhase(InterceptorPhase.Request));
        Assert.Equal(100, hint.GetPriorityForPhase(InterceptorPhase.Response));
    }

    [Fact]
    public void InterceptorPriorityHint_DeserializesFromObject()
    {
        var json = """{"request": -100, "response": 50}""";
        var hint = JsonSerializer.Deserialize<InterceptorPriorityHint>(json, JsonOptions);
        Assert.Equal(-100, hint.GetPriorityForPhase(InterceptorPhase.Request));
        Assert.Equal(50, hint.GetPriorityForPhase(InterceptorPhase.Response));
    }

    #endregion

    #region Interceptor Serialization

    [Fact]
    public void Interceptor_SerializesWithAllFields()
    {
        var interceptor = new Interceptor
        {
            Name = "test-interceptor",
            Version = "1.0.0",
            Description = "A test interceptor",
            Events = [InterceptorEvents.ToolsCall, InterceptorEvents.LlmCompletion],
            Type = InterceptorType.Validation,
            Phase = InterceptorPhase.Request,
            PriorityHint = new InterceptorPriorityHint(100)
        };

        var json = JsonSerializer.Serialize(interceptor, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Interceptor>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("test-interceptor", deserialized.Name);
        Assert.Equal("1.0.0", deserialized.Version);
        Assert.Equal("A test interceptor", deserialized.Description);
        Assert.Contains(InterceptorEvents.ToolsCall, deserialized.Events);
        Assert.Contains(InterceptorEvents.LlmCompletion, deserialized.Events);
        Assert.Equal(InterceptorType.Validation, deserialized.Type);
        Assert.Equal(InterceptorPhase.Request, deserialized.Phase);
        Assert.NotNull(deserialized.PriorityHint);
        Assert.Equal(100, deserialized.PriorityHint.Value.GetPriorityForPhase(InterceptorPhase.Request));
    }

    #endregion

    #region ValidationInterceptorResult Serialization

    [Fact]
    public void ValidationInterceptorResult_SerializesCorrectly()
    {
        var result = new ValidationInterceptorResult
        {
            Valid = false,
            Severity = ValidationSeverity.Error,
            Messages =
            [
                new() { Path = "$.name", Message = "Name is required", Severity = ValidationSeverity.Error }
            ],
            Suggestions =
            [
                new() { Path = "$.name", Value = JsonNode.Parse("\"default\"") }
            ]
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ValidationInterceptorResult>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.False(deserialized.Valid);
        Assert.Equal(ValidationSeverity.Error, deserialized.Severity);
        Assert.Single(deserialized.Messages!);
        Assert.Equal("$.name", deserialized.Messages![0].Path);
        Assert.Single(deserialized.Suggestions!);
    }

    #endregion

    #region MutationInterceptorResult Serialization

    [Fact]
    public void MutationInterceptorResult_SerializesCorrectly()
    {
        var result = MutationInterceptorResult.Mutated(JsonNode.Parse("{\"modified\": true}"));

        var json = JsonSerializer.Serialize(result, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<MutationInterceptorResult>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Modified);
        Assert.NotNull(deserialized.Payload);
        Assert.True(deserialized.Payload["modified"]!.GetValue<bool>());
    }

    #endregion

    #region InterceptorChainResult Serialization

    [Fact]
    public void InterceptorChainStatus_SerializesCorrectly()
    {
        // Test chain status enum serialization
        Assert.Equal("\"success\"", JsonSerializer.Serialize(InterceptorChainStatus.Success, JsonOptions));
        Assert.Equal("\"validation_failed\"", JsonSerializer.Serialize(InterceptorChainStatus.ValidationFailed, JsonOptions));
        Assert.Equal("\"mutation_failed\"", JsonSerializer.Serialize(InterceptorChainStatus.MutationFailed, JsonOptions));
        Assert.Equal("\"timeout\"", JsonSerializer.Serialize(InterceptorChainStatus.Timeout, JsonOptions));
    }

    [Fact]
    public void ValidationSummary_SerializesCorrectly()
    {
        var summary = new ValidationSummary { Errors = 1, Warnings = 2, Infos = 3 };
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ValidationSummary>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.Errors);
        Assert.Equal(2, deserialized.Warnings);
        Assert.Equal(3, deserialized.Infos);
    }

    [Fact]
    public void ChainAbortInfo_SerializesCorrectly()
    {
        var abortInfo = new ChainAbortInfo
        {
            Interceptor = "pii-validator",
            Reason = "PII detected in request",
            Type = "validation"
        };

        var json = JsonSerializer.Serialize(abortInfo, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChainAbortInfo>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("pii-validator", deserialized.Interceptor);
        Assert.Equal("PII detected in request", deserialized.Reason);
        Assert.Equal("validation", deserialized.Type);
    }

    #endregion

    #region LlmCompletionRequest Serialization

    [Fact]
    public void LlmCompletionRequest_SerializesCorrectly()
    {
        var request = new LlmCompletionRequest
        {
            Model = "gpt-4",
            Messages =
            [
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Hello!")
            ],
            Temperature = 0.7,
            MaxTokens = 1000,
            TopP = 0.9
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LlmCompletionRequest>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("gpt-4", deserialized.Model);
        Assert.Equal(2, deserialized.Messages.Count);
        Assert.Equal(LlmMessageRole.System, deserialized.Messages[0].Role);
        Assert.Equal("You are a helpful assistant.", deserialized.Messages[0].Content);
        Assert.Equal(LlmMessageRole.User, deserialized.Messages[1].Role);
        Assert.Equal(0.7, deserialized.Temperature);
        Assert.Equal(1000, deserialized.MaxTokens);
    }

    [Fact]
    public void LlmMessage_ToolMessage_SerializesCorrectly()
    {
        var message = LlmMessage.Tool("call_abc123", "{\"result\": \"success\"}", "get_weather");

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LlmMessage>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(LlmMessageRole.Tool, deserialized.Role);
        Assert.Equal("call_abc123", deserialized.ToolCallId);
        Assert.Equal("{\"result\": \"success\"}", deserialized.Content);
        Assert.Equal("get_weather", deserialized.Name);
    }

    [Fact]
    public void LlmMessage_AssistantWithToolCalls_SerializesCorrectly()
    {
        var message = LlmMessage.Assistant(null, [
            new LlmToolCall
            {
                Id = "call_abc123",
                Type = "function",
                Function = new LlmFunctionCall
                {
                    Name = "get_weather",
                    Arguments = "{\"location\": \"NYC\"}"
                }
            }
        ]);

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LlmMessage>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(LlmMessageRole.Assistant, deserialized.Role);
        Assert.NotNull(deserialized.ToolCalls);
        Assert.Single(deserialized.ToolCalls);
        Assert.Equal("call_abc123", deserialized.ToolCalls[0].Id);
        Assert.Equal("get_weather", deserialized.ToolCalls[0].Function!.Name);
    }

    #endregion

    #region InterceptorEvents Constants

    [Fact]
    public void InterceptorEvents_HasAllRequiredEvents()
    {
        // Server features
        Assert.Equal("tools/list", InterceptorEvents.ToolsList);
        Assert.Equal("tools/call", InterceptorEvents.ToolsCall);
        Assert.Equal("prompts/list", InterceptorEvents.PromptsList);
        Assert.Equal("prompts/get", InterceptorEvents.PromptsGet);
        Assert.Equal("resources/list", InterceptorEvents.ResourcesList);
        Assert.Equal("resources/read", InterceptorEvents.ResourcesRead);
        Assert.Equal("resources/subscribe", InterceptorEvents.ResourcesSubscribe);

        // Client features
        Assert.Equal("sampling/createMessage", InterceptorEvents.SamplingCreateMessage);
        Assert.Equal("elicitation/create", InterceptorEvents.ElicitationCreate);
        Assert.Equal("roots/list", InterceptorEvents.RootsList);

        // LLM interactions
        Assert.Equal("llm/completion", InterceptorEvents.LlmCompletion);

        // Wildcards
        Assert.Equal("*/request", InterceptorEvents.AllRequests);
        Assert.Equal("*/response", InterceptorEvents.AllResponses);
        Assert.Equal("*", InterceptorEvents.All);
    }

    #endregion
}
