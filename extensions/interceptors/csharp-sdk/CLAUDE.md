# CLAUDE.md - C# SDK for MCP Interceptors

This document provides guidance for Claude (and other AI assistants) working on the MCP Interceptors C# SDK implementation.

> **Note:** See [TODO.md](./TODO.md) for a list of code style alignment tasks with the MCP C# SDK.

## Project Overview

This SDK implements **SEP-1763: Interceptors for Model Context Protocol** - a standardized framework for intercepting, validating, and transforming MCP messages.

**Specification Reference:** https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1763

## Project Structure

```
csharp-sdk/
├── src/ModelContextProtocol.Interceptors/
│   ├── Protocol/           # Protocol types (Interceptor, Events, Results, etc.)
│   │   ├── InterceptorResult.cs       # Abstract base class for all results
│   │   ├── ValidationInterceptorResult.cs
│   │   ├── MutationInterceptorResult.cs
│   │   ├── ObservabilityInterceptorResult.cs
│   │   ├── InterceptorChainResult.cs  # Chain execution result
│   │   └── McpInterceptorValidationException.cs  # Validation failure exception
│   ├── Server/             # Server-side implementation
│   │   ├── McpServerInterceptorAttribute.cs
│   │   ├── McpServerInterceptorTypeAttribute.cs
│   │   ├── McpServerInterceptor.cs    # Abstract base class
│   │   ├── ReflectionMcpServerInterceptor.cs
│   │   ├── InterceptorServerHandlers.cs
│   │   └── InterceptorServerFilters.cs
│   ├── Client/             # Client-side implementation
│   │   ├── McpClientInterceptorAttribute.cs
│   │   ├── McpClientInterceptorTypeAttribute.cs
│   │   ├── McpClientInterceptor.cs    # Abstract base class
│   │   ├── ReflectionMcpClientInterceptor.cs
│   │   ├── ClientInterceptorContext.cs
│   │   ├── McpClientInterceptorCreateOptions.cs
│   │   ├── InterceptorClientHandlers.cs
│   │   ├── InterceptorClientFilters.cs
│   │   ├── McpClientInterceptorExtensions.cs
│   │   ├── InterceptorChainExecutor.cs     # Chain execution per SEP-1763
│   │   ├── InterceptingMcpClient.cs        # McpClient wrapper with interceptors
│   │   ├── InterceptingMcpClientOptions.cs # Configuration for InterceptingMcpClient
│   │   ├── InterceptingMcpClientExtensions.cs # Extension methods
│   │   └── PayloadConverter.cs             # JSON conversion utilities
│   └── McpServerInterceptorBuilderExtensions.cs  # DI extensions
├── samples/
│   ├── InterceptorServiceSample/   # Server-side validation interceptor example
│   └── InterceptorClientSample/    # Client-side interceptor integration example
└── ModelContextProtocol.Interceptors.sln
```

## Key Concepts from SEP-1763

### Interceptor Types

1. **Validation** - Validates requests/responses, returns pass/fail with severity levels
2. **Mutation** - Transforms payloads before they continue through the pipeline
3. **Observability** - Fire-and-forget logging/metrics collection, never blocks

### Phases

- `Request` - Intercept incoming requests
- `Response` - Intercept outgoing responses
- `Both` - Intercept in both directions

### Events

Interceptors subscribe to specific MCP events:

- Server Features: `tools/list`, `tools/call`, `prompts/list`, `prompts/get`, `resources/list`, `resources/read`, `resources/subscribe`
- Client Features: `sampling/createMessage`, `elicitation/create`, `roots/list`
- LLM Interactions: `llm/completion`
- Wildcards: `*/request`, `*/response`, `*`

### Execution Order

**Sending data (across trust boundary):**
```
Mutate (sequential by priority) → Validate & Observe (parallel) → Send
```

**Receiving data (from trust boundary):**
```
Receive → Validate & Observe (parallel) → Mutate (sequential by priority)
```

### Priority Ordering

