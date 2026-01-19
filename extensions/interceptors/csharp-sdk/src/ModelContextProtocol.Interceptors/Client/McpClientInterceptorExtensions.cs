using ModelContextProtocol.Client;
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Client;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring MCP client interceptors.
/// </summary>
public static class McpClientInterceptorExtensions
{
    private const string WithInterceptorsRequiresUnreferencedCodeMessage =
        $"The non-generic {nameof(WithInterceptors)} and {nameof(WithInterceptorsFromAssembly)} methods require dynamic lookup of method metadata" +
        $"and might not work in Native AOT. Use the generic {nameof(WithInterceptors)} method instead.";

    /// <summary>
    /// Creates <see cref="McpClientInterceptor"/> instances from a type.
    /// </summary>
    /// <typeparam name="TInterceptorType">The interceptor type.</typeparam>
    /// <param name="services">Optional service provider for dependency injection.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>A collection of <see cref="McpClientInterceptor"/> instances.</returns>
    /// <remarks>
    /// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TInterceptorType"/>
    /// type, where the methods are attributed as <see cref="McpClientInterceptorAttribute"/>, and creates an <see cref="McpClientInterceptor"/>
    /// instance for each.
    /// </remarks>
    public static IEnumerable<McpClientInterceptor> WithInterceptors<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicConstructors)] TInterceptorType>(
        IServiceProvider? services = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        foreach (var interceptorMethod in typeof(TInterceptorType).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (interceptorMethod.GetCustomAttribute<McpClientInterceptorAttribute>() is not null)
            {
                yield return interceptorMethod.IsStatic
                    ? McpClientInterceptor.Create(interceptorMethod, options: new() { Services = services, SerializerOptions = serializerOptions })
                    : McpClientInterceptor.Create(interceptorMethod, ctx => CreateTarget(ctx.Services, typeof(TInterceptorType)), new() { Services = services, SerializerOptions = serializerOptions });
            }
        }
    }

    /// <summary>
    /// Creates <see cref="McpClientInterceptor"/> instances from a target instance.
    /// </summary>
    /// <typeparam name="TInterceptorType">The interceptor type.</typeparam>
    /// <param name="target">The target instance from which the interceptors should be sourced.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>A collection of <see cref="McpClientInterceptor"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method discovers all methods (public and non-public) on the specified <typeparamref name="TInterceptorType"/>
    /// type, where the methods are attributed as <see cref="McpClientInterceptorAttribute"/>, and creates an <see cref="McpClientInterceptor"/>
    /// instance for each, using <paramref name="target"/> as the associated instance for instance methods.
    /// </para>
    /// <para>
    /// If <typeparamref name="TInterceptorType"/> is itself an <see cref="IEnumerable{T}"/> of <see cref="McpClientInterceptor"/>,
    /// this method returns those interceptors directly without scanning for methods on <typeparamref name="TInterceptorType"/>.
    /// </para>
    /// </remarks>
    public static IEnumerable<McpClientInterceptor> WithInterceptors<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods)] TInterceptorType>(
        TInterceptorType target,
        JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(target);

        if (target is IEnumerable<McpClientInterceptor> interceptors)
        {
            return interceptors;
        }

        return GetInterceptorsFromTarget(target, serializerOptions);
    }

    private static IEnumerable<McpClientInterceptor> GetInterceptorsFromTarget<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods)] TInterceptorType>(
        TInterceptorType target,
        JsonSerializerOptions? serializerOptions)
    {
        foreach (var interceptorMethod in typeof(TInterceptorType).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (interceptorMethod.GetCustomAttribute<McpClientInterceptorAttribute>() is not null)
            {
                yield return McpClientInterceptor.Create(
                    interceptorMethod,
                    interceptorMethod.IsStatic ? null : target,
                    new() { SerializerOptions = serializerOptions });
            }
        }
    }

    /// <summary>
    /// Creates <see cref="McpClientInterceptor"/> instances from types.
    /// </summary>
    /// <param name="interceptorTypes">Types with <see cref="McpClientInterceptorAttribute"/>-attributed methods to add as interceptors.</param>
    /// <param name="services">Optional service provider for dependency injection.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>A collection of <see cref="McpClientInterceptor"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="interceptorTypes"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method discovers all instance and static methods (public and non-public) on the specified <paramref name="interceptorTypes"/>
    /// types, where the methods are attributed as <see cref="McpClientInterceptorAttribute"/>, and creates an <see cref="McpClientInterceptor"/>
    /// instance for each.
    /// </remarks>
    [RequiresUnreferencedCode(WithInterceptorsRequiresUnreferencedCodeMessage)]
    public static IEnumerable<McpClientInterceptor> WithInterceptors(
        IEnumerable<Type> interceptorTypes,
        IServiceProvider? services = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(interceptorTypes);

        foreach (var interceptorType in interceptorTypes)
        {
            if (interceptorType is null) continue;

            foreach (var interceptorMethod in interceptorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (interceptorMethod.GetCustomAttribute<McpClientInterceptorAttribute>() is not null)
                {
                    yield return interceptorMethod.IsStatic
                        ? McpClientInterceptor.Create(interceptorMethod, options: new() { Services = services, SerializerOptions = serializerOptions })
                        : McpClientInterceptor.Create(interceptorMethod, ctx => CreateTarget(ctx.Services, interceptorType), new() { Services = services, SerializerOptions = serializerOptions });
                }
            }
        }
    }

    /// <summary>
    /// Creates <see cref="McpClientInterceptor"/> instances from types marked with <see cref="McpClientInterceptorTypeAttribute"/> in an assembly.
    /// </summary>
    /// <param name="interceptorAssembly">The assembly to load the types from. If <see langword="null"/>, the calling assembly is used.</param>
    /// <param name="services">Optional service provider for dependency injection.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>A collection of <see cref="McpClientInterceptor"/> instances.</returns>
    /// <remarks>
    /// <para>
    /// This method scans the specified assembly (or the calling assembly if none is provided) for classes
    /// marked with the <see cref="McpClientInterceptorTypeAttribute"/>. It then discovers all methods within those
    /// classes that are marked with the <see cref="McpClientInterceptorAttribute"/> and creates <see cref="McpClientInterceptor"/>s.
    /// </para>
    /// <para>
    /// Note that this method performs reflection at runtime and might not work in Native AOT scenarios. For
    /// Native AOT compatibility, consider using the generic <see cref="M:WithInterceptors"/> method instead.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(WithInterceptorsRequiresUnreferencedCodeMessage)]
    public static IEnumerable<McpClientInterceptor> WithInterceptorsFromAssembly(
        Assembly? interceptorAssembly = null,
        IServiceProvider? services = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        interceptorAssembly ??= Assembly.GetCallingAssembly();

        var interceptorTypes = from t in interceptorAssembly.GetTypes()
                               where t.GetCustomAttribute<McpClientInterceptorTypeAttribute>() is not null
                               select t;

        return WithInterceptors(interceptorTypes, services, serializerOptions);
    }

    /// <summary>Creates an instance of the target object.</summary>
    private static object CreateTarget(
        IServiceProvider? services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        if (services is not null)
        {
            return ActivatorUtilities.CreateInstance(services, type);
        }

        return Activator.CreateInstance(type)!;
    }
}
