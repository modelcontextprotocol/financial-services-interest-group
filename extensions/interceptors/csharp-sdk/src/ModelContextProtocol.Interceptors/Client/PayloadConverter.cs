using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol.Interceptors.Protocol.Llm;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Provides conversion utilities between typed MCP request/response objects and JsonNode for interceptor chains.
/// </summary>
/// <remarks>
/// This internal helper class enables the <see cref="InterceptingMcpClient"/> to convert typed MCP protocol
/// objects to <see cref="JsonNode"/> for interceptor processing and back to typed objects after mutations.
/// </remarks>
internal static class PayloadConverter
{
    private static JsonSerializerOptions DefaultOptions => McpJsonUtilities.DefaultOptions;

    #region Generic Conversion

    /// <summary>
    /// Converts a typed value to a <see cref="JsonNode"/>.
    /// </summary>
    /// <typeparam name="T">The type of value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">Optional serializer options. Defaults to MCP options if not provided.</param>
    /// <returns>The JSON representation of the value, or null if the value is null.</returns>
    public static JsonNode? ToJsonNode<T>(T? value, JsonSerializerOptions? options = null)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(value, options ?? DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a typed value.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="node">The JSON node to convert.</param>
    /// <param name="options">Optional serializer options. Defaults to MCP options if not provided.</param>
    /// <returns>The deserialized value, or default if the node is null.</returns>
    public static T? FromJsonNode<T>(JsonNode? node, JsonSerializerOptions? options = null)
    {
        if (node is null)
        {
            return default;
        }

        return node.Deserialize<T>(options ?? DefaultOptions);
    }

    /// <summary>
    /// Converts a typed value to a <see cref="JsonNode"/> using a specific type info.
    /// </summary>
    /// <typeparam name="T">The type of value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="typeInfo">The JSON type info for serialization.</param>
    /// <returns>The JSON representation of the value, or null if the value is null.</returns>
    public static JsonNode? ToJsonNode<T>(T? value, JsonTypeInfo<T> typeInfo)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(value, typeInfo);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a typed value using a specific type info.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="node">The JSON node to convert.</param>
    /// <param name="typeInfo">The JSON type info for deserialization.</param>
    /// <returns>The deserialized value, or default if the node is null.</returns>
    public static T? FromJsonNode<T>(JsonNode? node, JsonTypeInfo<T> typeInfo)
    {
        if (node is null)
        {
            return default;
        }

        return node.Deserialize(typeInfo);
    }

    #endregion

    #region CallTool Request Conversion

    /// <summary>
    /// Creates a <see cref="JsonNode"/> payload for a tool call request.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">Optional arguments dictionary.</param>
    /// <returns>A JSON object representing the tool call request.</returns>
    public static JsonNode ToCallToolRequestPayload(string toolName, IReadOnlyDictionary<string, object?>? arguments)
    {
        var obj = new JsonObject
        {
            ["name"] = toolName
        };

        if (arguments is not null && arguments.Count > 0)
        {
            var argsObj = new JsonObject();
            foreach (var kvp in arguments)
            {
                argsObj[kvp.Key] = kvp.Value switch
                {
                    null => null,
                    JsonNode jn => jn.DeepClone(),
                    JsonElement je => JsonNode.Parse(je.GetRawText()),
                    _ => JsonSerializer.SerializeToNode(kvp.Value, DefaultOptions)
                };
            }
            obj["arguments"] = argsObj;
        }

        return obj;
    }

    /// <summary>
    /// Extracts tool name and arguments from a tool call request payload.
    /// </summary>
    /// <param name="node">The JSON payload representing a tool call request.</param>
    /// <returns>A tuple containing the tool name and arguments dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the payload is invalid or missing required fields.</exception>
    public static (string ToolName, Dictionary<string, JsonElement>? Arguments) FromCallToolRequestPayload(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            throw new InvalidOperationException("CallTool request payload must be a JSON object.");
        }

        var name = obj["name"]?.GetValue<string>()
            ?? throw new InvalidOperationException("CallTool request payload must have a 'name' property.");

        Dictionary<string, JsonElement>? arguments = null;
        if (obj["arguments"] is JsonObject argsObj)
        {
            arguments = new Dictionary<string, JsonElement>();
            foreach (var kvp in argsObj)
            {
                // Handle null values by creating a proper JsonElement representing null
                // Using default(JsonElement) creates an undefined element that fails serialization
                arguments[kvp.Key] = kvp.Value is not null
                    ? JsonSerializer.Deserialize<JsonElement>(kvp.Value.ToJsonString())
                    : JsonSerializer.Deserialize<JsonElement>("null");
            }
        }

        return (name, arguments);
    }

