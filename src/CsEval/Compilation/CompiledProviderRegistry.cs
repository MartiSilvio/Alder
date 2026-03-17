using CsEval.Binding;
using CsEval.Parsing;

namespace CsEval.Compilation;

internal interface ICompiledProvider
{
    CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null);
    CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null);
    CompiledExpressionInfo TryCompile(BoundExpr bound, CsEvalOptions? options = null);
}
