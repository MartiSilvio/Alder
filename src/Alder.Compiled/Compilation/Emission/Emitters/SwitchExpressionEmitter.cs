using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.SwitchExpression)]
internal static class SwitchExpressionEmitter
{
    public static LinqExpression Emit(BoundSwitchExpressionExpr node, EmissionContext ctx)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var resultVar = LinqExpression.Variable(typeof(object), "switchExprResult");
        var doneLabel = LinqExpression.Label("switchExprDone");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, ctx.EmitBoxed(node.Expression))
        };

        for (var i = 0; i < node.Arms.Length; i++)
        {
            var arm = node.Arms[i];
            var previousContextVar = LinqExpression.Variable(typeof(AlderContext), $"switchArmPrevCtx{i}");
            var armCondition = (LinqExpression)LinqExpression.Call(
                MatchPatternMethod,
                valueVar,
                LinqExpression.Constant(arm.Pattern, typeof(Pattern)),
                ctx.ContextParam,
                ctx.CancellationTokenParam);

            if (arm.WhenGuard != null)
            {
                armCondition = LinqExpression.AndAlso(
                    armCondition,
                    LinqExpression.Call(RequireBooleanMethod, ctx.EmitBoxed(arm.WhenGuard)));
            }

            statements.Add(
                LinqExpression.Block(
                    typeof(void),
                    [previousContextVar],
                    LinqExpression.Assign(previousContextVar, ctx.ContextParam),
                    LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
                    LinqExpression.TryFinally(
                        LinqExpression.IfThen(
                            armCondition,
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, ctx.EmitBoxed(arm.Value)),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Assign(ctx.ContextParam, previousContextVar))));
        }

        statements.Add(
            LinqExpression.Throw(
                LinqExpression.New(
                    AlderExceptionCtor,
                    LinqExpression.Field(null, SwitchExpressionNonExhaustiveDescriptor),
                    LinqExpression.NewArrayInit(typeof(object),
                        LinqExpression.Coalesce(valueVar, LinqExpression.Constant("null", typeof(object))))),
                typeof(void)));
        statements.Add(LinqExpression.Label(doneLabel));
        statements.Add(resultVar);

        return LinqExpression.Block(typeof(object), [valueVar, resultVar], statements);
    }
}
