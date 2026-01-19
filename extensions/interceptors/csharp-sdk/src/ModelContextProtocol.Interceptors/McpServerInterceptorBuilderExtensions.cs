using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Interceptors;
using ModelContextProtocol.Interceptors.Server;
using ModelContextProtocol.Server;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring MCP interceptors via dependency injection.
/// </summary>
public static class McpServerInterceptorBuilderExtensions
{
    private const string WithInterceptorsRequiresUnreferencedCodeMessage =
        $"The non-generic {nameof(WithInterceptors)} and {nameof(WithInterceptorsFromAssembly)} methods require dynamic lookup of method metadata" +
        $"and might not work in Native AOT. Use the generic {nameof(WithInterceptors)} method instead.";

    /// <summary>Adds <see cref="McpServerInterceptor"/> instances to the service collection backing <paramref name="builder"/>.</summary>
    /// <typeparam name="TInterceptorType">The interceptor type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TInterceptorType"/>
    /// type, where the methods are attributed as <see cref="McpServerInterceptorAttribute"/>, and adds an <see cref="McpServerInterceptor"/>
    /// instance for each. For instance methods, an instance is constructed for each invocation of the interceptor.
    /// </remarks>
    public static IMcpServerBuilder WithInterceptors<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicConstructors)] TInterceptorType>(
        this IMcpServerBuilder builder,
        JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(builder);

        foreach (var interceptorMethod in typeof(TInterceptorType).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (interceptorMethod.GetCustomAttribute<McpServerInterceptorAttribute>() is not null)
            {
                builder.Services.AddSingleton((Func<IServiceProvider, McpServerInterceptor>)(interceptorMethod.IsStatic ?
                    services => McpServerInterceptor.Create(interceptorMethod, options: new() { Services = services, SerializerOptions = serializerOptions }) :
                    services => McpServerInterceptor.Create(interceptorMethod, static r => CreateTarget(r.Services, typeof(TInterceptorType)), new() { Services = services, SerializerOptions = serializerOptions })));
            }
        }

        return builder;
    }

    /// <summary>Adds <see cref="McpServerInterceptor"/> instances to the service collection backing <paramref name="builder"/>.</summary>
    /// <typeparam name="TInterceptorType">The interceptor type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="target">The target instance from which the interceptors should be sourced.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method discovers all methods (public and non-public) on the specified <typeparamref name="TInterceptorType"/>
    /// type, where the methods are attributed as <see cref="McpServerInterceptorAttribute"/>, and adds an <see cref="McpServerInterceptor"/>
    /// instance for each, using <paramref name="target"/> as the associated instance for instance methods.
    /// </para>
    /// <para>
    /// However, if <typeparamref name="TInterceptorType"/> is itself an <see cref="IEnumerable{T}"/> of <see cref="McpServerInterceptor"/>,
    /// this method registers those interceptors directly without scanning for methods on <typeparamref name="TInterceptorType"/>.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithInterceptors<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods)] TInterceptorType>(
        this IMcpServerBuilder builder,
        TInterceptorType target,
        JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(builder);
        Throw.IfNull(target);

        if (target is IEnumerable<McpServerInterceptor> interceptors)
        {
            return builder.WithInterceptors(interceptors);
        }

        foreach (var interceptorMethod in typeof(TInterceptorType).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (interceptorMethod.GetCustomAttribute<McpServerInterceptorAttribute>() is not null)
            {
                builder.Services.AddSingleton(services => McpServerInterceptor.Create(
                    interceptorMethod,
                    interceptorMethod.IsStatic ? null : target,
                    new() { Services = services, SerializerOptions = serializerOptions }));
            }
        }

        return builder;
    }

    /// <summary>Adds <see cref="McpServerInterceptor"/> instances to the service collection backing <paramref name="builder"/>.</summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="interceptors">The <see cref="McpServerInterceptor"/> instances to add to the server.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptors"/> is <see langword="null"/>.</exception>
    public static IMcpServerBuilder WithInterceptors(this IMcpServerBuilder builder, IEnumerable<McpServerInterceptor> interceptors)
    {
        Throw.IfNull(builder);
        Throw.IfNull(interceptors);

        foreach (var interceptor in interceptors)
        {
            if (interceptor is not null)
            {
                builder.Services.AddSingleton(interceptor);
            }
        }

        return builder;
    }

    /// <summary>Adds <see cref="McpServerInterceptor"/> instances to the service collection backing <paramref name="builder"/>.</summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="interceptorTypes">Types with <see cref="McpServerInterceptorAttribute"/>-attributed methods to add as interceptors to the server.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorTypes"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method discovers all instance and static methods (public and non-public) on the specified <paramref name="interceptorTypes"/>
    /// types, where the methods are attributed as <see cref="McpServerInterceptorAttribute"/>, and adds an <see cref="McpServerInterceptor"/>
    /// instance for each. For instance methods, an instance is constructed for each invocation of the interceptor.
    /// </remarks>
    [RequiresUnreferencedCode(WithInterceptorsRequiresUnreferencedCodeMessage)]
    public static IMcpServerBuilder WithInterceptors(this IMcpServerBuilder builder, IEnumerable<Type> interceptorTypes, JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(builder);
        Throw.IfNull(interceptorTypes);

        foreach (var interceptorType in interceptorTypes)
        {
            if (interceptorType is not null)
            {
                foreach (var interceptorMethod in interceptorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (interceptorMethod.GetCustomAttribute<McpServerInterceptorAttribute>() is not null)
                    {
                        builder.Services.AddSingleton((Func<IServiceProvider, McpServerInterceptor>)(interceptorMethod.IsStatic ?
                            services => McpServerInterceptor.Create(interceptorMethod, options: new() { Services = services, SerializerOptions = serializerOptions }) :
                            services => McpServerInterceptor.Create(interceptorMethod, r => CreateTarget(r.Services, interceptorType), new() { Services = services, SerializerOptions = serializerOptions })));
                    }
                }
            }
        }

        return builder;
    }

    /// <summary>
    /// Adds types marked with the <see cref="McpServerInterceptorTypeAttribute"/> attribute from the given assembly as interceptors to the server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="serializerOptions">The serializer options governing interceptor parameter marshalling.</param>
    /// <param name="interceptorAssembly">The assembly to load the types from. If <see langword="null"/>, the calling assembly is used.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method scans the specified assembly (or the calling assembly if none is provided) for classes
    /// marked with the <see cref="McpServerInterceptorTypeAttribute"/>. It then discovers all methods within those
    /// classes that are marked with the <see cref="McpServerInterceptorAttribute"/> and registers them as <see cref="McpServerInterceptor"/>s
    /// in the <paramref name="builder"/>'s <see cref="IServiceCollection"/>.
    /// </para>
    /// <para>
    /// The method automatically handles both static and instance methods. For instance methods, a new instance
    /// of the containing class is constructed for each invocation of the interceptor.
    /// </para>
    /// <para>
    /// Interceptors registered through this method can be discovered by clients using the <c>interceptors/list</c> request
    /// and invoked using the <c>interceptor/invoke</c> request.
    /// </para>
    /// <para>
    /// Note that this method performs reflection at runtime and might not work in Native AOT scenarios. For
    /// Native AOT compatibility, consider using the generic <see cref="M:WithInterceptors"/> method instead.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(WithInterceptorsRequiresUnreferencedCodeMessage)]
    public static IMcpServerBuilder WithInterceptorsFromAssembly(this IMcpServerBuilder builder, Assembly? interceptorAssembly = null, JsonSerializerOptions? serializerOptions = null)
    {
        Throw.IfNull(builder);

        interceptorAssembly ??= Assembly.GetCallingAssembly();

        return builder.WithInterceptors(
            from t in interceptorAssembly.GetTypes()
            where t.GetCustomAttribute<McpServerInterceptorTypeAttribute>() is not null
            select t,
            serializerOptions);
    }

    /// <summary>
    /// Configures a handler for listing interceptors available from the Model Context Protocol server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="handler">The handler that processes list interceptors requests.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This handler is called when a client requests a list of available interceptors. It should return all interceptors
    /// that can be invoked through the server, including their names, descriptions, and supported events.
    /// </para>
    /// <para>
    /// When interceptors are also defined using <see cref="McpServerInterceptor"/> collection, both sets of interceptors
    /// will be combined in the response to clients. This allows for a mix of programmatically defined
    /// interceptors and dynamically generated interceptors.
    /// </para>
    /// <para>
    /// This method is typically paired with <see cref="WithInvokeInterceptorHandler"/> to provide a complete interceptors implementation,
    /// where <see cref="WithListInterceptorsHandler"/> advertises available interceptors and <see cref="WithInvokeInterceptorHandler"/>
    /// executes them when invoked by clients.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithListInterceptorsHandler(this IMcpServerBuilder builder, Func<RequestContext<ListInterceptorsRequestParams>, CancellationToken, ValueTask<ListInterceptorsResult>> handler)
    {
        Throw.IfNull(builder);

        builder.Services.Configure<InterceptorServerHandlers>(s => s.ListInterceptorsHandler = handler);
        return builder;
    }

    /// <summary>
    /// Configures a handler for invoking interceptors available from the Model Context Protocol server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="handler">The handler function that processes interceptor invocations.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The invoke interceptor handler is responsible for executing validation interceptors and returning their results to clients.
    /// This method is typically paired with <see cref="WithListInterceptorsHandler"/> to provide a complete interceptors implementation,
    /// where <see cref="WithListInterceptorsHandler"/> advertises available interceptors and this handler executes them.
    /// </remarks>
    public static IMcpServerBuilder WithInvokeInterceptorHandler(this IMcpServerBuilder builder, Func<RequestContext<InvokeInterceptorRequestParams>, CancellationToken, ValueTask<ValidationInterceptorResult>> handler)
    {
        Throw.IfNull(builder);

        builder.Services.Configure<InterceptorServerHandlers>(s => s.InvokeInterceptorHandler = handler);
        return builder;
    }

    /// <summary>
    /// Adds a filter to the list interceptors handler pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="filter">The filter function that wraps the handler.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This filter wraps handlers that return a list of available interceptors when requested by a client.
    /// The filter can modify, log, or perform additional operations on requests and responses.
    /// </para>
    /// <para>
    /// This filter works alongside any interceptors defined in the <see cref="McpServerInterceptor"/> collection.
    /// Interceptors from both sources will be combined when returning results to clients.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder AddListInterceptorsFilter(this IMcpServerBuilder builder, Func<RequestContext<ListInterceptorsRequestParams>, Func<RequestContext<ListInterceptorsRequestParams>, CancellationToken, ValueTask<ListInterceptorsResult>>, CancellationToken, ValueTask<ListInterceptorsResult>> filter)
    {
        Throw.IfNull(builder);

        builder.Services.Configure<InterceptorServerFilters>(options => options.ListInterceptorsFilters.Add(filter));
        return builder;
    }

    /// <summary>
    /// Adds a filter to the invoke interceptor handler pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="filter">The filter function that wraps the handler.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This filter wraps handlers that are invoked when a client calls an interceptor.
    /// The filter can modify, log, or perform additional operations on requests and responses.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder AddInvokeInterceptorFilter(this IMcpServerBuilder builder, Func<RequestContext<InvokeInterceptorRequestParams>, Func<RequestContext<InvokeInterceptorRequestParams>, CancellationToken, ValueTask<ValidationInterceptorResult>>, CancellationToken, ValueTask<ValidationInterceptorResult>> filter)
    {
        Throw.IfNull(builder);

        builder.Services.Configure<InterceptorServerFilters>(options => options.InvokeInterceptorFilters.Add(filter));
        return builder;
    }

    /// <summary>Creates an instance of the target object.</summary>
    private static object CreateTarget(
        IServiceProvider? services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) =>
        services is not null ? ActivatorUtilities.CreateInstance(services, type) :
        Activator.CreateInstance(type)!;
}
