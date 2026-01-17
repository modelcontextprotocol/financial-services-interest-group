using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents the role of a message participant in an LLM conversation.
/// </summary>
/// <remarks>
/// Based on the OpenAI chat completion message roles as specified in SEP-1763.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<LlmMessageRole>))]
public enum LlmMessageRole
{
    /// <summary>
    /// System message providing instructions or context to the model.
    /// </summary>
    [JsonPropertyName("system")]
    System,

    /// <summary>
    /// Message from the user/human.
    /// </summary>
    [JsonPropertyName("user")]
    User,

    /// <summary>
    /// Message from the AI assistant.
    /// </summary>
    [JsonPropertyName("assistant")]
    Assistant,

    /// <summary>
    /// Message containing tool/function call results.
    /// </summary>
    [JsonPropertyName("tool")]
    Tool
}
