using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Server-side validation interceptors for MCP operations.
/// These interceptors can be deployed as a separate MCP interceptor service
/// to enforce policies across multiple clients centrally.
/// </summary>
[McpServerInterceptorType]
public partial class ServerValidationInterceptors
{
    // PII patterns
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b(?:\+?1[-.\s]?)?(?:\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled);

    // Security patterns
    private static readonly string[] SqlInjectionPatterns =
    [
        "'; DROP TABLE", "'; DELETE FROM", "' OR '1'='1", "' OR 1=1",
        "'; EXEC ", "'; INSERT INTO", "UNION SELECT", "-- ", "/*", "*/"
    ];

    private static readonly string[] CommandInjectionPatterns =
    [
        "; rm -rf", "| rm -rf", "&& rm -rf", "; cat /etc/passwd",
        "| cat /etc/passwd", "`rm ", "$(rm ", "; wget ", "| curl ",
        "; chmod ", "; nc ", "| nc "
    ];

    private static readonly string[] PathTraversalPatterns =
    [
        "../", "..\\", "/etc/passwd", "/etc/shadow",
        "C:\\Windows\\System32", "%2e%2e%2f", "%2e%2e/"
    ];

    /// <summary>
    /// Validates tool call arguments for PII (Personally Identifiable Information).
    /// Blocks requests containing SSN or credit card numbers, warns on email/phone.
    /// </summary>
    [McpServerInterceptor(
        Name = "pii-validator",
        Description = "Validates tool call arguments for PII leakage",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -1000)] // Security interceptors run early
    public ValidationInterceptorResult ValidatePii(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        // Parse as tool call request to inspect arguments
        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return ValidationInterceptorResult.Success(); // Can't parse, let other validators handle
        }

        if (toolCall?.Arguments is null)
        {
            return ValidationInterceptorResult.Success();
        }

        // Check each argument for PII
        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value.ToString() ?? string.Empty;

            if (SsnPattern.IsMatch(value))
            {
                messages.Add(new ValidationMessage
                {
                    Path = $"arguments.{arg.Key}",
                    Message = "Social Security Number detected - PII not allowed in tool arguments",
                    Severity = ValidationSeverity.Error
                });
            }

            if (CreditCardPattern.IsMatch(value))
            {
                messages.Add(new ValidationMessage
                {
                    Path = $"arguments.{arg.Key}",
                    Message = "Credit card number detected - PII not allowed in tool arguments",
                    Severity = ValidationSeverity.Error
                });
            }

            // Email and phone are warnings, not errors
            if (EmailPattern.IsMatch(value))
            {
                messages.Add(new ValidationMessage
                {
                    Path = $"arguments.{arg.Key}",
                    Message = "Email address detected - consider if PII is necessary",
                    Severity = ValidationSeverity.Warn
                });
            }

