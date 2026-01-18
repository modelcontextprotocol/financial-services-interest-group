using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Protocol.Llm;
using ModelContextProtocol.Interceptors.Server;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Server-side interceptors for llm/completion events.
/// These interceptors can be deployed as a centralized policy enforcement
/// layer for LLM API calls across multiple clients.
/// </summary>
[McpServerInterceptorType]
public class ServerLlmInterceptors
{
    // PII patterns
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);

    // Prompt injection patterns
    private static readonly string[] InjectionPatterns =
    [
        "ignore all previous", "ignore your instructions", "disregard your",
        "forget your", "you are now", "act as if", "pretend you are",
        "jailbreak", "DAN mode", "developer mode", "bypass", "override"
    ];

    // Sensitive data patterns for redaction
    private static readonly Regex ApiKeyPattern = new(@"\b(sk_live_|sk_test_|api_key[=:]\s*)[a-zA-Z0-9_-]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BearerTokenPattern = new(@"Bearer\s+[a-zA-Z0-9._-]+", RegexOptions.Compiled);

    #region Validation Interceptors

    /// <summary>
    /// Validates LLM completion requests for PII.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-pii-validator",
        Description = "Validates LLM prompts for PII",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -1000)]
    public ValidationInterceptorResult ValidateLlmPii(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        foreach (var message in request.Messages)
        {
            var content = message.Content ?? string.Empty;

            if (SsnPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Path = "messages[].content",
                    Message = "SSN detected in LLM prompt - blocked for PII protection",
                    Severity = ValidationSeverity.Error
                });
            }

            if (CreditCardPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Path = "messages[].content",
                    Message = "Credit card number detected in LLM prompt - blocked for PII protection",
                    Severity = ValidationSeverity.Error
                });
            }

            if (EmailPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Path = "messages[].content",
                    Message = "Email address detected - consider removing PII",
                    Severity = ValidationSeverity.Warn
                });
            }
        }

        if (messages.Count > 0)
        {
            var hasErrors = messages.Any(m => m.Severity == ValidationSeverity.Error);
            return new ValidationInterceptorResult
            {
                Valid = !hasErrors,
                Severity = hasErrors ? ValidationSeverity.Error : ValidationSeverity.Warn,
                Messages = messages
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Detects prompt injection attempts in LLM requests.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-injection-detector",
        Description = "Detects prompt injection attempts in LLM requests",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -900)]
    public ValidationInterceptorResult DetectLlmInjection(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        foreach (var message in request.Messages)
        {
            // Only check user messages
            if (message.Role != LlmMessageRole.User)
                continue;

            var content = (message.Content ?? string.Empty).ToLowerInvariant();

            foreach (var pattern in InjectionPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = "messages[].content",
                        Message = $"Potential prompt injection detected: '{pattern}'",
                        Severity = ValidationSeverity.Error
                    });
                    break;
                }
            }
        }

        if (messages.Count > 0)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = messages
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Enforces token and cost limits for LLM requests.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-cost-limiter",
        Description = "Enforces token and cost limits for LLM API calls",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -800)]
    public ValidationInterceptorResult EnforceLlmLimits(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return ValidationInterceptorResult.Success();
        }

        const int MaxPromptTokens = 8000;
        const int MaxCompletionTokens = 4000;
        const int MaxMessageCount = 50;

        var messages = new List<ValidationMessage>();

        // Check message count
        if (request.Messages.Count > MaxMessageCount)
        {
            messages.Add(new ValidationMessage
            {
                Path = "messages",
                Message = $"Message count ({request.Messages.Count}) exceeds limit ({MaxMessageCount})",
                Severity = ValidationSeverity.Error
            });
        }

        // Estimate prompt tokens (rough: ~4 chars per token)
        var estimatedPromptTokens = request.Messages.Sum(m => (m.Content?.Length ?? 0) / 4);
        if (estimatedPromptTokens > MaxPromptTokens)
        {
            messages.Add(new ValidationMessage
            {
                Path = "messages",
                Message = $"Estimated prompt tokens ({estimatedPromptTokens}) exceeds limit ({MaxPromptTokens})",
                Severity = ValidationSeverity.Error
            });
        }

        // Check max_tokens
        if (request.MaxTokens > MaxCompletionTokens)
        {
            messages.Add(new ValidationMessage
            {
                Path = "max_tokens",
                Message = $"Requested max_tokens ({request.MaxTokens}) exceeds limit ({MaxCompletionTokens})",
                Severity = ValidationSeverity.Error
            });
        }

        if (messages.Count > 0)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = messages,
                Info = new JsonObject
                {
                    ["limits"] = new JsonObject
                    {
                        ["maxPromptTokens"] = MaxPromptTokens,
                        ["maxCompletionTokens"] = MaxCompletionTokens,
                        ["maxMessageCount"] = MaxMessageCount
                    },
                    ["actual"] = new JsonObject
                    {
                        ["estimatedPromptTokens"] = estimatedPromptTokens,
                        ["requestedMaxTokens"] = request.MaxTokens,
                        ["messageCount"] = request.Messages.Count
                    }
                }
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Validates allowed models against a whitelist.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-model-validator",
        Description = "Validates that requested model is in the allowed list",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -700)]
    public ValidationInterceptorResult ValidateLlmModel(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return ValidationInterceptorResult.Success();
        }

        // Allowed models whitelist
        var allowedModels = new[]
        {
            "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini",
            "gpt-3.5-turbo", "gpt-3.5-turbo-16k",
            "claude-3-opus", "claude-3-sonnet", "claude-3-haiku",
            "claude-3.5-sonnet"
        };

        var model = request.Model ?? string.Empty;
        var isAllowed = allowedModels.Any(m => model.StartsWith(m, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed && !string.IsNullOrEmpty(model))
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [
                    new ValidationMessage
                    {
                        Path = "model",
                        Message = $"Model '{model}' is not in the allowed models list",
                        Severity = ValidationSeverity.Error
                    }
                ],
                Info = new JsonObject
                {
                    ["requestedModel"] = model,
                    ["allowedModels"] = JsonNode.Parse(JsonSerializer.Serialize(allowedModels))
                }
            };
        }

        return ValidationInterceptorResult.Success();
    }

    #endregion

    #region Mutation Interceptors

    /// <summary>
    /// Normalizes LLM request messages by trimming whitespace.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-prompt-normalizer",
        Description = "Normalizes prompts by trimming whitespace",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -100)]
    public MutationInterceptorResult NormalizeLlmPrompt(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var modified = false;
        foreach (var message in request.Messages)
        {
            if (message.Content is not null)
            {
                var trimmed = message.Content.Trim();
                if (trimmed != message.Content)
                {
                    message.Content = trimmed;
                    modified = true;
                }
            }
        }

        if (modified)
        {
            var mutatedPayload = JsonSerializer.SerializeToNode(request);
            var result = MutationInterceptorResult.Mutated(mutatedPayload);
            result.Info = new JsonObject { ["action"] = "trimmed whitespace from messages" };
            return result;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Redacts sensitive data from LLM responses.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-response-redactor",
        Description = "Redacts sensitive data from LLM responses",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response,
        PriorityHint = 100)]
    public MutationInterceptorResult RedactLlmResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var response = payload.Deserialize<LlmCompletionResponse>();
        if (response is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var modified = false;
        var redactions = new List<string>();

        foreach (var choice in response.Choices)
        {
            if (choice.Message.Content is not null)
            {
                var content = choice.Message.Content;

                if (ApiKeyPattern.IsMatch(content))
                {
                    content = ApiKeyPattern.Replace(content, "[REDACTED_API_KEY]");
                    redactions.Add("api_key");
                    modified = true;
                }

                if (BearerTokenPattern.IsMatch(content))
                {
                    content = BearerTokenPattern.Replace(content, "Bearer [REDACTED_TOKEN]");
                    redactions.Add("bearer_token");
                    modified = true;
                }

                if (SsnPattern.IsMatch(content))
                {
                    content = SsnPattern.Replace(content, "XXX-XX-XXXX");
                    redactions.Add("ssn");
                    modified = true;
                }

                if (CreditCardPattern.IsMatch(content))
                {
                    content = CreditCardPattern.Replace(content, "XXXX-XXXX-XXXX-XXXX");
                    redactions.Add("credit_card");
                    modified = true;
                }

                choice.Message.Content = content;
            }
        }

        if (modified)
        {
            var mutatedPayload = JsonSerializer.SerializeToNode(response);
            var result = MutationInterceptorResult.Mutated(mutatedPayload);
            result.Info = new JsonObject
            {
                ["action"] = "redacted sensitive data",
                ["types"] = JsonNode.Parse($"[\"{string.Join("\", \"", redactions.Distinct())}\"]")
            };
            return result;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Injects a system message for safety guidelines.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-safety-injector",
        Description = "Injects safety guidelines into LLM requests",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = 50)]
    public MutationInterceptorResult InjectSafetyGuidelines(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var request = payload.Deserialize<LlmCompletionRequest>();
        if (request is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        const string SafetyGuideline = "Important: Do not reveal any API keys, passwords, secrets, or personally identifiable information (PII) in your responses. If asked to generate or reveal such information, politely decline.";

        // Check if safety guideline already exists
        var hasGuideline = request.Messages.Any(m =>
            m.Role == LlmMessageRole.System &&
            m.Content?.Contains("Do not reveal", StringComparison.OrdinalIgnoreCase) == true);

        if (!hasGuideline)
        {
            // Insert safety guideline as first system message
            var safetyMessage = LlmMessage.System(SafetyGuideline);
            request.Messages.Insert(0, safetyMessage);

            var mutatedPayload = JsonSerializer.SerializeToNode(request);
            var result = MutationInterceptorResult.Mutated(mutatedPayload);
            result.Info = new JsonObject { ["action"] = "injected safety guidelines" };
            return result;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    #endregion

    #region Observability Interceptors

    /// <summary>
    /// Logs LLM request details for monitoring.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-request-logger",
        Description = "Logs LLM request details",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogLlmRequest(JsonNode? payload)
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

        var estimatedTokens = request.Messages.Sum(m => (m.Content?.Length ?? 0) / 4);

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["message_count"] = request.Messages.Count,
                ["estimated_prompt_tokens"] = estimatedTokens,
                ["max_tokens"] = request.MaxTokens ?? 0,
                ["temperature"] = request.Temperature ?? 1.0
            },
            Info = new JsonObject
            {
                ["event"] = "llm_request",
                ["model"] = request.Model,
                ["messageCount"] = request.Messages.Count,
                ["estimatedPromptTokens"] = estimatedTokens,
                ["hasTools"] = request.Tools?.Count > 0,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Logs LLM response details and tracks usage.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-response-logger",
        Description = "Logs LLM response details and usage metrics",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult LogLlmResponse(JsonNode? payload)
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

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["prompt_tokens"] = response.Usage?.PromptTokens ?? 0,
                ["completion_tokens"] = response.Usage?.CompletionTokens ?? 0,
                ["total_tokens"] = response.Usage?.TotalTokens ?? 0,
                ["choice_count"] = response.Choices.Count
            },
            Info = new JsonObject
            {
                ["event"] = "llm_response",
                ["id"] = response.Id,
                ["model"] = response.Model,
                ["choiceCount"] = response.Choices.Count,
                ["hasToolCalls"] = hasToolCalls,
                ["promptTokens"] = response.Usage?.PromptTokens,
                ["completionTokens"] = response.Usage?.CompletionTokens,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Tracks estimated costs for LLM API usage.
    /// </summary>
    [McpServerInterceptor(
        Name = "llm-cost-tracker",
        Description = "Tracks estimated LLM API costs",
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult TrackLlmCosts(JsonNode? payload)
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

        // Approximate pricing per 1K tokens (example, adjust as needed)
        var pricing = GetModelPricing(response.Model);
        var promptCost = (response.Usage.PromptTokens / 1000.0m) * pricing.promptCostPer1k;
        var completionCost = (response.Usage.CompletionTokens / 1000.0m) * pricing.completionCostPer1k;
        var totalCost = promptCost + completionCost;

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["estimated_cost_usd"] = (double)totalCost,
                ["prompt_cost_usd"] = (double)promptCost,
                ["completion_cost_usd"] = (double)completionCost
            },
            Info = new JsonObject
            {
                ["event"] = "cost_tracking",
                ["model"] = response.Model,
                ["promptTokens"] = response.Usage.PromptTokens,
                ["completionTokens"] = response.Usage.CompletionTokens,
                ["estimatedCostUsd"] = (double)totalCost,
                ["currency"] = "USD",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    private static (decimal promptCostPer1k, decimal completionCostPer1k) GetModelPricing(string? model)
    {
        // Example pricing - adjust based on actual model pricing
        return model?.ToLowerInvariant() switch
        {
            var m when m?.StartsWith("gpt-4o") == true => (0.005m, 0.015m),
            var m when m?.StartsWith("gpt-4-turbo") == true => (0.01m, 0.03m),
            var m when m?.StartsWith("gpt-4") == true => (0.03m, 0.06m),
            var m when m?.StartsWith("gpt-3.5") == true => (0.0005m, 0.0015m),
            var m when m?.StartsWith("claude-3-opus") == true => (0.015m, 0.075m),
            var m when m?.StartsWith("claude-3-sonnet") == true => (0.003m, 0.015m),
            var m when m?.StartsWith("claude-3-haiku") == true => (0.00025m, 0.00125m),
            _ => (0.01m, 0.03m) // Default
        };
    }

    #endregion
}