- Mutations execute sequentially by `priorityHint` (lower values first)
- Ties broken alphabetically by interceptor name
- Validations and observability run in parallel (priority ignored)
- Recommended ranges: security (-2B to -1M), sanitization (-999K to -10K), normalization (-9999 to -1), default (0), enrichment (1-9999), observability (10K+)

## Implementation Patterns

### Creating a Server-Side Interceptor

```csharp
[McpServerInterceptorType]
public class MyServerInterceptor
{
    [McpServerInterceptor(
        Name = "my-interceptor",
        Description = "Description of what it does",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request,
        PriorityHint = 0)]
    public ValidationInterceptorResult Validate(JsonNode? payload)
    {
        // Implementation
        return new ValidationInterceptorResult { Valid = true };
    }
}
```

### Creating a Client-Side Interceptor (Attribute-Based)

```csharp
[McpClientInterceptorType]
public class MyClientInterceptors
{
    [McpClientInterceptor(
        Name = "pii-validator",
        Description = "Validates tool arguments for PII leakage",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request,
        PriorityHint = -1000)] // Security interceptors run early
    public ValidationInterceptorResult ValidatePii(JsonNode? payload)
    {
        // Validate payload before sending to server
        if (ContainsSsn(payload))
            return ValidationInterceptorResult.Error("SSN detected in arguments");
        return ValidationInterceptorResult.Success();
    }
    
    [McpClientInterceptor(
        Name = "response-redactor",
        Type = InterceptorType.Mutation,
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Response,
        PriorityHint = 50)]
    public MutationInterceptorResult RedactResponse(JsonNode? payload)
    {
        // Transform response received from server
        var redacted = RedactSensitiveData(payload);
        return MutationInterceptorResult.Mutated(redacted);
    }
    
    [McpClientInterceptor(
        Name = "request-logger",
        Type = InterceptorType.Observability,
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request)]
    public ObservabilityInterceptorResult LogRequest(JsonNode? payload, string @event)
    {
        Console.WriteLine($"Tool call: {@event}");
        return ObservabilityInterceptorResult.Success();
    }
}
```

### Using InterceptingMcpClient (Full Integration)

The `InterceptingMcpClient` wraps `McpClient` and automatically executes interceptor chains for tool operations.

```csharp
// Create MCP client normally
await using var client = await McpClient.CreateAsync(transport);

// Collect interceptors from attributed classes
var interceptors = new List<McpClientInterceptor>();
interceptors.AddRange(McpClientInterceptorExtensions.WithInterceptors<MyClientInterceptors>());

// Wrap with interceptors using extension method
var interceptedClient = client.WithInterceptors(new InterceptingMcpClientOptions
{
    Interceptors = interceptors,
    DefaultTimeoutMs = 5000,
    ThrowOnValidationError = true,  // Throw McpInterceptorValidationException on errors
    InterceptResponses = true       // Also intercept responses
});

// Use intercepted client - interceptors run automatically
try
{
    var result = await interceptedClient.CallToolAsync("my-tool", new Dictionary<string, object?>
    {
        ["name"] = "John Doe"
    });
    Console.WriteLine(result.Content?.FirstOrDefault());
}
catch (McpInterceptorValidationException ex)
{
    Console.WriteLine($"Blocked by {ex.AbortedAt?.Interceptor}: {ex.AbortedAt?.Reason}");
    Console.WriteLine(ex.GetDetailedMessage()); // Detailed validation info
}

// List tools (also intercepted)
var tools = await interceptedClient.ListToolsAsync();

// Access inner client for non-intercepted operations
var serverInfo = interceptedClient.ServerInfo;
```

### Non-Throwing Mode

```csharp
var interceptedClient = client.WithInterceptors(new InterceptingMcpClientOptions
{
    Interceptors = interceptors,
    ThrowOnValidationError = false  // Return error result instead of throwing
});

var result = await interceptedClient.CallToolAsync("my-tool", args);
if (result.IsError)
{
    // Handle validation failure from result
    Console.WriteLine(result.Content?.FirstOrDefault());
}
```

### Registration via DI (Server)

```csharp
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<MyServerInterceptor>();
```

### Client Interceptor Chain Execution (Low-Level)

For advanced scenarios where you need direct chain execution:

