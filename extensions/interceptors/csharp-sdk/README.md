# MCP Interceptors C# SDK

This library provides interceptor support for the Model Context Protocol (MCP) .NET SDK. Interceptors enable validation, mutation, and observation of MCP messages without modifying the original server or client implementations.

## Overview

MCP Interceptors (SEP-1763) allow you to:

- **Validate** incoming requests before they reach handlers
- **Mutate** requests or responses to transform data
- **Observe** message flow for logging, metrics, or auditing

Interceptors can be deployed as:
- Sidecars alongside MCP servers
- Gateway services that proxy MCP traffic
- Embedded validators within applications

## Installation

```bash
dotnet add package ModelContextProtocol.Interceptors
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Interceptors;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<ParameterValidator>();

await builder.Build().RunAsync();
```

## Creating an Interceptor

```csharp
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using System.Text.Json.Nodes;

[McpServerInterceptorType]
public class ParameterValidator
{
    [McpServerInterceptor(
        Name = "parameter-validator",
        Description = "Validates tool call parameters",
        Events = new[] { InterceptorEvents.ToolsCall },
        Phase = InterceptorPhase.Request)]
    public ValidationInterceptorResult ValidateToolCall(JsonNode? payload)
    {
        if (payload is null)
        {
            return new ValidationInterceptorResult
            {
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = "Payload is required" }]
            };
        }

        return new ValidationInterceptorResult { Valid = true };
    }
}
```

## Interceptor Types

### Validation Interceptors
Validate requests/responses and return pass/fail results with optional error messages.

### Mutation Interceptors
Transform request or response payloads before they continue through the pipeline.

### Observability Interceptors
Observe message flow for logging, metrics collection, or auditing without modifying data.

## Configuration Options

### Phase
- `InterceptorPhase.Request` - Intercept incoming requests
- `InterceptorPhase.Response` - Intercept outgoing responses
- `InterceptorPhase.Both` - Intercept both directions

### Events
Interceptors can target specific MCP events:
- `InterceptorEvents.ToolsCall` - Tool invocation requests
- `InterceptorEvents.PromptGet` - Prompt retrieval
- `InterceptorEvents.ResourceRead` - Resource access
- And more...

### Priority
Use `PriorityHint` to control interceptor execution order (lower values run first).

## Sample Projects

See the `samples/InterceptorServiceSample` directory for a complete example of a security-focused validation interceptor.

## Requirements

- .NET 8.0 or later (or .NET Standard 2.0 compatible runtime)
- ModelContextProtocol SDK 0.1.0-preview.10 or later

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please see the FSIG CONTRIBUTING.md for guidelines.
