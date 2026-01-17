using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents an LLM chat completion response using the common OpenAI-compatible format.
/// </summary>
/// <remarks>
/// <para>
/// Based on the SEP-1763 specification for the <c>llm/completion</c> event response payload.
/// This format provides a provider-agnostic way to represent LLM completion responses,
/// enabling interceptors to work across different LLM providers (OpenAI, Azure, Anthropic, etc.).
/// </para>
/// <para>
/// This type is used for both server-side interceptors (intercepting responses via MCP protocol)
/// and client-side interceptors (intercepting direct LLM API responses).
/// </para>
/// </remarks>
public sealed class LlmCompletionResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this completion.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the object type (always "chat.completion").
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    /// <summary>
    /// Gets or sets the Unix timestamp when the completion was created.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the model used for the completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of completion choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public IList<LlmChoice> Choices { get; set; } = [];

    /// <summary>
    /// Gets or sets the token usage statistics.
    /// </summary>
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmUsage? Usage { get; set; }

    /// <summary>
    /// Gets or sets the system fingerprint for the model configuration.
    /// </summary>
    [JsonPropertyName("system_fingerprint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemFingerprint { get; set; }

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
