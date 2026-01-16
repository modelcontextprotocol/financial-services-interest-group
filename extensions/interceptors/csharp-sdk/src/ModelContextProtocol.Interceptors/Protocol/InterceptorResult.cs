using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Base class for all interceptor results.
/// </summary>
/// <remarks>
/// <para>
/// All interceptor invocations return results conforming to this unified envelope structure.
/// Derived types include <see cref="ValidationInterceptorResult"/>, <see cref="MutationInterceptorResult"/>,
/// and <see cref="ObservabilityInterceptorResult"/>.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ValidationInterceptorResult), "validation")]
[JsonDerivedType(typeof(MutationInterceptorResult), "mutation")]
[JsonDerivedType(typeof(ObservabilityInterceptorResult), "observability")]
public abstract class InterceptorResult
{
    /// <summary>
    /// Gets or sets the name of the interceptor that produced this result.
    /// </summary>
    [JsonPropertyName("interceptor")]
    public string? Interceptor { get; set; }

    /// <summary>
    /// Gets or sets the type of interceptor.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract InterceptorType Type { get; }

    /// <summary>
    /// Gets or sets the phase when this interceptor executed.
    /// </summary>
    [JsonPropertyName("phase")]
    public InterceptorPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets additional interceptor-specific information.
    /// </summary>
    [JsonPropertyName("info")]
    public JsonObject? Info { get; set; }
}
