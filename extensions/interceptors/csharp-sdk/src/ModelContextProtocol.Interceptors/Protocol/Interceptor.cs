using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Interceptors.Server;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents an interceptor that can validate, mutate, or observe MCP messages.
/// </summary>
/// <remarks>
/// <para>
/// Interceptors are a mechanism for hooking into MCP events to provide cross-cutting
/// functionality such as validation, transformation, logging, and security enforcement.
/// </para>
/// <para>
/// See SEP-1763 for the full specification.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class Interceptor
{
    /// <summary>
    /// Gets or sets the unique identifier for this interceptor.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic version of this interceptor.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of what this interceptor does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the events this interceptor subscribes to.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="InterceptorEvents"/> for event names.
    /// </remarks>
    [JsonPropertyName("events")]
    public IList<string> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the type of operation this interceptor performs.
    /// </summary>
    [JsonPropertyName("type")]
    public InterceptorType Type { get; set; }

    /// <summary>
    /// Gets or sets the execution phase for this interceptor.
    /// </summary>
    [JsonPropertyName("phase")]
    public InterceptorPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the priority hint for mutation interceptor ordering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower values execute first. Default is 0 if not specified.
    /// Interceptors with equal priority are ordered alphabetically by name.
    /// </para>
    /// <para>
    /// This field is only meaningful for mutation interceptors.
    /// For validation and observability interceptors, it is ignored.
    /// </para>
    /// </remarks>
    [JsonPropertyName("priorityHint")]
    public InterceptorPriorityHint? PriorityHint { get; set; }

    /// <summary>
    /// Gets or sets the protocol version compatibility for this interceptor.
    /// </summary>
    [JsonPropertyName("compat")]
    public InterceptorCompatibility? Compat { get; set; }

    /// <summary>
    /// Gets or sets the JSON Schema for interceptor configuration.
    /// </summary>
    /// <remarks>
    /// Documents the expected configuration format for this interceptor.
    /// </remarks>
    [JsonPropertyName("configSchema")]
    public JsonElement? ConfigSchema { get; set; }

    /// <summary>
    /// Gets or sets metadata reserved by MCP for protocol-level metadata.
    /// </summary>
    /// <remarks>
    /// Implementations must not make assumptions about its contents.
    /// </remarks>
    [JsonPropertyName("_meta")]
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Gets or sets the callable server interceptor corresponding to this metadata, if any.
    /// </summary>
    [JsonIgnore]
    public McpServerInterceptor? McpServerInterceptor { get; set; }

    /// <summary>
    /// Gets or sets the callable client interceptor corresponding to this metadata, if any.
    /// </summary>
    [JsonIgnore]
    public McpClientInterceptor? McpClientInterceptor { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string desc = Description is not null ? $", Description = \"{Description}\"" : "";
            string type = $", Type = {Type}";
            return $"Name = {Name}{type}{desc}";
        }
    }
}
