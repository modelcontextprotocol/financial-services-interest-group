using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents a suggested correction for a validation issue.
/// </summary>
public sealed class ValidationSuggestion
{
    /// <summary>
    /// Gets or sets the JSON path to the field that should be corrected.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the suggested value for the field.
    /// </summary>
    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }
}
