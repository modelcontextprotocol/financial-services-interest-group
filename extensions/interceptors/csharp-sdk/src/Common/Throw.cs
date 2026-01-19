using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ModelContextProtocol.Interceptors;

/// <summary>Provides helper methods for throwing exceptions.</summary>
internal static class Throw
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="arg"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="arg">The argument to validate.</param>
    /// <param name="parameterName">The name of the parameter (automatically populated by the compiler).</param>
    /// <exception cref="ArgumentNullException"><paramref name="arg"/> is <see langword="null"/>.</exception>
    public static void IfNull([NotNull] object? arg, [CallerArgumentExpression(nameof(arg))] string? parameterName = null)
    {
        if (arg is null)
        {
            ThrowArgumentNullException(parameterName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="arg"/> is <see langword="null"/>, empty, or whitespace.
    /// </summary>
    /// <param name="arg">The string argument to validate.</param>
    /// <param name="parameterName">The name of the parameter (automatically populated by the compiler).</param>
    /// <exception cref="ArgumentNullException"><paramref name="arg"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="arg"/> is empty or whitespace.</exception>
    public static void IfNullOrWhiteSpace([NotNull] string? arg, [CallerArgumentExpression(nameof(arg))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            ThrowArgumentNullOrWhiteSpaceException(arg, parameterName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="arg"/> is negative.
    /// </summary>
    /// <param name="arg">The value to validate.</param>
    /// <param name="parameterName">The name of the parameter (automatically populated by the compiler).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arg"/> is negative.</exception>
    public static void IfNegative(int arg, [CallerArgumentExpression(nameof(arg))] string? parameterName = null)
    {
        if (arg < 0)
        {
            ThrowArgumentOutOfRangeException(parameterName);
        }
    }

    [DoesNotReturn]
    private static void ThrowArgumentNullException(string? parameterName) =>
        throw new ArgumentNullException(parameterName);

    [DoesNotReturn]
    private static void ThrowArgumentNullOrWhiteSpaceException(string? arg, string? parameterName)
    {
        if (arg is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        throw new ArgumentException("Value cannot be empty or composed entirely of whitespace.", parameterName);
    }

    [DoesNotReturn]
    private static void ThrowArgumentOutOfRangeException(string? parameterName) =>
        throw new ArgumentOutOfRangeException(parameterName, "Value must not be negative.");
}
