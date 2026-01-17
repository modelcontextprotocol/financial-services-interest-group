using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.IO.Pipelines;
using System.Text.Json.Nodes;

// =============================================================================
// MCP Interceptors Client Sample
// =============================================================================
// This sample demonstrates how to use the InterceptingMcpClient to automatically
// execute interceptor chains for MCP tool operations.
//
// The sample:
// 1. Creates an in-memory MCP server with sample tools
// 2. Creates an MCP client with interceptors using InterceptingMcpClient
// 3. Demonstrates validation, mutation, and observability interceptors
// 4. Shows error handling with McpInterceptorValidationException
// =============================================================================

Console.WriteLine("=== MCP Interceptors Client Sample ===\n");

// Set up in-memory transport using pipes
Pipe clientToServerPipe = new(), serverToClientPipe = new();

// Create server with sample tools
await using McpServer server = McpServer.Create(
    new StreamServerTransport(clientToServerPipe.Reader.AsStream(), serverToClientPipe.Writer.AsStream()),
    new McpServerOptions
    {
        ServerInfo = new() { Name = "SampleServer", Version = "1.0.0" },
        ToolCollection =
        [
            // Echo tool - returns what you send
            McpServerTool.Create(
                (string message) => $"Echo: {message}",
                new() { Name = "echo", Description = "Echoes the message back" }),

            // Greet tool - creates a greeting
            McpServerTool.Create(
                (string name, string? title = null) =>
                    title is not null ? $"Hello, {title} {name}!" : $"Hello, {name}!",
                new() { Name = "greet", Description = "Creates a greeting" }),

            // Search tool - simulates a search operation
            McpServerTool.Create(
                (string query) => $"Search results for: {query}\n- Result 1\n- Result 2\n- Result 3",
                new() { Name = "search", Description = "Searches for content" }),

            // Sensitive tool - returns data that should be redacted
            McpServerTool.Create(
                () => "API Response: api_key=sk_live_abc123 and token=Bearer xyz789",
                new() { Name = "get_config", Description = "Gets configuration (contains sensitive data)" }),
        ]
    });

// Start server in background
_ = server.RunAsync();

// Create the base MCP client
await using McpClient client = await McpClient.CreateAsync(
    new StreamClientTransport(clientToServerPipe.Writer.AsStream(), serverToClientPipe.Reader.AsStream()));

Console.WriteLine($"Connected to server: {client.ServerInfo.Name} v{client.ServerInfo.Version}\n");

// =============================================================================
// Create interceptors using both attribute-based classes and inline delegates
// =============================================================================

// Collect interceptors from attributed classes
var attributeBasedInterceptors = new List<McpClientInterceptor>();
attributeBasedInterceptors.AddRange(McpClientInterceptorExtensions.WithInterceptors<ClientValidationInterceptors>());
attributeBasedInterceptors.AddRange(McpClientInterceptorExtensions.WithInterceptors<ClientMutationInterceptors>());
attributeBasedInterceptors.AddRange(McpClientInterceptorExtensions.WithInterceptors<ClientObservabilityInterceptors>());

Console.WriteLine($"Loaded {attributeBasedInterceptors.Count} attribute-based interceptors:");
foreach (var interceptor in attributeBasedInterceptors)
{
    var proto = interceptor.ProtocolInterceptor;
    var priority = proto.PriorityHint?.Request ?? proto.PriorityHint?.Response ?? 0;
    Console.WriteLine($"  - {proto.Name} ({proto.Type}, priority: {priority})");
}
Console.WriteLine();

// =============================================================================
// Wrap the client with interceptors
// =============================================================================

var interceptedClient = client.WithInterceptors(new InterceptingMcpClientOptions
{
    Interceptors = attributeBasedInterceptors,
    DefaultTimeoutMs = 10000,
    ThrowOnValidationError = true,
    InterceptResponses = true
});

// =============================================================================
// Demo 1: List tools (shows observability interceptor metrics)
// =============================================================================

Console.WriteLine("--- Demo 1: List Tools ---");
var tools = await interceptedClient.ListToolsAsync();
Console.WriteLine($"Available tools: {string.Join(", ", tools.Select(t => t.Name))}\n");

