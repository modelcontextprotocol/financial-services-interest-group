# ModelContextProtocol.Interceptors

MCP Interceptors extension for the Model Context Protocol (MCP) .NET SDK.

This package provides the interceptor framework (SEP-1763) for validating, mutating, and observing MCP messages.

## Features

- **Validation Interceptors**: Validate messages and provide detailed feedback with suggestions
- **Attribute-based Discovery**: Mark methods with `[McpServerInterceptor]` for automatic discovery
- **Dependency Injection Integration**: Full support for DI in interceptor methods

## Installation

```bash
dotnet add package ModelContextProtocol.Interceptors
```

## Usage

```csharp
using ModelContextProtocol.Interceptors;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<MyValidators>();

await builder.Build().RunAsync();

[McpServerInterceptorType]
public class MyValidators
{
    [McpServerInterceptor(
        Events = [InterceptorEvents.ToolsCall],
        Description = "Validates tool call parameters")]
    public ValidationInterceptorResult ValidateToolCall(JsonNode payload)
    {
        // Validation logic here
        return new ValidationInterceptorResult { Valid = true };
    }
}
```

## Documentation

For more information, see the [MCP documentation](https://modelcontextprotocol.io).
