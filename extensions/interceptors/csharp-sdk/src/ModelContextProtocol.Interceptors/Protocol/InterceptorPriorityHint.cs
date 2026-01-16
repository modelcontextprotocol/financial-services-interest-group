using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Represents a priority hint for mutation interceptor ordering.
/// </summary>
/// <remarks>
/// <para>
/// Priority hints determine the execution order for mutation interceptors.
/// Lower values execute first. Interceptors with equal priority are ordered alphabetically by name.
/// </para>
/// <para>
/// Can be specified as a single number (applies to both phases) or with different priorities per phase.
/// Default priority is 0 if not specified.
/// </para>
/// </remarks>
[JsonConverter(typeof(InterceptorPriorityHintConverter))]
public readonly struct InterceptorPriorityHint : IEquatable<InterceptorPriorityHint>
{
    /// <summary>
    /// Gets the priority for the request phase.
    /// </summary>
    [JsonPropertyName("request")]
    public int? Request { get; init; }

    /// <summary>
    /// Gets the priority for the response phase.
    /// </summary>
    [JsonPropertyName("response")]
    public int? Response { get; init; }

    /// <summary>
    /// Initializes a new instance with the same priority for both phases.
    /// </summary>
    /// <param name="priority">The priority value for both request and response phases.</param>
    public InterceptorPriorityHint(int priority)
    {
        Request = priority;
        Response = priority;
    }

    /// <summary>
    /// Initializes a new instance with different priorities per phase.
    /// </summary>
    /// <param name="request">The priority for the request phase, or null to use default (0).</param>
    /// <param name="response">The priority for the response phase, or null to use default (0).</param>
    public InterceptorPriorityHint(int? request, int? response)
    {
        Request = request;
        Response = response;
    }

    /// <summary>
    /// Gets the resolved priority for the specified phase.
    /// </summary>
    /// <param name="phase">The phase to get the priority for.</param>
    /// <returns>The priority value, defaulting to 0 if not specified.</returns>
    public int GetPriorityForPhase(InterceptorPhase phase) => phase switch
    {
        InterceptorPhase.Request => Request ?? 0,
        InterceptorPhase.Response => Response ?? 0,
        InterceptorPhase.Both => Request ?? Response ?? 0,
        _ => 0
    };

    /// <summary>
    /// Implicitly converts an integer to an <see cref="InterceptorPriorityHint"/> with the same priority for both phases.
    /// </summary>
    public static implicit operator InterceptorPriorityHint(int priority) => new(priority);

    /// <inheritdoc />
    public bool Equals(InterceptorPriorityHint other) => Request == other.Request && Response == other.Response;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is InterceptorPriorityHint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Request ?? 0) * 397) ^ (Response ?? 0);
        }
    }

    /// <summary>
    /// Determines whether two <see cref="InterceptorPriorityHint"/> instances are equal.
    /// </summary>
    public static bool operator ==(InterceptorPriorityHint left, InterceptorPriorityHint right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="InterceptorPriorityHint"/> instances are not equal.
    /// </summary>
    public static bool operator !=(InterceptorPriorityHint left, InterceptorPriorityHint right) => !left.Equals(right);
}

/// <summary>
/// JSON converter for <see cref="InterceptorPriorityHint"/> that supports both number and object formats.
/// </summary>
public sealed class InterceptorPriorityHintConverter : JsonConverter<InterceptorPriorityHint>
{
    /// <inheritdoc />
    public override InterceptorPriorityHint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new InterceptorPriorityHint(reader.GetInt32());
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int? request = null;
            int? response = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string? propertyName = reader.GetString();
                reader.Read();

                if (string.Equals(propertyName, "request", StringComparison.OrdinalIgnoreCase) && reader.TokenType == JsonTokenType.Number)
                {
                    request = reader.GetInt32();
                }
                else if (string.Equals(propertyName, "response", StringComparison.OrdinalIgnoreCase) && reader.TokenType == JsonTokenType.Number)
                {
                    response = reader.GetInt32();
                }
            }

            return new InterceptorPriorityHint(request, response);
        }

        throw new JsonException($"Expected number or object for InterceptorPriorityHint, got {reader.TokenType}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, InterceptorPriorityHint value, JsonSerializerOptions options)
    {
        // If both values are the same, write as a single number
        if (value.Request == value.Response && value.Request.HasValue)
        {
            writer.WriteNumberValue(value.Request.Value);
            return;
        }

        // Otherwise write as an object
        writer.WriteStartObject();

        if (value.Request.HasValue)
        {
            writer.WriteNumber("request", value.Request.Value);
        }

        if (value.Response.HasValue)
        {
            writer.WriteNumber("response", value.Response.Value);
        }

        writer.WriteEndObject();
    }
}
