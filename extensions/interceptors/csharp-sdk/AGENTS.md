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
│   ├── Server/             # Server-side implementation (attributes, handlers, filters)
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

### Creating an Interceptor

```csharp
[McpServerInterceptorType]
public class MyInterceptor
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

1. Add handler delegates in `InterceptorServerHandlers.cs`
2. Add filter delegates in `InterceptorServerFilters.cs`
3. Add builder extension methods in `McpServerInterceptorBuilderExtensions.cs`
4. Ensure proper null checking and argument validation

### Testing Considerations

- Test both valid and invalid payloads
- Test severity level propagation
- Test priority ordering for mutations
- Test parallel execution for validations
- Test fire-and-forget behavior for observability

## Current Implementation Status

### Implemented

- Core protocol types (`Interceptor`, `InterceptorEvent`, `InterceptorPhase`, etc.)
- Validation interceptor result types
- Server-side attribute-based interceptor registration
- DI builder extensions
- Sample parameter validation interceptor

### Not Yet Implemented

Refer to SEP-1763 for full specification. Areas that may need work:

- Mutation interceptor result types and execution
- Observability interceptor result types
- Chain execution (`interceptor/executeChain`)
- Client-side interceptor support
- Cryptographic signature verification (future feature)

## Dependencies

- `ModelContextProtocol` SDK (0.6.0-preview.10+)
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting`
- `System.Text.Json`

## Target Frameworks

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