```csharp
// Create interceptors from attributed class
var interceptors = McpClientInterceptorExtensions.WithInterceptors<MyClientInterceptor>(services);

// Execute chain for outgoing requests
var executor = new InterceptorChainExecutor(interceptors, services);
var result = await executor.ExecuteForSendingAsync(
    @event: InterceptorEvents.ToolsCall,
    payload: requestPayload,
    config: null,
    timeoutMs: 5000);

if (result.Status == InterceptorChainStatus.Success)
{
    // Use result.FinalPayload for the request
}
else if (result.Status == InterceptorChainStatus.ValidationFailed)
{
    // Handle validation failure
    Console.WriteLine($"Blocked by: {result.AbortedAt?.Interceptor}");
}

// Execute chain for incoming responses
var responseResult = await executor.ExecuteForReceivingAsync(
    @event: InterceptorEvents.ToolsCall,
    payload: responsePayload);
```

### Validation Results

Return appropriate severity levels:

- `ValidationSeverity.Info` - Informational, does not block
- `ValidationSeverity.Warn` - Warning, does not block
- `ValidationSeverity.Error` - Error, blocks execution

## InterceptingMcpClient Architecture

The `InterceptingMcpClient` follows SEP-1763 execution order for tool operations:

```
User Application
       │
       ▼
InterceptingMcpClient.CallToolAsync("my-tool", args)
       │
       ├─► 1. PayloadConverter.ToCallToolRequestPayload(toolName, args)
       │
       ├─► 2. _executor.ExecuteForSendingAsync("tools/call", payload)
       │       │
       │       ├── Mutations (sequential by priority)
       │       ├── Validations (parallel)
       │       └── Observability (fire-and-forget)
       │
       ├─► 3. If ValidationFailed → throw McpInterceptorValidationException
       │
       ├─► 4. PayloadConverter.FromCallToolRequestPayload(mutatedPayload)
       │
       ├─► 5. _inner.CallToolAsync(mutatedParams)
       │
       ├─► 6. PayloadConverter.ToCallToolResultPayload(result)
       │
       ├─► 7. _executor.ExecuteForReceivingAsync("tools/call", responsePayload)
       │
       └─► 8. Return mutated result
```

**Important:** When using `ListToolsAsync`, the returned `McpClientTool` instances are associated with the inner `McpClient`, not the `InterceptingMcpClient`. Calling `tool.InvokeAsync()` will bypass interceptors. Use `interceptedClient.CallToolAsync(toolName, args)` directly to ensure interceptors execute.

## Development Guidelines

### When Adding New Protocol Types

1. Follow the JSON-RPC patterns from the specification
2. Place protocol types in `Protocol/` directory
3. Use nullable reference types appropriately
4. Add XML documentation comments

### When Adding Server Features

1. Add handler delegates in `Server/InterceptorServerHandlers.cs`
2. Add filter delegates in `Server/InterceptorServerFilters.cs`
3. Add builder extension methods in `McpServerInterceptorBuilderExtensions.cs`
4. Ensure proper null checking and argument validation

### When Adding Client Features

1. Add handler delegates in `Client/InterceptorClientHandlers.cs`
2. Add filter delegates in `Client/InterceptorClientFilters.cs`
3. Add extension methods in `Client/McpClientInterceptorExtensions.cs`
4. Update `InterceptorChainExecutor` if chain execution logic changes
5. Ensure proper null checking and argument validation

### Testing Considerations

- Test both valid and invalid payloads
- Test severity level propagation
- Test priority ordering for mutations
- Test parallel execution for validations
- Test fire-and-forget behavior for observability
- Test `McpInterceptorValidationException` details

## Current Implementation Status

### Implemented

**Protocol Types:**
- `InterceptorResult` - Abstract base class with JSON polymorphism support
- `ValidationInterceptorResult` - For validation interceptors
- `MutationInterceptorResult` - For mutation interceptors
- `ObservabilityInterceptorResult` - For observability interceptors
- `InterceptorChainResult` - Result of chain execution
- `McpInterceptorValidationException` - Exception for validation failures
- Core types: `Interceptor`, `InterceptorEvent`, `InterceptorPhase`, `InterceptorType`, `InterceptorPriorityHint`

