using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ModelContextProtocol.Interceptors.Server;

/// <summary>Provides an <see cref="McpServerInterceptor"/> that's implemented via reflection.</summary>
internal sealed partial class ReflectionMcpServerInterceptor : McpServerInterceptor
{
    private readonly MethodInfo _method;
    private readonly object? _target;
    private readonly Func<RequestContext<InvokeInterceptorRequestParams>, object>? _createTargetFunc;
    private readonly IReadOnlyList<object> _metadata;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via a <see cref="Delegate"/> instance.
    /// </summary>
    public static new ReflectionMcpServerInterceptor Create(
        Delegate method,
        McpServerInterceptorCreateOptions? options)
    {
        Throw.IfNull(method);

        options = DeriveOptions(method.Method, options);

        return new ReflectionMcpServerInterceptor(method.Method, method.Target, null, options);
    }

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
    /// </summary>
    public static new ReflectionMcpServerInterceptor Create(
        MethodInfo method,
        object? target,
        McpServerInterceptorCreateOptions? options)
    {
        Throw.IfNull(method);

        options = DeriveOptions(method, options);

        return new ReflectionMcpServerInterceptor(method, target, null, options);
    }

    /// <summary>
    /// Creates an <see cref="McpServerInterceptor"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
    /// </summary>
    public static new ReflectionMcpServerInterceptor Create(
        MethodInfo method,
        Func<RequestContext<InvokeInterceptorRequestParams>, object> createTargetFunc,
        McpServerInterceptorCreateOptions? options)
    {
        Throw.IfNull(method);
        Throw.IfNull(createTargetFunc);

        options = DeriveOptions(method, options);

        return new ReflectionMcpServerInterceptor(method, null, createTargetFunc, options);
    }

    private static McpServerInterceptorCreateOptions DeriveOptions(MethodInfo method, McpServerInterceptorCreateOptions? options)
    {
        McpServerInterceptorCreateOptions newOptions = options?.Clone() ?? new();

        if (method.GetCustomAttribute<McpServerInterceptorAttribute>() is { } interceptorAttr)
        {
            newOptions.Name ??= interceptorAttr.Name;
            newOptions.Version ??= interceptorAttr.Version;
            newOptions.Description ??= interceptorAttr.Description;
            newOptions.Events ??= interceptorAttr.Events.Length > 0 ? interceptorAttr.Events : null;
            newOptions.Phase ??= interceptorAttr.Phase;

            if (interceptorAttr.PriorityHint != 0)
            {
                newOptions.PriorityHint ??= interceptorAttr.PriorityHint;
            }
        }

        if (method.GetCustomAttribute<DescriptionAttribute>() is { } descAttr)
        {
            newOptions.Description ??= descAttr.Description;
        }

        // Set metadata if not already provided
        newOptions.Metadata ??= CreateMetadata(method);

        return newOptions;
    }

    /// <summary>Initializes a new instance of the <see cref="ReflectionMcpServerInterceptor"/> class.</summary>
    private ReflectionMcpServerInterceptor(
        MethodInfo method,
        object? target,
        Func<RequestContext<InvokeInterceptorRequestParams>, object>? createTargetFunc,
        McpServerInterceptorCreateOptions? options)
    {
        _method = method;
        _target = target;
        _createTargetFunc = createTargetFunc;
        _serializerOptions = options?.SerializerOptions ?? McpJsonUtilities.DefaultOptions;
        _metadata = options?.Metadata ?? [];

        string name = options?.Name ?? DeriveName(method);
        ValidateInterceptorName(name);

        ProtocolInterceptor = new Interceptor
        {
            Name = name,
            Version = options?.Version,
            Description = options?.Description,
            Events = options?.Events?.ToList() ?? [],
            Type = InterceptorType.Validation, // PoC: Always validation type
            Phase = options?.Phase ?? InterceptorPhase.Request,
            PriorityHint = options?.PriorityHint,
            ConfigSchema = options?.ConfigSchema,
            Meta = options?.Meta,
            McpServerInterceptor = this,
        };
    }

    /// <inheritdoc />
    public override Interceptor ProtocolInterceptor { get; }

