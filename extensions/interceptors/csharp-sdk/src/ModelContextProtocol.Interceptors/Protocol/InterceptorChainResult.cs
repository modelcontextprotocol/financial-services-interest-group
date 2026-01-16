using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents the result of executing an interceptor chain.
/// </summary>
/// <remarks>
/// <para>
/// The chain result aggregates results from all executed interceptors and provides
/// the final payload after all mutations, along with a validation summary.
/// </para>
/// <para>
/// See SEP-1763 for the full specification of chain execution semantics.
/// </para>
/// </remarks>
public sealed class InterceptorChainResult
{
    /// <summary>
    /// Gets or sets the overall chain execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public InterceptorChainStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the event type that was processed.
    /// </summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>
    /// Gets or sets the phase of execution.
    /// </summary>
    [JsonPropertyName("phase")]
    public InterceptorPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the results from all executed interceptors.
    /// </summary>
    [JsonPropertyName("results")]
    public IList<InterceptorResult> Results { get; set; } = [];

    /// <summary>
    /// Gets or sets the final payload after all mutations (if chain completed).
    /// </summary>
    [JsonPropertyName("finalPayload")]
    public JsonNode? FinalPayload { get; set; }

    /// <summary>
    /// Gets or sets the validation summary.
    /// </summary>
    [JsonPropertyName("validationSummary")]
    public ValidationSummary ValidationSummary { get; set; } = new();

    /// <summary>
    /// Gets or sets the total execution time in milliseconds.
    /// </summary>
    [JsonPropertyName("totalDurationMs")]
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets details about where the chain was aborted, if applicable.
    /// </summary>
    [JsonPropertyName("abortedAt")]
    public ChainAbortInfo? AbortedAt { get; set; }
}

/// <summary>
/// Represents the status of an interceptor chain execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InterceptorChainStatus>))]
public enum InterceptorChainStatus
{
    /// <summary>
    /// The chain completed successfully.
    /// </summary>
    [JsonStringEnumMemberName("success")]
    Success,

    /// <summary>
    /// The chain was aborted due to a validation failure.
    /// </summary>
    [JsonStringEnumMemberName("validation_failed")]
    ValidationFailed,

    /// <summary>
    /// The chain was aborted due to a mutation failure.
    /// </summary>
    [JsonStringEnumMemberName("mutation_failed")]
    MutationFailed,

    /// <summary>
    /// The chain was aborted due to a timeout.
    /// </summary>
    [JsonStringEnumMemberName("timeout")]
    Timeout
}

/// <summary>
/// Represents a summary of validation results from an interceptor chain.
/// </summary>
public sealed class ValidationSummary
{
    /// <summary>
    /// Gets or sets the number of error-level validations.
    /// </summary>
    [JsonPropertyName("errors")]
    public int Errors { get; set; }

    /// <summary>
    /// Gets or sets the number of warning-level validations.
    /// </summary>
    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }

    /// <summary>
    /// Gets or sets the number of info-level validations.
    /// </summary>
    [JsonPropertyName("infos")]
    public int Infos { get; set; }
}

/// <summary>
/// Represents information about where an interceptor chain was aborted.
/// </summary>
public sealed class ChainAbortInfo
{
    /// <summary>
    /// Gets or sets the name of the interceptor that caused the abort.
    /// </summary>
    [JsonPropertyName("interceptor")]
    public required string Interceptor { get; set; }

    /// <summary>
    /// Gets or sets the reason for the abort.
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the type of abort.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
