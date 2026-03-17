namespace CsEval.Compiled;

public static class CsEvalCompiledExtensions
{
    public static CsEvalOptions UseCompiler(this CsEvalOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options with { Compiler = CompiledProvider.Instance };
    }

    public static CsEvalOptions UseCompiler(this CsEvalOptions options, IExpressionCompiler expressionCompiler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expressionCompiler);
        return options with { Compiler = CompiledProvider.Instance, ExpressionCompiler = expressionCompiler };
    }
}
