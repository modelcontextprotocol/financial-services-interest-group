using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents the reason why a model stopped generating tokens.
/// </summary>
/// <remarks>
/// Based on the OpenAI chat completion finish reasons as specified in SEP-1763.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<LlmFinishReason>))]
public enum LlmFinishReason
{
    /// <summary>
    /// Natural stop point reached or stop sequence encountered.
    /// </summary>
    [JsonPropertyName("stop")]
    Stop,

    /// <summary>
    /// Maximum token limit reached.
    /// </summary>
    [JsonPropertyName("length")]
    Length,

    /// <summary>
    /// Model decided to call one or more tools.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    ToolCalls,

    /// <summary>
    /// Content was filtered due to content policy.
    /// </summary>
    [JsonPropertyName("content_filter")]
    ContentFilter
}
