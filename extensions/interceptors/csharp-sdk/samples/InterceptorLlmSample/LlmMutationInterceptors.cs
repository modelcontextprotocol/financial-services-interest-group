using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Interceptors.Protocol.Llm;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Mutation interceptors for llm/completion events.
/// These interceptors transform requests and responses.
/// </summary>
[McpClientInterceptorType]
public partial class LlmMutationInterceptors
{
    // Patterns for sensitive data redaction
    private static readonly Regex ApiKeyPattern = new(@"\b(sk_live_|api_key[=:]\s*)[a-zA-Z0-9_-]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PasswordPattern = new(@"\b(password[=:]\s*|secret[=:]\s*)[^\s]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BearerTokenPattern = new(@"Bearer\s+[a-zA-Z0-9._-]+", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes prompt whitespace by trimming message content.
    /// </summary>
    [McpClientInterceptor(
        Name = "prompt-normalizer",
        Description = "Normalizes whitespace in prompts",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = -100)] // Run early
    public static MutationInterceptorResult NormalizePrompts(JsonNode? payload)
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
            return MutationInterceptorResult.Mutated(mutatedPayload);
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Redacts sensitive information from LLM responses.
    /// </summary>
    [McpClientInterceptor(
        Name = "response-redactor",
        Description = "Redacts sensitive information from LLM responses",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Response,
        PriorityHint = 100)] // Run late in response chain
    public static MutationInterceptorResult RedactResponse(JsonNode? payload)
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

                // Redact API keys
                if (ApiKeyPattern.IsMatch(content))
                {
                    content = ApiKeyPattern.Replace(content, "[REDACTED_API_KEY]");
                    redactions.Add("api_key");
                    modified = true;
                }

                // Redact passwords
                if (PasswordPattern.IsMatch(content))
                {
                    content = PasswordPattern.Replace(content, "[REDACTED_SECRET]");
                    redactions.Add("password");
                    modified = true;
                }

                // Redact bearer tokens
                if (BearerTokenPattern.IsMatch(content))
                {
                    content = BearerTokenPattern.Replace(content, "Bearer [REDACTED_TOKEN]");
                    redactions.Add("bearer_token");
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
    /// Adds metadata to track request origin.
    /// </summary>
    [McpClientInterceptor(
        Name = "metadata-injector",
        Description = "Adds tracking metadata to requests",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.LlmCompletion],
        Phase = InterceptorPhase.Request,
        PriorityHint = 100)] // Run late
    public static MutationInterceptorResult InjectMetadata(JsonNode? payload)
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

        // Add tracking metadata
        request.Meta ??= new JsonObject();
        request.Meta["interceptor_processed"] = true;
        request.Meta["interceptor_timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        request.Meta["interceptor_version"] = "1.0.0";

        var mutatedPayload = JsonSerializer.SerializeToNode(request);
        var result = MutationInterceptorResult.Mutated(mutatedPayload);
        result.Info = new JsonObject { ["action"] = "added tracking metadata" };
        return result;
    }
}
