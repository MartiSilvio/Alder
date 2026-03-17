using CsEval.Compilation;
using CsEval.Compiled.Compilation;
using CsEval.Binding;
using CsEval.Parsing;

namespace CsEval.Compiled;

internal sealed class CompiledProvider : ICompiledProvider
{
    internal static readonly CompiledProvider Instance = new();

    private CompiledProvider() { }

    public CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null)
        => ILExpressionCompiler.GetOrCompile(expressionText, ast, cache, options);

    public CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null)
        => ILExpressionCompiler.TryCompile(ast, options);

    public CompiledExpressionInfo TryCompile(BoundExpr bound, CsEvalOptions? options = null)
        => ILExpressionCompiler.TryCompile(bound, options);
}
