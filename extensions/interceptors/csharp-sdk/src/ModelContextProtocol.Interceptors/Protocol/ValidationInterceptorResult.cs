using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the result of invoking a validation interceptor.
/// </summary>
/// <remarks>
/// <para>
/// Validation interceptors validate messages and can block execution if validation fails
/// with <see cref="ValidationSeverity.Error"/>. Info and Warning severities do not block.
/// </para>
/// </remarks>
public sealed class ValidationInterceptorResult : InterceptorResult
{
    /// <summary>
    /// Gets the type of interceptor (always "validation" for this result type).
    /// </summary>
    [JsonPropertyName("type")]
    public override InterceptorType Type => InterceptorType.Validation;

    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    /// <summary>
    /// Gets or sets the overall validation severity.
    /// </summary>
    /// <remarks>
    /// Only <see cref="ValidationSeverity.Error"/> blocks execution.
    /// </remarks>
    [JsonPropertyName("severity")]
    public ValidationSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets detailed validation messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<ValidationMessage>? Messages { get; set; }

    /// <summary>
    /// Gets or sets optional suggested corrections.
    /// </summary>
    [JsonPropertyName("suggestions")]
    public IList<ValidationSuggestion>? Suggestions { get; set; }

    /// <summary>
    /// Gets or sets an optional cryptographic signature for this validation result.
    /// </summary>
    /// <remarks>
    /// Reserved for future use to enable verification that validation occurred at trust boundaries.
    /// </remarks>
    [JsonPropertyName("signature")]
    public ValidationSignature? Signature { get; set; }

    /// <summary>
    /// Creates a validation result indicating success.
    /// </summary>
    /// <returns>A validation result with <see cref="Valid"/> set to true.</returns>
    public static ValidationInterceptorResult Success() => new() { Valid = true };

    /// <summary>
    /// Creates a validation result indicating failure with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="path">Optional path to the invalid field.</param>
    /// <returns>A validation result with <see cref="Valid"/> set to false and error severity.</returns>
    public static ValidationInterceptorResult Error(string message, string? path = null) => new()
    {
        Valid = false,
        Severity = ValidationSeverity.Error,
        Messages = [new() { Message = message, Severity = ValidationSeverity.Error, Path = path }]
    };

    /// <summary>
    /// Creates a validation result with a warning that does not block execution.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="path">Optional path to the field with the warning.</param>
    /// <returns>A validation result with <see cref="Valid"/> set to true and warning severity.</returns>
    public static ValidationInterceptorResult Warning(string message, string? path = null) => new()
    {
        Valid = true,
        Severity = ValidationSeverity.Warn,
        Messages = [new() { Message = message, Severity = ValidationSeverity.Warn, Path = path }]
    };
}

/// <summary>
/// Represents a cryptographic signature for validation results.
/// </summary>
/// <remarks>
/// Reserved for future use to enable cryptographic verification of validation results at trust boundaries.
/// </remarks>
public sealed class ValidationSignature
{
    /// <summary>
    /// Gets or sets the signature algorithm.
    /// </summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "ed25519";

    /// <summary>
    /// Gets or sets the public key used for verification.
    /// </summary>
    [JsonPropertyName("publicKey")]
    public required string PublicKey { get; set; }

    /// <summary>
    /// Gets or sets the signature value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
