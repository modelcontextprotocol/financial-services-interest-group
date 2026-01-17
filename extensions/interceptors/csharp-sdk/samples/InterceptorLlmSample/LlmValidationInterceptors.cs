using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Interceptors.Protocol.Llm;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Validation interceptors for llm/completion events.
/// These interceptors check for policy violations before requests are sent to the LLM.
/// </summary>
[McpClientInterceptorType]
public partial class LlmValidationInterceptors
{
    // Common PII patterns
    private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b(?:\+?1[-.\s]?)?(?:\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled);

    // Prompt injection patterns
    private static readonly string[] InjectionPatterns =
    [
        "ignore all previous",
        "ignore your instructions",
        "disregard your",
        "forget your",
        "you are now",
        "act as if",
        "pretend you are",
        "jailbreak",
        "DAN mode",
        "developer mode"
    ];

    /// <summary>
    /// Detects PII in LLM completion requests.
    /// </summary>
    [McpClientInterceptor(
        Name = "pii-detector",
        Description = "Detects personally identifiable information (PII) in LLM prompts",
        Type = InterceptorType.Validation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request)]
    public static ValidationInterceptorResult DetectPii(JsonNode? payload)
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

            // Check for SSN (always error)
            if (SsnPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Message = "Social Security Number detected in prompt. This is not allowed.",
                    Severity = ValidationSeverity.Error,
                    Path = "messages[].content"
                });
            }

            // Check for credit card (always error)
            if (CreditCardPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Message = "Credit card number detected in prompt. This is not allowed.",
                    Severity = ValidationSeverity.Error,
                    Path = "messages[].content"
                });
            }

            // Check for email (warning)
            if (EmailPattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Message = "Email address detected in prompt. Consider removing PII.",
                    Severity = ValidationSeverity.Warn,
                    Path = "messages[].content"
                });
            }

            // Check for phone (warning)
            if (PhonePattern.IsMatch(content))
            {
                messages.Add(new ValidationMessage
                {
                    Message = "Phone number detected in prompt. Consider removing PII.",
                    Severity = ValidationSeverity.Warn,
                    Path = "messages[].content"
                });
            }
        }

        var hasErrors = messages.Any(m => m.Severity == ValidationSeverity.Error);

        return new ValidationInterceptorResult
        {
            Valid = !hasErrors,
            Severity = hasErrors ? ValidationSeverity.Error : (messages.Any() ? ValidationSeverity.Warn : null),
            Messages = messages.Count > 0 ? messages : null
        };
    }

    /// <summary>
    /// Detects prompt injection attempts.
    /// </summary>
    [McpClientInterceptor(
        Name = "injection-detector",
        Description = "Detects potential prompt injection attempts",
        Type = InterceptorType.Validation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request)]
    public static ValidationInterceptorResult DetectInjection(JsonNode? payload)
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
            // Only check user messages for injection
            if (message.Role != LlmMessageRole.User)
                continue;

            var content = (message.Content ?? string.Empty).ToLowerInvariant();

            foreach (var pattern in InjectionPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new ValidationMessage
                    {
                        Message = $"Potential prompt injection detected: '{pattern}'",
                        Severity = ValidationSeverity.Error,
                        Path = "messages[].content"
                    });
                    break; // One detection per message is enough
                }
            }
        }

        return new ValidationInterceptorResult
        {
            Valid = messages.Count == 0,
            Severity = messages.Count > 0 ? ValidationSeverity.Error : null,
            Messages = messages.Count > 0 ? messages : null,
            // Suggestions can be added at the result level, not on individual messages
            Suggestions = messages.Count > 0 ? [
                new ValidationSuggestion
                {
                    Path = "messages[].content",
                    Value = JsonValue.Create("Remove or rephrase the suspicious content")
                }
            ] : null
        };
    }

    /// <summary>
    /// Enforces token limits to control costs.
    /// </summary>
    [McpClientInterceptor(
        Name = "token-limiter",
        Description = "Enforces token limits to control LLM API costs",
        Type = InterceptorType.Validation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request)]
    public static ValidationInterceptorResult EnforceTokenLimits(JsonNode? payload)
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

        const int MaxPromptTokens = 4000;
        const int MaxCompletionTokens = 2000;

        var messages = new List<ValidationMessage>();

        // Rough token estimation (4 chars per token on average)
        var estimatedPromptTokens = request.Messages.Sum(m => (m.Content?.Length ?? 0) / 4);

        if (estimatedPromptTokens > MaxPromptTokens)
        {
            messages.Add(new ValidationMessage
            {
                Message = $"Estimated prompt tokens ({estimatedPromptTokens}) exceeds limit ({MaxPromptTokens})",
                Severity = ValidationSeverity.Error,
                Path = "messages"
            });
        }

        if (request.MaxTokens > MaxCompletionTokens)
        {
            messages.Add(new ValidationMessage
            {
                Message = $"Requested max_tokens ({request.MaxTokens}) exceeds limit ({MaxCompletionTokens})",
                Severity = ValidationSeverity.Error,
                Path = "max_tokens"
            });
        }

        return new ValidationInterceptorResult
        {
            Valid = messages.Count == 0,
            Severity = messages.Count > 0 ? ValidationSeverity.Error : null,
            Messages = messages.Count > 0 ? messages : null,
            Info = new JsonObject
            {
                ["estimatedPromptTokens"] = estimatedPromptTokens,
                ["maxPromptTokens"] = MaxPromptTokens,
                ["requestedMaxTokens"] = request.MaxTokens,
                ["maxCompletionTokens"] = MaxCompletionTokens
            }
        };
    }
}
