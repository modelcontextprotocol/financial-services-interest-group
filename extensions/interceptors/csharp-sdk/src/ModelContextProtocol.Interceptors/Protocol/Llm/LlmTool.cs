using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents a tool/function definition for LLM tool use.
/// </summary>
/// <remarks>
/// Based on the OpenAI tools specification in SEP-1763.
/// </remarks>
public sealed class LlmTool
{
    /// <summary>
    /// Gets or sets the type of tool (always "function" currently).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function definition.
    /// </summary>
    [JsonPropertyName("function")]
    public LlmFunctionDefinition Function { get; set; } = new();

    /// <summary>
    /// Creates a tool definition from a function specification.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="description">Optional description of what the function does.</param>
    /// <param name="parameters">Optional JSON Schema for the function parameters.</param>
    /// <returns>A new tool definition.</returns>
    public static LlmTool Create(string name, string? description = null, JsonElement? parameters = null) =>
        new()
        {
            Type = "function",
            Function = new()
            {
                Name = name,
                Description = description,
                Parameters = parameters
            }
        };
}

/// <summary>
/// Represents a function definition within a tool.
/// </summary>
public sealed class LlmFunctionDefinition
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the function does.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the JSON Schema for the function parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Parameters { get; set; }
}
