# AGENTS.md - C# SDK for MCP Interceptors

This document provides guidance for AI coding agents working on the MCP Interceptors C# SDK implementation.

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
│   │   └── InterceptorChainResult.cs  # Chain execution result
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
│   │   └── InterceptorChainExecutor.cs  # Chain execution per SEP-1763
│   └── McpServerInterceptorBuilderExtensions.cs  # DI extensions
├── samples/
│   └── InterceptorServiceSample/   # Example validation interceptor
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

### Creating a Client-Side Interceptor

```csharp
[McpClientInterceptorType]
public class MyClientInterceptor
{
    [McpClientInterceptor(
        Name = "request-validator",
        Description = "Validates outgoing requests",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request,
        PriorityHint = 0)]
    public ValidationInterceptorResult ValidateRequest(JsonNode? payload)
    {
        // Validate payload before sending to server
        return ValidationInterceptorResult.Success();
    }
    
    [McpClientInterceptor(
        Name = "response-mutator",
        Type = InterceptorType.Mutation,
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Response,
        PriorityHint = 10)]
    public MutationInterceptorResult MutateResponse(JsonNode? payload)
    {
        // Transform response received from server
        return new MutationInterceptorResult 
        { 
            Modified = true, 
            Payload = transformedPayload 
        };
    }
}
```

### Registration via DI (Server)

```csharp
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<MyServerInterceptor>();
```

### Client Interceptor Chain Execution

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

### Registration via DI

```csharp
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<MyInterceptor>();
```

### Validation Results

Return appropriate severity levels:

- `ValidationSeverity.Info` - Informational, does not block
- `ValidationSeverity.Warn` - Warning, does not block
- `ValidationSeverity.Error` - Error, blocks execution

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

## Current Implementation Status

### Implemented

**Protocol Types:**
- `InterceptorResult` - Abstract base class with JSON polymorphism support
- `ValidationInterceptorResult` - For validation interceptors
- `MutationInterceptorResult` - For mutation interceptors
- `ObservabilityInterceptorResult` - For observability interceptors
- `InterceptorChainResult` - Result of chain execution
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

**Sample:**
- Parameter validation interceptor sample

### Not Yet Implemented

Refer to SEP-1763 for full specification. Areas that may need work:

- Client-side integration with `IMcpClient` (hooking into actual request/response flow)
- `interceptor/executeChain` protocol method
- Cryptographic signature verification (future feature)
- Unit tests for client-side chain execution

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

### Debugging Tips

- Check that interceptor methods are properly attributed
- Verify events match between registration and invocation
- Check priority values for mutation ordering issues
- Use logging to trace interceptor chain execution
