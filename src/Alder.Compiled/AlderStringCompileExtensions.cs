namespace Alder.Compiled;

/// <summary>
/// String extension methods that route compilation through the global <see cref="AlderEval"/> engine.
/// </summary>
public static class AlderStringCompileExtensions
{
    /// <summary>
    /// Compiles this string as a code body with named parameters into a native delegate.
    /// </summary>
    /// <typeparam name="TDelegate">A Func or Action delegate type.</typeparam>
    /// <param name="code">The C# code body to compile.</param>
    /// <param name="parameterNames">Names for each parameter, matching the delegate's type arguments by position.</param>
    /// <returns>A compiled delegate that can be invoked with typed arguments.</returns>
    public static TDelegate Compile<TDelegate>(this string code, params string[] parameterNames)
        where TDelegate : Delegate
        => AlderEval.GetEngine().Compile<TDelegate>(code, parameterNames);

    /// <summary>
    /// Compiles this string as an expression and returns a <see cref="Func{T}"/> backed by the global engine state.
    /// </summary>
    public static Func<T?> CompileToFunc<T>(this string code)
        => AlderEval.GetEngine().CompileToFunc<T>(code);
}
