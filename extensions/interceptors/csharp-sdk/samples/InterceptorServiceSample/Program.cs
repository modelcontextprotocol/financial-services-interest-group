using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Interceptors;

// This sample demonstrates how to create an MCP server that exposes validation interceptors.
// Interceptors can be deployed as separate services (sidecars, gateways) to validate,
// mutate, or observe MCP messages without modifying the original server or client.

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithInterceptors<ParameterValidator>();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

await builder.Build().RunAsync();
