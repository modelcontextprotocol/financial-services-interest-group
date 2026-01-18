using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Server-side mutation interceptors for MCP operations.
/// These interceptors transform requests and responses as they pass through
/// the interceptor service, enabling centralized data transformation.
/// </summary>
[McpServerInterceptorType]
public partial class ServerMutationInterceptors
{
    // Patterns for sensitive data redaction
    private static readonly Regex ApiKeyPattern = new(@"\b(sk_live_|sk_test_|api_key[=:]\s*|apikey[=:]\s*)[a-zA-Z0-9_-]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PasswordPattern = new(@"(password|passwd|pwd|secret|token)[=:]\s*[^\s,;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BearerTokenPattern = new(@"Bearer\s+[a-zA-Z0-9._-]+", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes tool call arguments by trimming whitespace.
    /// </summary>
    [McpServerInterceptor(
        Name = "argument-normalizer",
        Description = "Normalizes tool call arguments by trimming whitespace",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -100)] // Run early
    public MutationInterceptorResult NormalizeArguments(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        if (toolCall?.Arguments is null || toolCall.Arguments.Count == 0)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var modified = false;
        var normalizedArgs = new Dictionary<string, object?>();

        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            {
                var strValue = element.GetString();
                var trimmed = strValue?.Trim();
                if (trimmed != strValue)
                {
                    normalizedArgs[arg.Key] = trimmed;
                    modified = true;
                }
                else
                {
                    normalizedArgs[arg.Key] = value;
                }
            }
            else
            {
                normalizedArgs[arg.Key] = value;
            }
        }

        if (modified)
        {
            var mutatedPayload = new JsonObject
            {
                ["name"] = toolCall.Name,
                ["arguments"] = JsonSerializer.SerializeToNode(normalizedArgs)
            };

            var result = MutationInterceptorResult.Mutated(mutatedPayload);
            result.Info = new JsonObject { ["action"] = "trimmed whitespace from string arguments" };
            return result;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Redacts sensitive information from tool responses.
    /// </summary>
    [McpServerInterceptor(
        Name = "response-redactor",
        Description = "Redacts sensitive information from tool responses",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response,
        PriorityHint = 100)] // Run late
    public MutationInterceptorResult RedactResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        CallToolResult? result;
        try
        {
            result = payload.Deserialize<CallToolResult>();
        }
        catch (JsonException)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        if (result?.Content is null || result.Content.Count == 0)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var modified = false;
        var redactions = new List<string>();
        var newContent = new List<ContentBlock>();

        foreach (var content in result.Content)
        {
            if (content is TextContentBlock textBlock)
            {
                var text = textBlock.Text ?? string.Empty;
                var originalText = text;

                // Redact API keys
                if (ApiKeyPattern.IsMatch(text))
                {
                    text = ApiKeyPattern.Replace(text, "[REDACTED_API_KEY]");
                    redactions.Add("api_key");
                }

                // Redact passwords/secrets
                if (PasswordPattern.IsMatch(text))
                {
                    text = PasswordPattern.Replace(text, "[REDACTED_SECRET]");
                    redactions.Add("password");
                }

                // Redact bearer tokens
                if (BearerTokenPattern.IsMatch(text))
                {
                    text = BearerTokenPattern.Replace(text, "Bearer [REDACTED_TOKEN]");
                    redactions.Add("bearer_token");
                }

                // Redact SSNs
                if (SsnPattern.IsMatch(text))
                {
                    text = SsnPattern.Replace(text, "XXX-XX-XXXX");
                    redactions.Add("ssn");
                }

                // Redact credit cards
                if (CreditCardPattern.IsMatch(text))
                {
                    text = CreditCardPattern.Replace(text, "XXXX-XXXX-XXXX-XXXX");
                    redactions.Add("credit_card");
                }

                if (text != originalText)
                {
                    modified = true;
                    newContent.Add(new TextContentBlock { Text = text });
                }
                else
                {
                    newContent.Add(content);
                }
            }
            else
            {
                newContent.Add(content);
            }
        }

        if (modified)
        {
            var mutatedResult = new CallToolResult
            {
                Content = newContent,
                IsError = result.IsError
            };

            var mutatedPayload = JsonSerializer.SerializeToNode(mutatedResult);
            var mutationResult = MutationInterceptorResult.Mutated(mutatedPayload);
            mutationResult.Info = new JsonObject
            {
                ["action"] = "redacted sensitive data",
                ["types"] = JsonNode.Parse($"[\"{string.Join("\", \"", redactions.Distinct())}\"]")
            };
            return mutationResult;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Adds tracking metadata to tool requests.
    /// </summary>
    [McpServerInterceptor(
        Name = "request-metadata-injector",
        Description = "Adds tracking metadata to tool requests",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = 100)] // Run late
    public MutationInterceptorResult InjectRequestMetadata(JsonNode? payload)
    {
        if (payload is not JsonObject obj)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Clone the payload and add metadata
        var mutatedPayload = obj.DeepClone() as JsonObject ?? new JsonObject();

        // Add or update _meta object
        if (mutatedPayload["_meta"] is not JsonObject meta)
        {
            meta = new JsonObject();
            mutatedPayload["_meta"] = meta;
        }

        meta["interceptor_processed"] = true;
        meta["interceptor_timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        meta["interceptor_id"] = Guid.NewGuid().ToString("N")[..8];

        var result = MutationInterceptorResult.Mutated(mutatedPayload);
        result.Info = new JsonObject { ["action"] = "added tracking metadata" };
        return result;
    }

    /// <summary>
    /// Sanitizes resource content by removing potentially dangerous HTML/script tags.
    /// </summary>
    [McpServerInterceptor(
        Name = "content-sanitizer",
        Description = "Sanitizes resource content by removing dangerous tags",
        Events = [InterceptorEvents.ResourcesRead],
        Phase = InterceptorPhase.Response,
        PriorityHint = 50)]
    public MutationInterceptorResult SanitizeContent(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        ReadResourceResult? result;
        try
        {
            result = payload.Deserialize<ReadResourceResult>();
        }
        catch (JsonException)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        if (result?.Contents is null || result.Contents.Count == 0)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var modified = false;
        var sanitizations = new List<string>();
        var newContents = new List<ResourceContents>();

        foreach (var content in result.Contents)
        {
            if (content is TextResourceContents textContent)
            {
                var text = textContent.Text ?? string.Empty;
                var originalText = text;

                // Remove script tags
                var scriptPattern = new Regex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase);
                if (scriptPattern.IsMatch(text))
                {
                    text = scriptPattern.Replace(text, "[REMOVED_SCRIPT]");
                    sanitizations.Add("script_tags");
                }

                // Remove onclick/onerror handlers
                var eventHandlerPattern = new Regex(@"\s+on\w+\s*=\s*[""'][^""']*[""']", RegexOptions.IgnoreCase);
                if (eventHandlerPattern.IsMatch(text))
                {
                    text = eventHandlerPattern.Replace(text, "");
                    sanitizations.Add("event_handlers");
                }

                // Remove javascript: URLs
                var jsUrlPattern = new Regex(@"javascript\s*:", RegexOptions.IgnoreCase);
                if (jsUrlPattern.IsMatch(text))
                {
                    text = jsUrlPattern.Replace(text, "[REMOVED_JS_URL]");
                    sanitizations.Add("javascript_urls");
                }

                if (text != originalText)
                {
                    modified = true;
                    newContents.Add(new TextResourceContents
                    {
                        Uri = textContent.Uri,
                        MimeType = textContent.MimeType,
                        Text = text
                    });
                }
                else
                {
                    newContents.Add(content);
                }
            }
            else
            {
                newContents.Add(content);
            }
        }

        if (modified)
        {
            var mutatedResult = new ReadResourceResult
            {
                Contents = newContents
            };

            var mutatedPayload = JsonSerializer.SerializeToNode(mutatedResult);
            var mutationResult = MutationInterceptorResult.Mutated(mutatedPayload);
            mutationResult.Info = new JsonObject
            {
                ["action"] = "sanitized content",
                ["removed"] = JsonNode.Parse($"[\"{string.Join("\", \"", sanitizations.Distinct())}\"]")
            };
            return mutationResult;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }

    /// <summary>
    /// Transforms tool names to enforce naming conventions.
    /// </summary>
    [McpServerInterceptor(
        Name = "tool-name-normalizer",
        Description = "Normalizes tool names to enforce naming conventions",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -50)]
    public MutationInterceptorResult NormalizeToolName(JsonNode? payload)
    {
        if (payload is not JsonObject obj)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        var name = obj["name"]?.GetValue<string>();
        if (string.IsNullOrEmpty(name))
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Normalize: lowercase, replace spaces with underscores
        var normalizedName = name.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

        if (normalizedName != name)
        {
            var mutatedPayload = obj.DeepClone() as JsonObject ?? new JsonObject();
            mutatedPayload["name"] = normalizedName;

            var result = MutationInterceptorResult.Mutated(mutatedPayload);
            result.Info = new JsonObject
            {
                ["action"] = "normalized tool name",
                ["original"] = name,
                ["normalized"] = normalizedName
            };
            return result;
        }

        return MutationInterceptorResult.Unchanged(payload);
    }
}
