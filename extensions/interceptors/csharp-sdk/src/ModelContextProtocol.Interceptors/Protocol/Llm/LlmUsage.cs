using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents token usage statistics for an LLM completion.
/// </summary>
/// <remarks>
/// Based on the OpenAI usage specification in SEP-1763.
/// </remarks>
public sealed class LlmUsage
{
    /// <summary>
    /// Gets or sets the number of tokens in the prompt.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens in the completion.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens used (prompt + completion).
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
