using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// A sample validation interceptor that validates tool call parameters for security issues.
/// This interceptor can be deployed as a separate MCP service to validate requests
/// before they reach the actual tool implementation.
/// </summary>
[McpServerInterceptorType]
public class ParameterValidator
{
    /// <summary>
    /// Validates tool call parameters for potential security issues.
    /// </summary>
    [McpServerInterceptor(
        Name = "parameter-validator",
        Description = "Validates tool call parameters for security issues",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request)]
    public ValidationInterceptorResult ValidateToolCall(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = "Payload is required", Severity = ValidationSeverity.Error }]
            };
        }

        // Parse the tool call request
        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException ex)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = $"Invalid payload format: {ex.Message}", Severity = ValidationSeverity.Error }]
            };
        }

        if (toolCall is null || string.IsNullOrEmpty(toolCall.Name))
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = "Tool name is required", Severity = ValidationSeverity.Error }]
            };
        }

        // Validate tool arguments for security issues
        var messages = new List<ValidationMessage>();

        if (toolCall.Arguments is not null)
        {
            foreach (var arg in toolCall.Arguments)
            {
                var value = arg.Value.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                // Check for SQL injection patterns
                if (ContainsSqlInjectionPattern(value))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = "Potentially malicious SQL content detected",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Check for command injection patterns
                if (ContainsCommandInjectionPattern(value))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = "Potentially malicious command content detected",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Check for path traversal patterns
                if (ContainsPathTraversalPattern(value))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = "Potentially malicious path traversal detected",
                        Severity = ValidationSeverity.Warn
                    });
                }
            }
        }

        if (messages.Count > 0)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = messages.Max(m => m.Severity),
                Messages = messages
            };
        }

        return new ValidationInterceptorResult { Valid = true };
    }

    /// <summary>
    /// Validates that required fields are present in tool calls.
    /// </summary>
    [McpServerInterceptor(
        Name = "required-fields-validator",
        Description = "Validates that required fields are present in tool calls",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request,
        PriorityHint = 10)]
    public ValidationInterceptorResult ValidateRequiredFields(JsonNode? payload, string @event)
    {
        if (payload is null)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = $"Payload is required for event '{@event}'", Severity = ValidationSeverity.Error }]
            };
        }

        return new ValidationInterceptorResult { Valid = true };
    }

    private static bool ContainsSqlInjectionPattern(string value)
    {
        var patterns = new[]
        {
            "'; DROP TABLE",
            "'; DELETE FROM",
            "' OR '1'='1",
            "' OR 1=1",
            "'; EXEC ",
            "'; INSERT INTO",
            "UNION SELECT",
            "-- "
        };

        return patterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsCommandInjectionPattern(string value)
    {
        var patterns = new[]
        {
            "; rm -rf",
            "| rm -rf",
            "&& rm -rf",
            "; cat /etc/passwd",
            "| cat /etc/passwd",
            "`rm -rf`",
            "$(rm -rf",
            "; wget ",
            "| wget ",
            "; curl "
        };

        return patterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsPathTraversalPattern(string value)
    {
        var patterns = new[]
        {
            "../",
            "..\\",
            "/etc/passwd",
            "/etc/shadow",
            "C:\\Windows\\System32"
        };

        return patterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
