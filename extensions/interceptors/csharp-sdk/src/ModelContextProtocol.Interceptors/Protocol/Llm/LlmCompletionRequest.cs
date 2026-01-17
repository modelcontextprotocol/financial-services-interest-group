using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents an LLM chat completion request using the common OpenAI-compatible format.
/// </summary>
/// <remarks>
/// <para>
/// Based on the SEP-1763 specification for the <c>llm/completion</c> event payload.
/// This format provides a provider-agnostic way to represent LLM completion requests,
/// enabling interceptors to work across different LLM providers (OpenAI, Azure, Anthropic, etc.).
/// </para>
/// <para>
/// This type is used for both server-side interceptors (intercepting requests via MCP protocol)
/// and client-side interceptors (intercepting direct LLM API calls).
/// </para>
/// </remarks>
public sealed class LlmCompletionRequest
{
    /// <summary>
    /// Gets or sets the list of messages comprising the conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<LlmMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sampling temperature between 0 and 2.
    /// </summary>
    /// <remarks>
    /// Higher values like 0.8 make output more random, while lower values like 0.2 make it more focused.
    /// </remarks>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the nucleus sampling probability (0-1).
    /// </summary>
    /// <remarks>
    /// An alternative to temperature. Only the tokens comprising the top_p probability mass are considered.
    /// </remarks>
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    /// <summary>
    /// Gets or sets the frequency penalty (-2.0 to 2.0).
    /// </summary>
    /// <remarks>
    /// Positive values penalize new tokens based on their existing frequency in the text so far.
    /// </remarks>
    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Gets or sets the presence penalty (-2.0 to 2.0).
    /// </summary>
    /// <remarks>
    /// Positive values penalize new tokens based on whether they appear in the text so far.
    /// </remarks>
    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Gets or sets the stop sequences.
    /// </summary>
    /// <remarks>
    /// Up to 4 sequences where the API will stop generating further tokens.
    /// </remarks>
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? Stop { get; set; }

    /// <summary>
    /// Gets or sets the tools (functions) available to the model.
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<LlmTool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets the tool choice specification.
    /// </summary>
    /// <remarks>
    /// Controls which (if any) function is called by the model.
    /// </remarks>
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmToolChoice? ToolChoice { get; set; }

    /// <summary>
    /// Gets or sets the response format specification.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the random seed for deterministic sampling.
    /// </summary>
    /// <remarks>
    /// If specified, the system will make a best effort to sample deterministically.
    /// </remarks>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Seed { get; set; }

    /// <summary>
    /// Gets or sets a unique identifier representing the end-user.
    /// </summary>
    /// <remarks>
    /// Used to monitor and detect abuse.
    /// </remarks>
    [JsonPropertyName("user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the number of completions to generate (default 1).
    /// </summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    /// <summary>
    /// Gets or sets whether to stream partial progress (not applicable for interceptors).
    /// </summary>
    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }

    /// <summary>
    /// Gets or sets additional provider-specific metadata.
    /// </summary>
    /// <remarks>
    /// Reserved for MCP-level metadata or provider extensions.
    /// </remarks>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonObject? Meta { get; set; }
}
