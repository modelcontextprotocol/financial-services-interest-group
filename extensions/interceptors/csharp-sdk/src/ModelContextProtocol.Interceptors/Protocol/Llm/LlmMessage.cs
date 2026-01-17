using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents a message in an LLM conversation.
/// </summary>
/// <remarks>
/// <para>
/// Based on the OpenAI chat completion message format as specified in SEP-1763.
/// This provides a common, provider-agnostic format for LLM messages.
/// </para>
/// <para>
/// The content can be either a simple string or an array of content parts for multimodal content.
/// Use <see cref="Content"/> for simple text and <see cref="ContentParts"/> for multimodal.
/// </para>
/// </remarks>
public sealed class LlmMessage
{
    /// <summary>
    /// Gets or sets the role of the message author.
    /// </summary>
    [JsonPropertyName("role")]
    public LlmMessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the text content when content is a simple string.
    /// </summary>
    /// <remarks>
    /// This is the common case for text-only messages.
    /// For multimodal content, use <see cref="ContentParts"/> instead.
    /// </remarks>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the content parts for multimodal messages.
    /// </summary>
    /// <remarks>
    /// Use this for messages containing multiple content types (text + images).
    /// For simple text messages, use <see cref="Content"/> instead.
    /// </remarks>
    [JsonPropertyName("content_parts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<LlmContentPart>? ContentParts { get; set; }

    /// <summary>
    /// Gets or sets an optional name for the participant.
    /// </summary>
    /// <remarks>
    /// May contain a-z, A-Z, 0-9, and underscores, with a maximum length of 64 characters.
    /// </remarks>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tool calls made by the assistant.
    /// </summary>
    /// <remarks>
    /// Only present for assistant messages that include tool calls.
    /// </remarks>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<LlmToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tool call this message is responding to.
    /// </summary>
    /// <remarks>
    /// Required for tool role messages to identify which tool call this responds to.
    /// </remarks>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Creates a system message.
    /// </summary>
    /// <param name="content">The system message content.</param>
    /// <param name="name">Optional participant name.</param>
    /// <returns>A new system message.</returns>
    public static LlmMessage System(string content, string? name = null) =>
        new() { Role = LlmMessageRole.System, Content = content, Name = name };

    /// <summary>
    /// Creates a user message with text content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="name">Optional participant name.</param>
    /// <returns>A new user message.</returns>
    public static LlmMessage User(string content, string? name = null) =>
        new() { Role = LlmMessageRole.User, Content = content, Name = name };

    /// <summary>
    /// Creates a user message with multimodal content parts.
    /// </summary>
    /// <param name="parts">The content parts.</param>
    /// <param name="name">Optional participant name.</param>
    /// <returns>A new user message with content parts.</returns>
    public static LlmMessage User(IList<LlmContentPart> parts, string? name = null) =>
        new() { Role = LlmMessageRole.User, ContentParts = parts, Name = name };

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="toolCalls">Optional tool calls made by the assistant.</param>
    /// <param name="name">Optional participant name.</param>
    /// <returns>A new assistant message.</returns>
    public static LlmMessage Assistant(string? content, IList<LlmToolCall>? toolCalls = null, string? name = null) =>
        new() { Role = LlmMessageRole.Assistant, Content = content, ToolCalls = toolCalls, Name = name };

    /// <summary>
    /// Creates a tool response message.
    /// </summary>
    /// <param name="toolCallId">The ID of the tool call being responded to.</param>
    /// <param name="content">The tool response content.</param>
    /// <param name="name">Optional participant name (usually the function name).</param>
    /// <returns>A new tool message.</returns>
    public static LlmMessage Tool(string toolCallId, string content, string? name = null) =>
        new() { Role = LlmMessageRole.Tool, ToolCallId = toolCallId, Content = content, Name = name };
}
