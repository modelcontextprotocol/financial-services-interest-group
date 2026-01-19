namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Exception thrown when an interceptor validation fails with error severity, blocking execution.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <see cref="InterceptingMcpClient"/> when a validation interceptor
/// returns a result with <see cref="ValidationSeverity.Error"/> severity. According to SEP-1763,
/// only error-severity validations block execution; info and warning severities are recorded but
/// do not prevent the operation from proceeding.
/// </para>
/// <para>
/// The exception contains the full <see cref="InterceptorChainResult"/> which provides detailed
/// information about all interceptors that executed, including validation messages, mutation results,
/// and timing information.
/// </para>
/// </remarks>
/// <example>
/// Handling interceptor validation failures:
/// <code>
/// try
/// {
///     var result = await interceptedClient.CallToolAsync("sensitive-tool", args);
/// }
/// catch (McpInterceptorValidationException ex)
/// {
///     Console.WriteLine($"Blocked by interceptor: {ex.AbortedAt?.Interceptor}");
///     Console.WriteLine($"Reason: {ex.AbortedAt?.Reason}");
///     
///     foreach (var validation in ex.ValidationResults)
///     {
///         foreach (var message in validation.Messages ?? [])
///         {
///             Console.WriteLine($"  [{message.Severity}] {message.Path}: {message.Message}");
///         }
///     }
/// }
/// </code>
/// </example>
public sealed class McpInterceptorValidationException : McpException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpInterceptorValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="chainResult">The full chain execution result.</param>
    public McpInterceptorValidationException(string message, InterceptorChainResult chainResult)
        : base(message)
    {
        Throw.IfNull(chainResult);
        ChainResult = chainResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpInterceptorValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="chainResult">The full chain execution result.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public McpInterceptorValidationException(string message, InterceptorChainResult chainResult, Exception? innerException)
        : base(message, innerException)
    {
        Throw.IfNull(chainResult);
        ChainResult = chainResult;
    }

    /// <summary>
    /// Gets the full chain execution result containing all interceptor results.
    /// </summary>
    /// <remarks>
    /// The chain result includes:
    /// <list type="bullet">
    /// <item><description>All validation results with their messages and severities</description></item>
    /// <item><description>All mutation results with any modifications made</description></item>
    /// <item><description>All observability results</description></item>
    /// <item><description>The final payload state before the chain was aborted</description></item>
    /// <item><description>Timing information for each interceptor</description></item>
    /// </list>
    /// </remarks>
    public InterceptorChainResult ChainResult { get; }

    /// <summary>
    /// Gets the event type that was being processed when validation failed.
    /// </summary>
    /// <remarks>
    /// This will be one of the <see cref="InterceptorEvents"/> constants, such as
    /// <see cref="InterceptorEvents.ToolsCall"/> or <see cref="InterceptorEvents.ToolsList"/>.
    /// </remarks>
    public string? Event => ChainResult.Event;

    /// <summary>
    /// Gets the phase of execution when validation failed.
    /// </summary>
    public InterceptorPhase Phase => ChainResult.Phase;

    /// <summary>
    /// Gets information about which interceptor caused the chain to abort.
    /// </summary>
    /// <remarks>
    /// Contains the interceptor name, the reason for aborting, and the type of abort (e.g., "validation").
    /// </remarks>
    public ChainAbortInfo? AbortedAt => ChainResult.AbortedAt;

    /// <summary>
    /// Gets the validation summary with counts of errors, warnings, and info messages.
    /// </summary>
    public ValidationSummary ValidationSummary => ChainResult.ValidationSummary;

    /// <summary>
    /// Gets all validation results from the chain execution.
    /// </summary>
    /// <remarks>
    /// This includes both passing and failing validations. Use this to get detailed
    /// information about all validation messages, including paths, severities, and suggestions.
    /// </remarks>
    public IEnumerable<ValidationInterceptorResult> ValidationResults =>
        ChainResult.Results.OfType<ValidationInterceptorResult>();

    /// <summary>
    /// Gets only the validation results that failed (severity = error).
    /// </summary>
    public IEnumerable<ValidationInterceptorResult> FailedValidations =>
        ValidationResults.Where(v => !v.Valid && v.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Creates a formatted message describing all validation failures.
    /// </summary>
    /// <returns>A string containing all validation error messages.</returns>
    public string GetDetailedMessage()
    {
        var messages = new List<string>();

        if (AbortedAt is not null)
        {
            messages.Add($"Interceptor chain aborted by '{AbortedAt.Interceptor}': {AbortedAt.Reason}");
        }

        foreach (var validation in FailedValidations)
        {
            if (validation.Messages is not null)
            {
                foreach (var msg in validation.Messages.Where(m => m.Severity == ValidationSeverity.Error))
                {
                    var path = string.IsNullOrEmpty(msg.Path) ? "" : $" at '{msg.Path}'";
                    messages.Add($"[{validation.Interceptor ?? "unknown"}]{path}: {msg.Message}");
                }
            }
        }

        return messages.Count > 0
            ? string.Join(Environment.NewLine, messages)
            : Message;
    }
}
