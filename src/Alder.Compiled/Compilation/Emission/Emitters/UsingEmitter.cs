using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.UsingStatement)]
internal static class UsingEmitter
{
    public static LinqExpression Emit(BoundUsingStatementExpr node, EmissionContext ctx)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "usingPrevCtx");
        var resourceVar = LinqExpression.Variable(typeof(object), "usingResource");
        var resultVar = LinqExpression.Variable(typeof(object), "usingResult");

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resourceVar, resultVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(
                    LinqExpression.Assign(resourceVar, ctx.EmitBoxed(node.Resource)),
                    LinqExpression.TryFinally(
                        LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.Body)),
                        LinqExpression.Call(DisposeResourceMethod, resourceVar))),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }
}