    /// <inheritdoc />
    public override IReadOnlyList<object> Metadata => _metadata;

    /// <inheritdoc />
    public override async ValueTask<ValidationInterceptorResult> InvokeAsync(
        RequestContext<InvokeInterceptorRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        Throw.IfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Resolve target instance
            object? targetInstance = _target ?? _createTargetFunc?.Invoke(request);

            try
            {
                // Bind parameters
                object?[] args = BindParameters(request, cancellationToken);

                // Invoke the method
                object? result = _method.Invoke(targetInstance, args);

                // Handle async methods
                result = await HandleAsyncResult(result).ConfigureAwait(false);

                // Convert result to ValidationInterceptorResult
                return ConvertToResult(result, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                // Dispose target if needed
                if (targetInstance != _target)
                {
                    if (targetInstance is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (targetInstance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            return new ValidationInterceptorResult
            {
                Interceptor = ProtocolInterceptor.Name,
                Phase = request.Params?.Phase ?? ProtocolInterceptor.Phase,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = ex.InnerException.Message, Severity = ValidationSeverity.Error }]
            };
        }
        catch (Exception ex)
        {
            return new ValidationInterceptorResult
            {
                Interceptor = ProtocolInterceptor.Name,
                Phase = request.Params?.Phase ?? ProtocolInterceptor.Phase,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Valid = false,
                Severity = ValidationSeverity.Error,
                Messages = [new() { Message = ex.Message, Severity = ValidationSeverity.Error }]
            };
        }
    }

    private object?[] BindParameters(RequestContext<InvokeInterceptorRequestParams> request, CancellationToken cancellationToken)
    {
        var parameters = _method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            args[i] = BindParameter(param, request, cancellationToken);
        }

        return args;
    }

    private object? BindParameter(ParameterInfo param, RequestContext<InvokeInterceptorRequestParams> request, CancellationToken cancellationToken)
    {
        var paramType = param.ParameterType;
        var paramName = param.Name?.ToLowerInvariant();

        // Bind CancellationToken
        if (paramType == typeof(CancellationToken))
        {
            return cancellationToken;
        }

        // Bind IServiceProvider
        if (paramType == typeof(IServiceProvider))
        {
            return request.Services;
        }

        // Bind McpServer
        if (typeof(McpServer).IsAssignableFrom(paramType))
        {
            return request.Server;
        }

        // Bind payload
        if (paramType == typeof(JsonNode) && paramName is "payload")
        {
            return request.Params?.Payload;
        }

        // Bind config
        if (paramType == typeof(JsonNode) && paramName is "config")
        {
            return request.Params?.Config;
        }

        // Bind context
        if (paramType == typeof(InvokeInterceptorContext))
        {
            return request.Params?.Context;
        }

        // Bind event
        if (paramType == typeof(string) && paramName is "event")
        {
            return request.Params?.Event;
        }

        // Bind phase
        if (paramType == typeof(InterceptorPhase) && paramName is "phase")
        {
            return request.Params?.Phase ?? ProtocolInterceptor.Phase;
        }

        // Try to resolve from DI
        if (request.Services is not null)
        {
            var service = request.Services.GetService(paramType);
            if (service is not null)
            {
                return service;
            }
        }

        // Use default value if available
        if (param.HasDefaultValue)
        {
            return param.DefaultValue;
        }

        return null;
    }

    private static async ValueTask<object?> HandleAsyncResult(object? result)
    {
        if (result is null)
        {
            return null;
        }

        // Handle Task
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            return GetTaskResult(task);
        }

        // Handle ValueTask
        if (result is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            return null;
        }

        // Handle ValueTask<ValidationInterceptorResult>
        if (result is ValueTask<ValidationInterceptorResult> valueTaskResult)
        {
            return await valueTaskResult.ConfigureAwait(false);
        }

        // Handle ValueTask<bool>
        if (result is ValueTask<bool> valueTaskBool)
        {
            return await valueTaskBool.ConfigureAwait(false);
        }

        return result;
    }

    private static object? GetTaskResult(Task task)
    {
        // Use dynamic to avoid reflection issues with trimming
        // For Task<T> types, we need to get the Result
        if (task is Task<ValidationInterceptorResult> taskResult)
        {
            return taskResult.Result;
        }

