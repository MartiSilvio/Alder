using Alder.Binding;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compilation;

internal interface ICompiledProvider
{
    CompiledExpressionInfo TryCompile(Expr ast, AlderConfig config);
    CompiledExpressionInfo TryCompile(BoundExpr bound, AlderConfig config);
}
