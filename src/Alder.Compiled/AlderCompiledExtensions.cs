namespace Alder.Compiled;

public static class AlderCompiledExtensions
{
    public static AlderOptions UseCompiler(this AlderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options with { Compiler = CompiledProvider.Instance };
    }

    public static AlderOptions UseCompiler(this AlderOptions options, IExpressionCompiler expressionCompiler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expressionCompiler);
        return options with { Compiler = CompiledProvider.Instance, ExpressionCompiler = expressionCompiler };
    }
}
