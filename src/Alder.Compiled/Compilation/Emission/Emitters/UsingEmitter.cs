using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.UsingStatement)]
internal static class UsingEmitter
{
    public static LinqExpression Emit(BoundUsingStatementExpr node, EmissionContext ctx)
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
