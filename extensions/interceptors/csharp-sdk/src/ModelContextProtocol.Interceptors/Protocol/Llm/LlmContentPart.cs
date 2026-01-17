using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors.Protocol.Llm;

/// <summary>
/// Represents a content part within an LLM message for multimodal content.
/// </summary>
/// <remarks>
/// Based on the OpenAI multimodal content parts specification in SEP-1763.
/// Supports text and image content types.
/// </remarks>
public sealed class LlmContentPart
{
    /// <summary>
    /// Gets or sets the type of content part.
    /// </summary>
    /// <remarks>
    /// Valid values are "text" or "image_url".
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Gets or sets the text content when Type is "text".
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the image URL information when Type is "image_url".
    /// </summary>
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LlmImageUrl? ImageUrl { get; set; }

    /// <summary>
    /// Creates a text content part.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>A new text content part.</returns>
    public static LlmContentPart CreateText(string text) => new() { Type = "text", Text = text };

    /// <summary>
    /// Creates an image URL content part.
    /// </summary>
    /// <param name="url">The image URL or base64 data URI.</param>
    /// <param name="detail">Optional detail level ("auto", "low", or "high").</param>
    /// <returns>A new image content part.</returns>
    public static LlmContentPart CreateImage(string url, string? detail = null) =>
        new() { Type = "image_url", ImageUrl = new() { Url = url, Detail = detail } };
}

/// <summary>
/// Represents image URL information for multimodal content.
/// </summary>
public sealed class LlmImageUrl
{
    /// <summary>
    /// Gets or sets the URL of the image or a base64-encoded data URI.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detail level for the image.
    /// </summary>
    /// <remarks>
    /// Valid values are "auto", "low", or "high".
    /// </remarks>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }
}
