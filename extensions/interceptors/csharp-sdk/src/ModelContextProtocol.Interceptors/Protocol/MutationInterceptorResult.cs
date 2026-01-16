using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the result of invoking a mutation interceptor.
/// </summary>
/// <remarks>
/// <para>
/// Mutation interceptors transform or modify message payloads. They are executed sequentially
/// by priority, with lower priority values executing first.
/// </para>
/// <para>
/// When <see cref="Modified"/> is true, the <see cref="Payload"/> contains the transformed content
/// that should replace the original payload in the message pipeline.
/// </para>
/// </remarks>
public sealed class MutationInterceptorResult : InterceptorResult
{
    /// <summary>
    /// Gets the type of interceptor (always "mutation" for this result type).
    /// </summary>
    [JsonPropertyName("type")]
    public override InterceptorType Type => InterceptorType.Mutation;

    /// <summary>
    /// Gets or sets whether the payload was modified.
    /// </summary>
    [JsonPropertyName("modified")]
    public bool Modified { get; set; }

    /// <summary>
    /// Gets or sets the mutated payload (or original if not modified).
    /// </summary>
    [JsonPropertyName("payload")]
    public JsonNode? Payload { get; set; }

    /// <summary>
    /// Creates a mutation result indicating no modification was made.
    /// </summary>
    /// <param name="originalPayload">The original payload to pass through unchanged.</param>
    /// <returns>A mutation result with <see cref="Modified"/> set to false.</returns>
    public static MutationInterceptorResult Unchanged(JsonNode? originalPayload) => new()
    {
        Modified = false,
        Payload = originalPayload
    };

    /// <summary>
    /// Creates a mutation result indicating the payload was modified.
    /// </summary>
    /// <param name="mutatedPayload">The new, transformed payload.</param>
    /// <returns>A mutation result with <see cref="Modified"/> set to true.</returns>
    public static MutationInterceptorResult Mutated(JsonNode? mutatedPayload) => new()
    {
        Modified = true,
        Payload = mutatedPayload
    };
}