// =============================================================================
// Demo 2: Normal tool call (shows request/response logging)
// =============================================================================

Console.WriteLine("--- Demo 2: Normal Tool Call ---");
var echoResult = await interceptedClient.CallToolAsync("echo", new Dictionary<string, object?>
{
    ["message"] = "Hello from intercepted client!"
});
PrintResult(echoResult);

// =============================================================================
// Demo 3: Tool call with argument normalization (mutation)
// =============================================================================

Console.WriteLine("--- Demo 3: Argument Normalization (Mutation) ---");
var greetResult = await interceptedClient.CallToolAsync("greet", new Dictionary<string, object?>
{
    ["name"] = "  John Doe  ", // Will be trimmed
    ["title"] = "   " // Will be converted to null (empty after trim)
});
PrintResult(greetResult);

// =============================================================================
// Demo 4: Response redaction (mutation on response)
// =============================================================================

Console.WriteLine("--- Demo 4: Response Redaction ---");
var configResult = await interceptedClient.CallToolAsync("get_config", new Dictionary<string, object?>());
PrintResult(configResult);

// =============================================================================
// Demo 5: PII Detection - Email Warning (validation with warning)
// =============================================================================

Console.WriteLine("--- Demo 5: PII Warning (Email) ---");
var searchWithEmail = await interceptedClient.CallToolAsync("search", new Dictionary<string, object?>
{
    ["query"] = "contact john@example.com for details"
});
PrintResult(searchWithEmail);

// =============================================================================
// Demo 6: PII Detection - SSN Error (validation blocks request)
// =============================================================================

Console.WriteLine("--- Demo 6: PII Error (SSN - Blocked) ---");
try
{
    var searchWithSsn = await interceptedClient.CallToolAsync("search", new Dictionary<string, object?>
    {
        ["query"] = "user SSN is 123-45-6789"
    });
    PrintResult(searchWithSsn);
}
catch (McpInterceptorValidationException ex)
{
    Console.WriteLine($"  REQUEST BLOCKED!");
    Console.WriteLine($"  Blocked by: {ex.AbortedAt?.Interceptor}");
    Console.WriteLine($"  Reason: {ex.AbortedAt?.Reason}");
    Console.WriteLine($"  Chain status: {ex.ChainResult.Status}");
    Console.WriteLine();

    // Show detailed validation messages
    Console.WriteLine("  Validation details:");
    Console.WriteLine(ex.GetDetailedMessage());
}

// =============================================================================
// Demo 7: Using non-throwing mode
// =============================================================================

Console.WriteLine("--- Demo 7: Non-Throwing Mode ---");
var nonThrowingClient = client.WithInterceptors(new InterceptingMcpClientOptions
{
    Interceptors = attributeBasedInterceptors,
    ThrowOnValidationError = false // Don't throw, return error result instead
});

var blockedResult = await nonThrowingClient.CallToolAsync("search", new Dictionary<string, object?>
{
    ["query"] = "credit card 4111-1111-1111-1111"
});
Console.WriteLine($"  IsError: {blockedResult.IsError}");
if (blockedResult.Content?.FirstOrDefault() is TextContentBlock text)
{
    Console.WriteLine($"  Message: {text.Text}");
}
Console.WriteLine();

// =============================================================================
// Demo 8: Accessing inner client for non-intercepted operations
// =============================================================================

Console.WriteLine("--- Demo 8: Accessing Inner Client ---");
Console.WriteLine($"  Server capabilities via inner client:");
Console.WriteLine($"    Tools: {interceptedClient.ServerCapabilities.Tools is not null}");
Console.WriteLine($"  Server info: {interceptedClient.ServerInfo.Name} v{interceptedClient.ServerInfo.Version}");
Console.WriteLine($"  Inner client type: {interceptedClient.Inner.GetType().Name}");
Console.WriteLine();

Console.WriteLine("=== Sample Complete ===");

// Helper to print results
static void PrintResult(CallToolResult result)
{
    Console.WriteLine($"  IsError: {result.IsError}");
    if (result.Content is not null)
    {
        foreach (var content in result.Content)
        {
            if (content is TextContentBlock textBlock)
            {
                Console.WriteLine($"  Content: {textBlock.Text}");
            }
        }
    }
    Console.WriteLine();
}
