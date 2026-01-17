using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Sample observability interceptors for MCP client operations.
/// These interceptors demonstrate logging, metrics collection, and tracing.
/// </summary>
[McpClientInterceptorType]
public class ClientObservabilityInterceptors
{
    /// <summary>
    /// Logs all outgoing tool call requests for audit purposes.
    /// </summary>
    [McpClientInterceptor(
        Name = "request-logger",
        Description = "Logs outgoing tool call requests",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogRequest(JsonNode? payload, string @event)
    {
        if (payload is null)
        {
            Console.WriteLine($"[Observability] Request for '{@event}' - no payload");
            return ObservabilityInterceptorResult.Success();
        }

        // Extract tool name for logging
        string? toolName = null;
        try
        {
            var toolCall = payload.Deserialize<CallToolRequestParams>();
            toolName = toolCall?.Name;
        }
        catch
        {
            // Ignore deserialization errors
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [REQUEST] Tool: {toolName ?? "unknown"} | Event: {@event}");

        // Log argument keys (not values for privacy)
        if (payload["arguments"] is JsonObject args)
        {
            var argKeys = string.Join(", ", args.Select(a => a.Key));
            Console.WriteLine($"           Arguments: [{argKeys}]");
        }

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["loggedAt"] = DateTimeOffset.UtcNow.ToString("o"),
                ["toolName"] = toolName
            }
        };
    }

    /// <summary>
    /// Logs all incoming tool call responses for audit purposes.
    /// </summary>
    [McpClientInterceptor(
        Name = "response-logger",
        Description = "Logs incoming tool call responses",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult LogResponse(JsonNode? payload, string @event)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff");

        if (payload is null)
        {
            Console.WriteLine($"[{timestamp}] [RESPONSE] Event: {@event} - no payload");
            return ObservabilityInterceptorResult.Success();
        }

        // Extract result info for logging
        bool? isError = null;
        int contentCount = 0;
        try
        {
            var result = payload.Deserialize<CallToolResult>();
            isError = result?.IsError;
            contentCount = result?.Content?.Count ?? 0;
        }
        catch
        {
            // Ignore deserialization errors
        }

        var status = isError == true ? "ERROR" : "SUCCESS";
        Console.WriteLine($"[{timestamp}] [RESPONSE] Status: {status} | Content blocks: {contentCount}");

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["loggedAt"] = DateTimeOffset.UtcNow.ToString("o"),
                ["isError"] = isError,
                ["contentCount"] = contentCount
            }
        };
    }

    /// <summary>
    /// Collects metrics about tool list operations.
    /// </summary>
    [McpClientInterceptor(
        Name = "tools-list-metrics",
        Description = "Collects metrics about tools list operations",
        Type = InterceptorType.Observability,
        Events = [InterceptorEvents.ToolsList],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult CollectToolsMetrics(JsonNode? payload)
    {
        if (payload is null)
        {
            return ObservabilityInterceptorResult.Success();
        }

        // Count tools in response
        int toolCount = 0;
        try
        {
            if (payload["tools"] is JsonArray tools)
            {
                toolCount = tools.Count;
            }
        }
        catch
        {
            // Ignore errors
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [METRICS] Tools discovered: {toolCount}");

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["toolCount"] = toolCount,
                ["collectedAt"] = DateTimeOffset.UtcNow.ToString("o")
            }
        };
    }
}
