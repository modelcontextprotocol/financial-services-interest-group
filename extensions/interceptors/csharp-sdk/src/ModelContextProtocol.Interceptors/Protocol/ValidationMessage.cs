using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents a validation message with path, message, and severity.
/// </summary>
public sealed class ValidationMessage
{
    /// <summary>
    /// Gets or sets the JSON path to the field being validated.
    /// </summary>
    /// <remarks>
    /// For example, "params.arguments.location" indicates the location field
    /// within the arguments of the params object.
    /// </remarks>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the validation message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the severity of this validation message.
    /// </summary>
    [JsonPropertyName("severity")]
    public ValidationSeverity Severity { get; set; }
}
