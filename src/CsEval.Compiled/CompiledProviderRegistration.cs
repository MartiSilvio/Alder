using CsEval.Compilation;
using CsEval.Compiled.Compilation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled;

internal sealed class CompiledProvider : ICompiledProvider
{
    public CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null)
        => ILExpressionCompiler.GetOrCompile(expressionText, ast, cache, options);

    public CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null, CsEvalContext? typeHintContext = null)
        => ILExpressionCompiler.TryCompile(ast, options, typeHintContext);
}

internal static class CompiledProviderRegistration
{
    private static readonly CompiledProvider Provider = new();

    internal static void Register()
    {
        CompiledProviderRegistry.Register(Provider);
    }
}