**Server-Side:**
- Attribute-based interceptor registration (`McpServerInterceptor`, `McpServerInterceptorType`)
- `McpServerInterceptor` abstract base class
- `ReflectionMcpServerInterceptor` for method-based interceptors
- DI builder extensions
- Handler and filter delegates

**Client-Side:**
- Attribute-based interceptor registration (`McpClientInterceptor`, `McpClientInterceptorType`)
- `McpClientInterceptor` abstract base class
- `ReflectionMcpClientInterceptor` for method-based interceptors
- `InterceptorChainExecutor` - Executes interceptor chains per SEP-1763 spec
- Extension methods for creating interceptors from types/assemblies
- Handler and filter delegates
- **`InterceptingMcpClient`** - Full McpClient wrapper with automatic interceptor execution
- **`InterceptingMcpClientOptions`** - Configuration for the intercepting client
- **`InterceptingMcpClientExtensions`** - Extension methods: `client.WithInterceptors(options)`
- **`PayloadConverter`** - JSON conversion utilities for request/response payloads

**Samples:**
- `InterceptorServiceSample` - Server-side parameter validation interceptor
- `InterceptorClientSample` - Client-side interceptor integration demonstrating:
  - Validation interceptors (PII detection)
  - Mutation interceptors (argument normalization, response redaction)
  - Observability interceptors (request/response logging)
  - Error handling with `McpInterceptorValidationException`

### Not Yet Implemented

Refer to SEP-1763 for full specification. Areas that may need work:

- `interceptor/executeChain` protocol method
- Cryptographic signature verification (future feature)
- Unit tests for client-side chain execution
- LLM completion interceptors (`llm/completion` event)

## Dependencies

- `ModelContextProtocol` SDK (0.6.0-preview.10+)
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting`
- `System.Text.Json`

## Target Frameworks

- .NET 10.0 (primary)
- .NET 9.0
- .NET 8.0
- .NET Standard 2.0 (for broader compatibility)

## Common Tasks

### Adding a New Event Type

1. Add constant to `InterceptorEvents.cs`
2. Update any event filtering logic
3. Add tests for the new event

### Adding a New Interceptor Type

1. Add result type in `Protocol/` (e.g., `MutationInterceptorResult.cs`)
2. Update `InterceptorType` enum if needed
3. Add handler support in server implementation
4. Update builder extensions

### Adding Support for New MCP Operations in InterceptingMcpClient

1. Add payload conversion methods in `PayloadConverter.cs`
2. Add the intercepted operation method in `InterceptingMcpClient.cs`
3. Follow the existing pattern: convert → execute sending chain → call inner → execute receiving chain → return

### Debugging Tips

- Check that interceptor methods are properly attributed
- Verify events match between registration and invocation
- Check priority values for mutation ordering issues
- Use logging to trace interceptor chain execution
- Inspect `McpInterceptorValidationException.ChainResult` for detailed failure info

## Coding Style Guidelines

This project follows the coding style of the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

### Key Patterns

**Null Validation:**
```csharp
// Use Throw helper instead of manual null checks
Throw.IfNull(parameter);  // NOT: if (parameter is null) throw new ArgumentNullException(...)
```

**Class Modifiers:**
- Use `sealed` on classes not designed for inheritance
- Use `internal` for implementation details not part of the public API

**Async/Await:**
- Always use `ConfigureAwait(false)` on awaits in library code
- Prefer `ValueTask` for hot paths to reduce allocations
- Include `CancellationToken` on all async methods

**JSON Serialization:**
- Use `System.Text.Json` exclusively
- Use `JsonPropertyName` attributes for property mapping

### Common Files

- `Common/Throw.cs` - Helper class for null/argument validation
- `Common/Polyfills/` - Compatibility attributes for netstandard2.0

### Reference

For coding style examples, compare with:
- `/mnt/d/code/ai/mcp/csharp-sdk/src/ModelContextProtocol.Core/`
- `/mnt/d/code/ai/mcp/csharp-sdk/src/Common/Throw.cs`
