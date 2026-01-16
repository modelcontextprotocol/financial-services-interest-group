namespace ModelContextProtocol.Interceptors.Server;

/// <summary>
/// Attribute used to mark a class as containing MCP server interceptor methods.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a class, this attribute indicates that the class contains methods
/// that should be scanned for <see cref="McpServerInterceptorAttribute"/> attributes
/// when discovering interceptors.
/// </para>
/// <para>
/// This attribute is used in conjunction with <see cref="McpServerInterceptorAttribute"/>
/// to enable automatic discovery and registration of interceptors.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class McpServerInterceptorTypeAttribute : Attribute
{
}
