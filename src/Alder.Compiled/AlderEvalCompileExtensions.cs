namespace Alder.Compiled;

/// <summary>
/// Exposes the compiled backend through the global <see cref="AlderEval"/> engine.
/// Configure the global engine with <c>UseCompiler()</c> before calling these APIs.
/// </summary>
public static class AlderEvalCompileExtensions
{
    /// <summary>
    /// Compiles a code body with named parameters into a native <typeparamref name="TDelegate"/> delegate
    /// using the global engine.
    /// </summary>
    /// <typeparam name="TDelegate">A Func or Action delegate type.</typeparam>
    /// <param name="code">The code body to compile.</param>
    /// <param name="parameterNames">Names for each parameter, matching the delegate's type arguments by position.</param>
    /// <returns>A compiled delegate that can be invoked with typed arguments.</returns>
    public static TDelegate Compile<TDelegate>(string code, params string[] parameterNames)
        where TDelegate : Delegate
        => AlderEval.GetEngine().Compile<TDelegate>(code, parameterNames);

    /// <summary>
    /// Compiles a code body with named parameters into a native <typeparamref name="TDelegate"/> delegate
    /// using the global <see cref="AlderEval"/> engine.
    /// </summary>
    public static TDelegate Compile<TDelegate>(string code, IEnumerable<string> parameterNames)
        where TDelegate : Delegate
        => AlderEval.GetEngine().Compile<TDelegate>(code, parameterNames);

    /// <summary>
    /// Compiles an expression and returns an <see cref="AlderCompiledExpression{T}"/> bound to the global engine.
    /// </summary>
    public static AlderCompiledExpression<T> Compile<T>(string code)
        => AlderEval.GetEngine().Compile<T>(code);

    /// <summary>
    /// Compiles an expression and returns a <see cref="Func{T}"/> backed by the global engine state.
    /// </summary>
    public static Func<T?> CompileToFunc<T>(string code)
        => AlderEval.GetEngine().CompileToFunc<T>(code);
}
