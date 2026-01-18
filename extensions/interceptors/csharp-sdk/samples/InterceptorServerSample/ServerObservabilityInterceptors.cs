using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Server-side observability interceptors for MCP operations.
/// These interceptors log, monitor, and audit MCP messages without modifying them.
/// Useful for centralized logging, metrics collection, and compliance auditing.
/// </summary>
[McpServerInterceptorType]
public class ServerObservabilityInterceptors
{
    // In-memory metrics storage (in production, use a proper metrics service)
    private static readonly ConcurrentDictionary<string, long> ToolCallCounts = new();
    private static readonly ConcurrentDictionary<string, long> ResourceReadCounts = new();
    private static readonly ConcurrentDictionary<string, List<long>> ToolCallDurations = new();

    /// <summary>
    /// Logs tool call requests for monitoring and debugging.
    /// </summary>
    [McpServerInterceptor(
        Name = "tool-request-logger",
        Description = "Logs tool call request details",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogToolRequest(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        if (toolCall is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        // Track tool call counts
        var toolName = toolCall.Name ?? "unknown";
        ToolCallCounts.AddOrUpdate(toolName, 1, (_, count) => count + 1);

        var argumentCount = toolCall.Arguments?.Count ?? 0;
        var argumentNames = toolCall.Arguments?.Keys.ToArray() ?? [];

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = "tool_request",
                ["tool"] = toolName,
                ["argumentCount"] = argumentCount,
                ["argumentNames"] = JsonNode.Parse(JsonSerializer.Serialize(argumentNames)),
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["totalCallsToTool"] = ToolCallCounts.GetValueOrDefault(toolName, 0)
            }
        };
    }

    /// <summary>
    /// Logs tool call responses including success/failure status.
    /// </summary>
    [McpServerInterceptor(
        Name = "tool-response-logger",
        Description = "Logs tool call response details",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult LogToolResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        CallToolResult? result;
        try
        {
            result = payload.Deserialize<CallToolResult>();
        }
        catch (JsonException)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        if (result is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var contentCount = result.Content?.Count ?? 0;
        var contentTypes = result.Content?.Select(c => c.GetType().Name).Distinct().ToArray() ?? [];

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["content_count"] = contentCount,
                ["is_error"] = (result.IsError ?? false) ? 1.0 : 0.0
            },
            Info = new JsonObject
            {
                ["event"] = "tool_response",
                ["isError"] = result.IsError,
                ["contentCount"] = contentCount,
                ["contentTypes"] = JsonNode.Parse(JsonSerializer.Serialize(contentTypes)),
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Logs resource read requests.
    /// </summary>
    [McpServerInterceptor(
        Name = "resource-request-logger",
        Description = "Logs resource read request details",
        Events = [InterceptorEvents.ResourcesRead],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogResourceRequest(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var uri = payload["uri"]?.GetValue<string>() ?? "unknown";

        // Track resource read counts by URI pattern
        var uriPattern = ExtractUriPattern(uri);
        ResourceReadCounts.AddOrUpdate(uriPattern, 1, (_, count) => count + 1);

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = "resource_request",
                ["uri"] = uri,
                ["uriPattern"] = uriPattern,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["totalReadsToPattern"] = ResourceReadCounts.GetValueOrDefault(uriPattern, 0)
            }
        };
    }

    /// <summary>
    /// Logs resource read responses including content size.
    /// </summary>
    [McpServerInterceptor(
        Name = "resource-response-logger",
        Description = "Logs resource read response details",
        Events = [InterceptorEvents.ResourcesRead],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult LogResourceResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        ReadResourceResult? result;
        try
        {
            result = payload.Deserialize<ReadResourceResult>();
        }
        catch (JsonException)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        if (result is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var contentCount = result.Contents?.Count ?? 0;
        long totalSize = 0;

        if (result.Contents is not null)
        {
            foreach (var content in result.Contents)
            {
                if (content is TextResourceContents textContent)
                {
                    totalSize += textContent.Text?.Length ?? 0;
                }
                else if (content is BlobResourceContents blobContent)
                {
                    totalSize += blobContent.Blob?.Length ?? 0;
                }
            }
        }

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["content_count"] = contentCount,
                ["total_size_bytes"] = totalSize
            },
            Info = new JsonObject
            {
                ["event"] = "resource_response",
                ["contentCount"] = contentCount,
                ["totalSizeBytes"] = totalSize,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Tracks prompt list requests for analytics.
    /// </summary>
    [McpServerInterceptor(
        Name = "prompt-request-logger",
        Description = "Logs prompt list and get request details",
        Events = [InterceptorEvents.PromptsList, InterceptorEvents.PromptsGet],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogPromptRequest(JsonNode? payload, string @event)
    {
        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = @event,
                ["payload"] = payload?.ToJsonString() ?? "null",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Collects aggregate metrics for dashboards.
    /// </summary>
    [McpServerInterceptor(
        Name = "metrics-collector",
        Description = "Collects aggregate metrics across all operations",
        Events = [InterceptorEvents.ToolsCall, InterceptorEvents.ResourcesRead],
        Phase = InterceptorPhase.Response)]
    public ObservabilityInterceptorResult CollectMetrics(JsonNode? payload, string @event)
    {
        // Calculate aggregate metrics
        var totalToolCalls = ToolCallCounts.Values.Sum();
        var totalResourceReads = ResourceReadCounts.Values.Sum();
        var uniqueTools = ToolCallCounts.Count;
        var uniqueResourcePatterns = ResourceReadCounts.Count;

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Metrics = new Dictionary<string, double>
            {
                ["total_tool_calls"] = totalToolCalls,
                ["total_resource_reads"] = totalResourceReads,
                ["unique_tools_used"] = uniqueTools,
                ["unique_resource_patterns"] = uniqueResourcePatterns
            },
            Info = new JsonObject
            {
                ["event"] = "metrics_snapshot",
                ["triggeringEvent"] = @event,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Generates alerts for suspicious activity patterns.
    /// </summary>
    [McpServerInterceptor(
        Name = "anomaly-detector",
        Description = "Detects anomalous patterns and generates alerts",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult DetectAnomalies(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return new ObservabilityInterceptorResult { Observed = false };
        }

        var alerts = new List<ObservabilityAlert>();

        // Check for rapid-fire requests to the same tool
        var toolName = toolCall?.Name ?? "unknown";
        var callCount = ToolCallCounts.GetValueOrDefault(toolName, 0);

        if (callCount > 100)
        {
            alerts.Add(new ObservabilityAlert
            {
                Level = "warning",
                Message = $"High volume of calls to tool '{toolName}': {callCount} calls",
                Tags = ["rate_limiting", "abuse_prevention"]
            });
        }

        // Check for unusually large payloads
        var payloadSize = payload.ToJsonString().Length;
        if (payloadSize > 10000)
        {
            alerts.Add(new ObservabilityAlert
            {
                Level = "info",
                Message = $"Large payload detected: {payloadSize} bytes",
                Tags = ["performance", "payload_size"]
            });
        }

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Alerts = alerts.Count > 0 ? alerts : null,
            Info = new JsonObject
            {
                ["event"] = "anomaly_check",
                ["tool"] = toolName,
                ["payloadSize"] = payloadSize,
                ["alertCount"] = alerts.Count,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Audit logger for compliance purposes.
    /// </summary>
    [McpServerInterceptor(
        Name = "audit-logger",
        Description = "Creates audit trail for compliance requirements",
        Events = [InterceptorEvents.ToolsCall, InterceptorEvents.ResourcesRead, InterceptorEvents.PromptsList, InterceptorEvents.PromptsGet],
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult CreateAuditEntry(JsonNode? payload, string @event)
    {
        // In production, this would write to a secure audit log
        var auditEntry = new JsonObject
        {
            ["auditId"] = Guid.NewGuid().ToString(),
            ["event"] = @event,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["payloadHash"] = ComputePayloadHash(payload),
            ["sourceIp"] = "127.0.0.1", // Would be extracted from context in production
            ["userId"] = "system" // Would be extracted from authentication context
        };

        return new ObservabilityInterceptorResult
        {
            Observed = true,
            Info = new JsonObject
            {
                ["event"] = "audit_entry_created",
                ["auditEntry"] = auditEntry
            }
        };
    }

    private static string ExtractUriPattern(string uri)
    {
        // Extract pattern from URI (e.g., file:///path/to/file.txt -> file:///**/*)
        try
        {
            var uriObj = new Uri(uri);
            return $"{uriObj.Scheme}://{uriObj.Host}/**";
        }
        catch
        {
            return uri.Split('/').FirstOrDefault() ?? "unknown";
        }
    }

    private static string ComputePayloadHash(JsonNode? payload)
    {
        if (payload is null)
        {
            return "null";
        }

        var json = payload.ToJsonString();
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes)[..16]; // First 16 chars of hash
    }
}
