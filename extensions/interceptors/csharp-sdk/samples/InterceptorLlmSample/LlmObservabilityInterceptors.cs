using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Interceptors.Protocol.Llm;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Observability interceptors for llm/completion events.
/// These interceptors log and monitor LLM requests/responses.
/// </summary>
[McpClientInterceptorType]
public partial class LlmObservabilityInterceptors
{
    /// <summary>
    /// Logs LLM request details for monitoring and debugging.
    /// </summary>
    [McpClientInterceptor(
        Name = "request-logger",
        Description = "Logs LLM request details",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request)]
    public static ObservabilityInterceptorResult LogRequest(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        // In a real implementation, this would send to a logging service
        // Here we just capture the metrics
        var messageCount = request.Messages.Count;
        var estimatedTokens = request.Messages.Sum(m => (m.Content?.Length ?? 0) / 4);
        var hasTools = request.Tools?.Count > 0;

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = "llm_request",
                ["model"] = request.Model,
                ["messageCount"] = messageCount,
                ["estimatedPromptTokens"] = estimatedTokens,
                ["maxTokens"] = request.MaxTokens,
                ["temperature"] = request.Temperature,
                ["hasTools"] = hasTools,
                ["toolCount"] = request.Tools?.Count ?? 0,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Logs LLM response details including token usage.
    /// </summary>
    [McpClientInterceptor(
        Name = "response-logger",
        Description = "Logs LLM response details and usage metrics",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response)]
    public static ObservabilityInterceptorResult LogResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var response = payload.Deserialize<LlmCompletionResponse>();
        if (response is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var hasToolCalls = response.Choices.Any(c => c.Message.ToolCalls?.Count > 0);
        var finishReasons = response.Choices.Select(c => c.FinishReason?.ToString() ?? "unknown").ToArray();

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = "llm_response",
                ["id"] = response.Id,
                ["model"] = response.Model,
                ["choiceCount"] = response.Choices.Count,
                ["finishReasons"] = JsonNode.Parse($"[\"{string.Join("\", \"", finishReasons)}\"]"),
                ["hasToolCalls"] = hasToolCalls,
                ["promptTokens"] = response.Usage?.PromptTokens,
                ["completionTokens"] = response.Usage?.CompletionTokens,
                ["totalTokens"] = response.Usage?.TotalTokens,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Tracks estimated costs based on token usage.
    /// </summary>
    [McpClientInterceptor(
        Name = "cost-tracker",
        Description = "Tracks estimated API costs based on token usage",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response)]
    public static ObservabilityInterceptorResult TrackCosts(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var response = payload.Deserialize<LlmCompletionResponse>();
        if (response?.Usage is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        // Approximate GPT-4 pricing (example only, not accurate)
        const decimal PromptCostPer1k = 0.03m;
        const decimal CompletionCostPer1k = 0.06m;

        var promptCost = (response.Usage.PromptTokens / 1000.0m) * PromptCostPer1k;
        var completionCost = (response.Usage.CompletionTokens / 1000.0m) * CompletionCostPer1k;
        var totalCost = promptCost + completionCost;

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["promptTokens"] = response.Usage.PromptTokens,
                ["completionTokens"] = response.Usage.CompletionTokens,
                ["estimatedCostUsd"] = (double)totalCost
            },
            Info = new JsonObject
            {
                ["event"] = "cost_tracking",
                ["model"] = response.Model,
                ["promptTokens"] = response.Usage.PromptTokens,
                ["completionTokens"] = response.Usage.CompletionTokens,
                ["estimatedPromptCostUsd"] = (double)promptCost,
                ["estimatedCompletionCostUsd"] = (double)completionCost,
                ["estimatedTotalCostUsd"] = (double)totalCost,
                ["currency"] = "USD",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }
}
