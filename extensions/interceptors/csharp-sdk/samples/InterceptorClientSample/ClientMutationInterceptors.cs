using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Sample mutation interceptors for MCP client operations.
/// These interceptors demonstrate argument transformation and response filtering.
/// </summary>
[McpClientInterceptorType]
public class ClientMutationInterceptors
{
    /// <summary>
    /// Normalizes tool arguments by trimming whitespace and converting empty strings to null.
    /// </summary>
    [McpClientInterceptor(
        Name = "argument-normalizer",
        Description = "Normalizes tool arguments (trim whitespace, handle empty strings)",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -100)] // Normalization runs before validation
    public MutationInterceptorResult NormalizeArguments(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Parse the tool call
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

        bool modified = false;
        var normalizedArgs = new Dictionary<string, JsonElement>();

        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value;

            // Trim string values
            if (value.ValueKind == JsonValueKind.String)
            {
                var stringValue = value.GetString();
                if (stringValue is not null)
                {
                    var trimmed = stringValue.Trim();
                    if (trimmed != stringValue)
                    {
                        modified = true;
                        // Convert empty strings to null
                        if (string.IsNullOrEmpty(trimmed))
                        {
                            normalizedArgs[arg.Key] = JsonDocument.Parse("null").RootElement;
                        }
                        else
                        {
                            normalizedArgs[arg.Key] = JsonDocument.Parse($"\"{EscapeJsonString(trimmed)}\"").RootElement;
                        }
                        continue;
                    }
                }
            }

            normalizedArgs[arg.Key] = value;
        }

        if (!modified)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Rebuild the payload with normalized arguments
        var mutatedPayload = new JsonObject
        {
            ["name"] = toolCall.Name,
            ["arguments"] = JsonSerializer.SerializeToNode(normalizedArgs)
        };

        if (toolCall.Meta is not null)
        {
            mutatedPayload["_meta"] = JsonSerializer.SerializeToNode(toolCall.Meta);
        }

        return MutationInterceptorResult.Mutated(mutatedPayload);
    }

    /// <summary>
    /// Adds a timestamp to all outgoing tool call requests for audit purposes.
    /// </summary>
    [McpClientInterceptor(
        Name = "request-timestamp",
        Description = "Adds timestamp metadata to tool call requests",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = 100)] // Runs after normalization/validation
    public MutationInterceptorResult AddTimestamp(JsonNode? payload)
    {
        if (payload is not JsonObject obj)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Create or update _meta with timestamp
        var meta = obj["_meta"]?.AsObject() ?? new JsonObject();
        meta["clientTimestamp"] = DateTimeOffset.UtcNow.ToString("o");
        meta["clientVersion"] = "1.0.0";

        obj["_meta"] = meta;

        return MutationInterceptorResult.Mutated(obj);
    }

    /// <summary>
    /// Redacts sensitive information from tool responses before returning to caller.
    /// </summary>
    [McpClientInterceptor(
        Name = "response-redactor",
        Description = "Redacts sensitive patterns from tool responses",
        Type = InterceptorType.Mutation,
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response,
        PriorityHint = 50)]
    public MutationInterceptorResult RedactResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Parse the result
        CallToolResult? result;
        try
        {
            result = payload.Deserialize<CallToolResult>();
        }
        catch (JsonException)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        if (result?.Content is null)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        bool modified = false;
        var newContent = new List<ContentBlock>();

        foreach (var content in result.Content)
        {
            if (content is TextContentBlock textBlock)
            {
                var redactedText = RedactSensitivePatterns(textBlock.Text);
                if (redactedText != textBlock.Text)
                {
                    modified = true;
                    newContent.Add(new TextContentBlock { Text = redactedText });
                    continue;
                }
            }
            newContent.Add(content);
        }

        if (!modified)
        {
            return MutationInterceptorResult.Unchanged(payload);
        }

        // Rebuild result with redacted content
        var mutatedResult = new JsonObject
        {
            ["content"] = JsonSerializer.SerializeToNode(newContent),
            ["isError"] = result.IsError
        };

        return MutationInterceptorResult.Mutated(mutatedResult);
    }

    private static string RedactSensitivePatterns(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        // Redact API keys (common patterns)
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(?i)(api[_-]?key|apikey|secret|password|token)[""']?\s*[:=]\s*[""']?[\w\-\.]+[""']?",
            "$1=***REDACTED***");

        // Redact bearer tokens
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(?i)bearer\s+[\w\-\.]+",
            "Bearer ***REDACTED***");

        return text;
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
