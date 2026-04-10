namespace Alder.Compiled;

/// <summary>
/// Provides <c>Compile&lt;TDelegate&gt;</c> methods on the global <see cref="AlderEval"/> engine.
/// Requires <c>AlderEval.Configure(o => o.UseCompiler())</c> before use.
/// </summary>
public static class AlderEvalCompileExtensions
{
    /// <summary>
    /// Compiles a code body with named parameters into a native <typeparamref name="TDelegate"/> delegate
    /// using the global <see cref="AlderEval"/> engine.
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
    /// Compiles an expression and returns an <see cref="AlderCompiledExpression{T}"/> using the global engine.
    /// </summary>
    public static AlderCompiledExpression<T> Compile<T>(string code)
        => AlderEval.GetEngine().Compile<T>(code);

    /// <summary>
    /// Compiles an expression and returns a <see cref="Func{T}"/> for zero-overhead invocation using the global engine.
    /// </summary>
    public static Func<T?> CompileToFunc<T>(string code)
        => AlderEval.GetEngine().CompileToFunc<T>(code);
}