        if (task is Task<bool> taskBool)
        {
            return taskBool.Result;
        }

        // For non-generic Task, there's no result
        return null;
    }

    private ValidationInterceptorResult ConvertToResult(object? result, long durationMs)
    {
        if (result is ValidationInterceptorResult validationResult)
        {
            validationResult.Interceptor ??= ProtocolInterceptor.Name;
            validationResult.DurationMs = durationMs;
            return validationResult;
        }

        if (result is bool isValid)
        {
            return new ValidationInterceptorResult
            {
                Interceptor = ProtocolInterceptor.Name,
                Phase = ProtocolInterceptor.Phase,
                DurationMs = durationMs,
                Valid = isValid,
                Severity = isValid ? null : ValidationSeverity.Error,
            };
        }

        // Default to valid if no result
        return new ValidationInterceptorResult
        {
            Interceptor = ProtocolInterceptor.Name,
            Phase = ProtocolInterceptor.Phase,
            DurationMs = durationMs,
            Valid = true,
        };
    }

    /// <summary>Creates a name to use based on the supplied method.</summary>
    internal static string DeriveName(MethodInfo method, JsonNamingPolicy? policy = null)
    {
        string name = method.Name;

        // Remove any "Async" suffix if the method is an async method and if the method name isn't just "Async".
        const string AsyncSuffix = "Async";
        if (IsAsyncMethod(method) &&
            name.EndsWith(AsyncSuffix, StringComparison.Ordinal) &&
            name.Length > AsyncSuffix.Length)
        {
            name = name.Substring(0, name.Length - AsyncSuffix.Length);
        }

        // Replace anything other than ASCII letters or digits with underscores, trim off any leading or trailing underscores.
        name = NonAsciiLetterDigitsRegex().Replace(name, "_").Trim('_');

        // If after all our transformations the name is empty, just use the original method name.
        if (name.Length == 0)
        {
            name = method.Name;
        }

        // Case the name based on the provided naming policy.
        return (policy ?? JsonNamingPolicy.SnakeCaseLower).ConvertName(name) ?? name;

        static bool IsAsyncMethod(MethodInfo method)
        {
            Type t = method.ReturnType;

            if (t == typeof(Task) || t == typeof(ValueTask))
            {
                return true;
            }

            if (t.IsGenericType)
            {
                t = t.GetGenericTypeDefinition();
                if (t == typeof(Task<>) || t == typeof(ValueTask<>))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Creates metadata from attributes on the specified method and its declaring class.</summary>
    internal static IReadOnlyList<object> CreateMetadata(MethodInfo method)
    {
        List<object> metadata = [method];

        if (method.DeclaringType is not null)
        {
            metadata.AddRange(method.DeclaringType.GetCustomAttributes());
        }

        metadata.AddRange(method.GetCustomAttributes());

        return metadata.AsReadOnly();
    }

#if NET
    /// <summary>Regex that flags runs of characters other than ASCII digits or letters.</summary>
    [GeneratedRegex("[^0-9A-Za-z]+")]
    private static partial Regex NonAsciiLetterDigitsRegex();

    /// <summary>Regex that validates interceptor names.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,128}\z")]
    private static partial Regex ValidateInterceptorNameRegex();
#else
    private static Regex NonAsciiLetterDigitsRegex() => _nonAsciiLetterDigits;
    private static readonly Regex _nonAsciiLetterDigits = new("[^0-9A-Za-z]+", RegexOptions.Compiled);

    private static Regex ValidateInterceptorNameRegex() => _validateInterceptorName;
    private static readonly Regex _validateInterceptorName = new(@"^[A-Za-z0-9_.-]{1,128}\z", RegexOptions.Compiled);
#endif

    private static void ValidateInterceptorName(string name)
    {
        if (name is null)
        {
            throw new ArgumentException("Interceptor name cannot be null.");
        }

        if (!ValidateInterceptorNameRegex().IsMatch(name))
        {
            throw new ArgumentException($"The interceptor name '{name}' is invalid. Interceptor names must match the regular expression '{ValidateInterceptorNameRegex()}'");
        }
    }
}
