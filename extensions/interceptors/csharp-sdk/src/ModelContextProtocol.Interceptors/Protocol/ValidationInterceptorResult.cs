using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the result of invoking a validation interceptor.
/// </summary>
public sealed class ValidationInterceptorResult
{
    /// <summary>
    /// Gets or sets the name of the interceptor that produced this result.
    /// </summary>
    [JsonPropertyName("interceptor")]
    public string? Interceptor { get; set; }

    /// <summary>
    /// Gets or sets the type of interceptor (always "validation" for this result type).
    /// </summary>
    [JsonPropertyName("type")]
    public InterceptorType Type { get; set; } = InterceptorType.Validation;

    /// <summary>
    /// Gets or sets the phase when this interceptor executed.
    /// </summary>
    [JsonPropertyName("phase")]
    public InterceptorPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets additional interceptor-specific information.
    /// </summary>
    [JsonPropertyName("info")]
    public JsonObject? Info { get; set; }

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