    /// <summary>
    /// Converts a <see cref="CallToolRequestParams"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="requestParams">The request parameters.</param>
    /// <returns>A JSON representation of the request.</returns>
    public static JsonNode? ToCallToolRequestParamsPayload(CallToolRequestParams? requestParams)
    {
        if (requestParams is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(requestParams, DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to <see cref="CallToolRequestParams"/>.
    /// </summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The deserialized request parameters.</returns>
    public static CallToolRequestParams? FromCallToolRequestParamsPayload(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Deserialize<CallToolRequestParams>(DefaultOptions);
    }

    #endregion

    #region CallTool Result Conversion

    /// <summary>
    /// Converts a <see cref="CallToolResult"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="result">The tool call result.</param>
    /// <returns>A JSON representation of the result.</returns>
    public static JsonNode? ToCallToolResultPayload(CallToolResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(result, DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a <see cref="CallToolResult"/>.
    /// </summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The deserialized result.</returns>
    public static CallToolResult? FromCallToolResultPayload(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Deserialize<CallToolResult>(DefaultOptions);
    }

    #endregion

    #region ListTools Conversion

    /// <summary>
    /// Creates a <see cref="JsonNode"/> payload for a list tools request.
    /// </summary>
    /// <param name="cursor">Optional pagination cursor.</param>
    /// <returns>A JSON object representing the list tools request.</returns>
    public static JsonNode? ToListToolsRequestPayload(string? cursor)
    {
        if (cursor is null)
        {
            return new JsonObject();
        }

        return new JsonObject
        {
            ["cursor"] = cursor
        };
    }

    /// <summary>
    /// Extracts the cursor from a list tools request payload.
    /// </summary>
    /// <param name="node">The JSON payload.</param>
    /// <returns>The cursor value, or null if not present.</returns>
    public static string? FromListToolsRequestPayload(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return null;
        }

        return obj["cursor"]?.GetValue<string>();
    }

    /// <summary>
    /// Converts a <see cref="ListToolsResult"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="result">The list tools result.</param>
    /// <returns>A JSON representation of the result.</returns>
    public static JsonNode? ToListToolsResultPayload(ListToolsResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(result, DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a <see cref="ListToolsResult"/>.
    /// </summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The deserialized result.</returns>
    public static ListToolsResult? FromListToolsResultPayload(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Deserialize<ListToolsResult>(DefaultOptions);
    }

    #endregion

    #region LLM Completion Conversion

    /// <summary>
    /// Converts an <see cref="LlmCompletionRequest"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="request">The LLM completion request.</param>
    /// <returns>A JSON representation of the request.</returns>
    public static JsonNode? ToLlmCompletionRequestPayload(LlmCompletionRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(request, DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="LlmCompletionRequest"/>.
    /// </summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The deserialized request.</returns>
    public static LlmCompletionRequest? FromLlmCompletionRequestPayload(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Deserialize<LlmCompletionRequest>(DefaultOptions);
    }

    /// <summary>
    /// Converts an <see cref="LlmCompletionResponse"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="response">The LLM completion response.</param>
    /// <returns>A JSON representation of the response.</returns>
    public static JsonNode? ToLlmCompletionResponsePayload(LlmCompletionResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(response, DefaultOptions);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="LlmCompletionResponse"/>.
    /// </summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The deserialized response.</returns>
    public static LlmCompletionResponse? FromLlmCompletionResponsePayload(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Deserialize<LlmCompletionResponse>(DefaultOptions);
    }

    #endregion
}
