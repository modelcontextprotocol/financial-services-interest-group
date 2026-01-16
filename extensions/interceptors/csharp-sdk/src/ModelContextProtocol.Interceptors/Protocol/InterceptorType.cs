using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Specifies the type of operation an interceptor performs.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InterceptorType>))]
public enum InterceptorType
{
    /// <summary>
    /// Validates messages and can block execution if validation fails with error severity.
    /// </summary>
    [JsonStringEnumMemberName("validation")]
    Validation,

    /// <summary>
    /// Transforms or modifies message payloads. Mutations are executed sequentially by priority.
    /// </summary>
    [JsonStringEnumMemberName("mutation")]
    Mutation,

    /// <summary>
    /// Observes messages for logging, metrics, or auditing. Fire-and-forget, never blocks execution.
    /// </summary>
    [JsonStringEnumMemberName("observability")]
    Observability
}
