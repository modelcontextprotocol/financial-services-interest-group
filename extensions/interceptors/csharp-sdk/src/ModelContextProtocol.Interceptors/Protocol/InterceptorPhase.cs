using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Specifies the execution phase for an interceptor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InterceptorPhase>))]
public enum InterceptorPhase
{
    /// <summary>
    /// Interceptor executes on incoming requests before processing.
    /// </summary>
    [JsonStringEnumMemberName("request")]
    Request,

    /// <summary>
    /// Interceptor executes on outgoing responses after processing.
    /// </summary>
    [JsonStringEnumMemberName("response")]
    Response,

    /// <summary>
    /// Interceptor executes on both requests and responses.
    /// </summary>
    [JsonStringEnumMemberName("both")]
    Both
}
