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

        if (node.ReturnTypeName != null)
        {
            return LinqExpression.Call(
                CreateIteratorLambdaValueMethod,
                parameters,
                LinqExpression.Constant(node.Body, typeof(Expr)),
                ctx.ContextParam,
                LinqExpression.Constant(node.ReturnTypeName));
        }

        return LinqExpression.Call(
            CreateLambdaValueMethod,
            parameters,
            LinqExpression.Constant(node.Body, typeof(Expr)),
            ctx.ContextParam);
    }
}
