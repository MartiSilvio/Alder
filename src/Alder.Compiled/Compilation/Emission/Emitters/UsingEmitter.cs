using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class UsingEmitter : INodeEmitter<BoundUsingStatementExpr>
{
    public LinqExpression Emit(BoundUsingStatementExpr node, EmissionContext ctx)
    {
        var resourceVar = LinqExpression.Variable(typeof(object), "usingResource");
        var resultVar = LinqExpression.Variable(typeof(object), "usingResult");

        return LinqExpression.Block(
            typeof(object),
            [resourceVar, resultVar],
            LinqExpression.Assign(resourceVar, ctx.EmitBoxed(node.Resource)),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.Body)),
                LinqExpression.Call(DisposeResourceMethod, resourceVar)),
            resultVar);
    }
}
