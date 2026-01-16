namespace ModelContextProtocol.Interceptors.Client;

/// <summary>
/// Attribute applied to a class to indicate that it contains MCP client interceptor methods.
/// </summary>
/// <remarks>
/// <para>
/// Classes marked with this attribute will be scanned for methods marked with
/// <see cref="McpClientInterceptorAttribute"/> when using assembly-based interceptor discovery.
/// </para>
/// <para>
/// This attribute is used by the <c>WithInterceptorsFromAssembly</c> extension method
/// in <c>Microsoft.Extensions.DependencyInjection.McpClientInterceptorExtensions</c>
/// to locate interceptor types in an assembly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [McpClientInterceptorType]
/// public class MyClientInterceptors
/// {
///     [McpClientInterceptor(
///         Name = "request-validator",
///         Events = new[] { InterceptorEvents.ToolsCall },
///         Phase = InterceptorPhase.Request)]
///     public ValidationInterceptorResult ValidateRequest(JsonNode? payload)
///     {
///         // Validation logic
///         return new ValidationInterceptorResult { Valid = true };
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class McpClientInterceptorTypeAttribute : Attribute
{
}
