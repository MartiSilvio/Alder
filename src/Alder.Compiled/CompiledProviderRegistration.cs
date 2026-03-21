using Alder.Compilation;
using Alder.Compiled.Compilation;
using Alder.Binding;
using Alder.Parsing;

namespace Alder.Compiled;

internal sealed class CompiledProvider : ICompiledProvider
{
    internal static readonly CompiledProvider Instance = new();

    private CompiledProvider() { }

    public CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, AlderOptions? options = null)
        => ILExpressionCompiler.GetOrCompile(expressionText, ast, cache, options);

    public CompiledExpressionInfo TryCompile(Expr ast, AlderOptions? options = null)
        => ILExpressionCompiler.TryCompile(ast, options);

    public CompiledExpressionInfo TryCompile(BoundExpr bound, AlderOptions? options = null)
        => ILExpressionCompiler.TryCompile(bound, options);
}
