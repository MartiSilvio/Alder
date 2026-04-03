using Alder.Binding.BoundNodes;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class LambdaEmitter : INodeEmitter<BoundLambdaExpr>
{
    public LinqExpression Emit(BoundLambdaExpr node, EmissionContext ctx)
    {
        var parameters = LinqExpression.NewArrayInit(
            typeof(string),
            node.Parameters.Select(static name => LinqExpression.Constant(name)));

        return LinqExpression.Call(
            CreateLambdaValueMethod,
            parameters,
            LinqExpression.Constant(node.Body, typeof(Expr)),
            ctx.ContextParam);
    }
}
