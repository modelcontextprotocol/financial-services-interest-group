using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents the desired output format for LLM responses.
/// </summary>
/// <remarks>
/// Based on the OpenAI response format specification in SEP-1763.
/// </remarks>
public sealed class LlmResponseFormat
{
    /// <summary>
    /// Gets or sets the type of response format.
    /// </summary>
    /// <remarks>
    /// Valid values are "text" for plain text output or "json_object" for JSON output.
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Creates a text response format.
    /// </summary>
    public static LlmResponseFormat Text => new() { Type = "text" };

    /// <summary>
    /// Creates a JSON object response format.
    /// </summary>
    public static LlmResponseFormat JsonObject => new() { Type = "json_object" };
}
