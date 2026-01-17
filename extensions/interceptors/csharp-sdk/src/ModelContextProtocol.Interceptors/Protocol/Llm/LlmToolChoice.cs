using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents a tool choice specification for controlling tool use.
/// </summary>
/// <remarks>
/// Based on the OpenAI tool_choice specification in SEP-1763.
/// Can be "none", "auto", or a specific function choice.
/// </remarks>
public sealed class LlmToolChoice
{
    /// <summary>
    /// Gets or sets a string value for simple choices ("none" or "auto").
    /// </summary>
    /// <remarks>
    /// When this is set, <see cref="Type"/> and <see cref="Function"/> should be null.
    /// </remarks>
    [JsonPropertyName("choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Choice { get; set; }

    /// <summary>
    /// Gets or sets the type for specific function choice ("function").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the function specification for specific function choice.
    /// </summary>
    [JsonPropertyName("function")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmToolChoiceFunction? Function { get; set; }

    /// <summary>
    /// Gets whether this represents the "none" choice (no tools will be called).
    /// </summary>
    [JsonIgnore]
    public bool IsNone => Choice == "none";

    /// <summary>
    /// Gets whether this represents the "auto" choice (model decides).
    /// </summary>
    [JsonIgnore]
    public bool IsAuto => Choice == "auto";

    /// <summary>
    /// Gets whether this represents a specific function choice.
    /// </summary>
    [JsonIgnore]
    public bool IsSpecificFunction => Type == "function" && Function is not null;

    /// <summary>
    /// Creates a "none" tool choice (no tools will be called).
    /// </summary>
    public static LlmToolChoice None => new() { Choice = "none" };

    /// <summary>
    /// Creates an "auto" tool choice (model decides).
    /// </summary>
    public static LlmToolChoice Auto => new() { Choice = "auto" };

    /// <summary>
    /// Creates a specific function tool choice.
    /// </summary>
    /// <param name="functionName">The name of the function to call.</param>
    /// <returns>A tool choice for the specific function.</returns>
    public static LlmToolChoice ForFunction(string functionName) =>
        new() { Type = "function", Function = new() { Name = functionName } };
}

/// <summary>
/// Represents a specific function choice.
/// </summary>
public sealed class LlmToolChoiceFunction
{
    /// <summary>
    /// Gets or sets the name of the function to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
