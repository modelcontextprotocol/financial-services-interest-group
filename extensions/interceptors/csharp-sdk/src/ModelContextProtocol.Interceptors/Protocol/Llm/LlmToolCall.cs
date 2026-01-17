using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents a tool/function call made by the model.
/// </summary>
/// <remarks>
/// Based on the OpenAI tool call specification in SEP-1763.
/// </remarks>
public sealed class LlmToolCall
{
    /// <summary>
    /// Gets or sets the unique identifier for this tool call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of tool call (always "function" currently).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function call details.
    /// </summary>
    [JsonPropertyName("function")]
    public LlmFunctionCall Function { get; set; } = new();
}

/// <summary>
/// Represents a function call within a tool call.
/// </summary>
public sealed class LlmFunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the function as a JSON string.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
