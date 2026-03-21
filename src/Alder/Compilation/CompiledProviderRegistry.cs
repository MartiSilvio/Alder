using Alder.Binding;
using Alder.Parsing;

namespace Alder.Compilation;

internal interface ICompiledProvider
{
    CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, AlderOptions? options = null);
    CompiledExpressionInfo TryCompile(Expr ast, AlderOptions? options = null);
    CompiledExpressionInfo TryCompile(BoundExpr bound, AlderOptions? options = null);
}