            if (PhonePattern.IsMatch(value))
            {
                messages.Add(new ValidationMessage
                {
                    Path = $"arguments.{arg.Key}",
                    Message = "Phone number detected - consider if PII is necessary",
                    Severity = ValidationSeverity.Warn
                });
            }
        }

        if (messages.Count > 0)
        {
            var maxSeverity = messages.Max(m => m.Severity);
            return new ValidationInterceptorResult
            {
                Valid = maxSeverity != ValidationSeverity.Error,
                Severity = maxSeverity,
                Messages = messages
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Validates tool call arguments for SQL injection patterns.
    /// </summary>
    [McpServerInterceptor(
        Name = "sql-injection-validator",
        Description = "Detects potential SQL injection in tool arguments",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -900)]
    public ValidationInterceptorResult ValidateSqlInjection(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return ValidationInterceptorResult.Success();
        }

        if (toolCall?.Arguments is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value.ToString() ?? string.Empty;

            foreach (var pattern in SqlInjectionPatterns)
            {
                if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = $"Potential SQL injection detected: suspicious pattern found",
                        Severity = ValidationSeverity.Error
                    });
                    break; // One detection per argument is enough
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
    /// Validates tool call arguments for command injection patterns.
    /// </summary>
    [McpServerInterceptor(
        Name = "command-injection-validator",
        Description = "Detects potential command injection in tool arguments",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -900)]
    public ValidationInterceptorResult ValidateCommandInjection(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return ValidationInterceptorResult.Success();
        }

        if (toolCall?.Arguments is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value.ToString() ?? string.Empty;

            foreach (var pattern in CommandInjectionPatterns)
            {
                if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = $"Potential command injection detected: suspicious pattern found",
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
    /// Validates tool call arguments for path traversal patterns.
    /// </summary>
    [McpServerInterceptor(
        Name = "path-traversal-validator",
        Description = "Detects potential path traversal in tool arguments",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Request,
        PriorityHint = -800)]
    public ValidationInterceptorResult ValidatePathTraversal(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        CallToolRequestParams? toolCall;
        try
        {
            toolCall = payload.Deserialize<CallToolRequestParams>();
        }
        catch (JsonException)
        {
            return ValidationInterceptorResult.Success();
        }

        if (toolCall?.Arguments is null)
        {
            return ValidationInterceptorResult.Success();
        }

        var messages = new List<ValidationMessage>();

        foreach (var arg in toolCall.Arguments)
        {
            var value = arg.Value.ToString() ?? string.Empty;

            foreach (var pattern in PathTraversalPatterns)
            {
                if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add(new ValidationMessage
                    {
                        Path = $"arguments.{arg.Key}",
                        Message = $"Potential path traversal detected: '{pattern}' found",
                        Severity = ValidationSeverity.Warn
                    });
                    break;
                }
            }
        }

        if (messages.Count > 0)
        {
            return new ValidationInterceptorResult
            {
                Valid = true, // Warnings don't block
                Severity = ValidationSeverity.Warn,
                Messages = messages
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Validates that tool responses don't contain error indicators.
    /// </summary>
    [McpServerInterceptor(
        Name = "response-error-validator",
        Description = "Validates tool call responses for errors",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response)]
    public ValidationInterceptorResult ValidateResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        CallToolResult? result;
        try
        {
            result = payload.Deserialize<CallToolResult>();
        }
        catch (JsonException)
        {
            return ValidationInterceptorResult.Success();
        }

        if (result?.IsError == true)
        {
            var errorMessage = result.Content?.FirstOrDefault() switch
            {
                TextContentBlock text => text.Text,
                _ => "Tool execution failed"
            };

            return new ValidationInterceptorResult
            {
                Valid = true, // Don't block errors, just report them
                Severity = ValidationSeverity.Warn,
                Messages = [new() { Message = $"Tool returned error: {errorMessage}", Severity = ValidationSeverity.Warn }]
            };
        }

        return ValidationInterceptorResult.Success();
    }

    /// <summary>
    /// Validates resource read requests for allowed paths.
    /// </summary>
    [McpServerInterceptor(
        Name = "resource-path-validator",
        Description = "Validates resource read requests against allowed paths",
        Events = [InterceptorEvents.ResourcesRead],
        Phase = InterceptorPhase.Request)]
    public ValidationInterceptorResult ValidateResourcePath(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Error("Resource read payload is required");
        }

        var uri = payload["uri"]?.GetValue<string>();
        if (string.IsNullOrEmpty(uri))
        {
            return ValidationInterceptorResult.Error("Resource URI is required", "uri");
        }

        // Block access to sensitive paths
        var blockedPatterns = new[] { "/etc/", "/proc/", "/sys/", "C:\\Windows\\", "file:///etc/" };
        foreach (var pattern in blockedPatterns)
        {
            if (uri.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationInterceptorResult.Error($"Access to sensitive path blocked: {pattern}", "uri");
            }
        }

        return ValidationInterceptorResult.Success();
    }
}
