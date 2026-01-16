using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Specifies the severity level for validation messages.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ValidationSeverity>))]
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message that does not block execution.
    /// </summary>
    [JsonStringEnumMemberName("info")]
    Info,

    /// <summary>
    /// Warning message that does not block execution but indicates potential issues.
    /// </summary>
    [JsonStringEnumMemberName("warn")]
    Warn,

    /// <summary>
    /// Error message that blocks execution.
    /// </summary>
    [JsonStringEnumMemberName("error")]
    Error
}
