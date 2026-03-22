namespace Alder.Compiled;

public static class AlderCompiledExtensions
{
    public static AlderOptions UseCompiler(this AlderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Compiler = CompiledProvider.Instance;
        return options;
    }

    public static AlderOptions UseCompiler(this AlderOptions options, IExpressionCompiler expressionCompiler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expressionCompiler);
        options.Compiler = CompiledProvider.Instance;
        options.ExpressionCompiler = expressionCompiler;
        return options;
    }
}
