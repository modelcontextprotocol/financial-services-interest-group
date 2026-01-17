using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Sample validation interceptors for MCP client operations.
/// These interceptors demonstrate PII detection and request validation.
/// </summary>
[McpClientInterceptorType]
public partial class ClientValidationInterceptors
{
    // Common PII patterns
    private static readonly Regex SsnPattern = SsnRegex();
    private static readonly Regex EmailPattern = EmailRegex();
    private static readonly Regex CreditCardPattern = CreditCardRegex();

    /// <summary>
    /// Validates outgoing tool call arguments for PII (Personally Identifiable Information).
    /// Blocks requests containing SSN, email addresses, or credit card numbers.
    /// </summary>
    [McpClientInterceptor(
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

            // Email is a warning, not an error
            if (EmailPattern.IsMatch(value))
            {
                messages.Add(new ValidationMessage
                {
                    Path = $"arguments.{arg.Key}",
                    Message = "Email address detected - consider if PII is necessary",
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
    /// Validates that tool responses don't contain error indicators that should be handled.
    /// </summary>
    [McpClientInterceptor(
        Name = "response-validator",
        Description = "Validates tool call responses for errors",
        Events = [InterceptorEvents.ToolsCall],
        Phase = InterceptorPhase.Response)]
    public ValidationInterceptorResult ValidateResponse(JsonNode? payload)
    {
        if (payload is null)
        {
            return ValidationInterceptorResult.Success();
        }

        // Parse as tool call result
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
            // Extract error message from content if available
            var errorMessage = result.Content?.FirstOrDefault() switch
            {
                TextContentBlock text => text.Text,
                _ => "Tool execution failed"
            };

            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Warn, // Warning so it doesn't block, but is logged
                Messages = [new() { Message = $"Tool returned error: {errorMessage}", Severity = ValidationSeverity.Warn }]
            };
        }

        return ValidationInterceptorResult.Success();
    }

#if NET
    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\d{4}[-\s]?){3}\d{4}\b")]
    private static partial Regex CreditCardRegex();
#else
    private static Regex SsnRegex() => new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static Regex EmailRegex() => new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);
    private static Regex CreditCardRegex() => new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
#endif
}
