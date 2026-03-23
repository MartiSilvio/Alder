using Alder.Binding;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compilation;

internal interface ICompiledProvider
{
    CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, AlderConfig config);
    CompiledExpressionInfo TryCompile(Expr ast, AlderConfig config);
    CompiledExpressionInfo TryCompile(BoundExpr bound, AlderConfig config);
}
