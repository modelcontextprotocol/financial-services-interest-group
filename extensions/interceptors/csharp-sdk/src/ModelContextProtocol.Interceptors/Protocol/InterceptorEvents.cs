namespace ModelContextProtocol.Interceptors;

/// <summary>
/// Provides constants with the names of events that interceptors can subscribe to.
/// </summary>
public static class InterceptorEvents
{
    // MCP Server Features - Tools

    /// <summary>
    /// Event fired when listing available tools.
    /// </summary>
    public const string ToolsList = "tools/list";

    /// <summary>
    /// Event fired when invoking a tool.
    /// </summary>
    public const string ToolsCall = "tools/call";

    // MCP Server Features - Prompts

    /// <summary>
    /// Event fired when listing available prompts.
    /// </summary>
    public const string PromptsList = "prompts/list";

    /// <summary>
    /// Event fired when retrieving a prompt.
    /// </summary>
    public const string PromptsGet = "prompts/get";

    // MCP Server Features - Resources

    /// <summary>
    /// Event fired when listing available resources.
    /// </summary>
    public const string ResourcesList = "resources/list";

    /// <summary>
    /// Event fired when reading a resource.
    /// </summary>
    public const string ResourcesRead = "resources/read";

    /// <summary>
    /// Event fired when subscribing to resource updates.
    /// </summary>
    public const string ResourcesSubscribe = "resources/subscribe";

    // MCP Client Features

    /// <summary>
    /// Event fired when a server requests sampling (LLM inference) from the client.
    /// </summary>
    public const string SamplingCreateMessage = "sampling/createMessage";

    /// <summary>
    /// Event fired when a server requests elicitation (user input) from the client.
    /// </summary>
    public const string ElicitationCreate = "elicitation/create";

    /// <summary>
    /// Event fired when listing client roots (filesystem access).
    /// </summary>
    public const string RootsList = "roots/list";

    // LLM Interaction Events

    /// <summary>
    /// Event fired for LLM completion requests (using common OpenAI-like format).
    /// </summary>
    public const string LlmCompletion = "llm/completion";

    // Wildcard Events

    /// <summary>
    /// Wildcard event that matches all request-phase events.
    /// </summary>
    public const string AllRequests = "*/request";

    /// <summary>
    /// Wildcard event that matches all response-phase events.
    /// </summary>
    public const string AllResponses = "*/response";

    /// <summary>
    /// Wildcard event that matches all events.
    /// </summary>
    public const string All = "*";
}
