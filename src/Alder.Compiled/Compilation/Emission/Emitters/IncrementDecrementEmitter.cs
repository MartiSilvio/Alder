using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.IncrementOperator)]
internal static class IncrementDecrementEmitter
{
    public static LinqExpression Emit(BoundIncrementDecrementExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            if (TryEmitNative(node, promoted, ctx, out var nativeResult))
                return nativeResult;

            var isIncrement = node.Operator == TokenType.PlusPlus;
            var oldVar = LinqExpression.Variable(promoted.Variable.Type, "incrOld");
            var newVar = LinqExpression.Variable(typeof(object), "incrNew");
            return LinqExpression.Block(
                promoted.Variable.Type,
                [oldVar, newVar],
                LinqExpression.Assign(oldVar, promoted.Variable),
                LinqExpression.Assign(
                    newVar,
                    LinqExpression.Call(
                        ApplyIncrementDecrementLocalMethod,
                        LinqExpression.Constant(node.Name),
                        EmitHelpers.AsObject(promoted.Variable),
                        LinqExpression.Constant(isIncrement),
                        LinqExpression.Constant(promoted.Variable.Type, typeof(Type)),
                        ctx.ContextParam,
                        LinqExpression.Constant(ctx.IsChecked))),
                LinqExpression.Assign(promoted.Variable,
                    EmitHelpers.EnsureTypedExpression(newVar, promoted.VariableType)),
                node.IsPrefix ? promoted.Variable : oldVar);
        }

        return LinqExpression.Call(
            ApplyIncrementDecrementMethod,
            LinqExpression.Constant(node.Name),
            LinqExpression.Constant(node.Operator == TokenType.PlusPlus),
            LinqExpression.Constant(node.IsPrefix),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }

    private static bool TryEmitNative(
        BoundIncrementDecrementExpr node,
        PromotedLocal promoted,
        EmissionContext ctx,
        out LinqExpression result)
    {
        result = null!;
        if (ctx.IsChecked || promoted.VariableType == typeof(object) || promoted.VariableType.IsEnum)
            return false;

        if (!CompoundAssignEmitter.IsAddSubtractSafeType(promoted.VariableType))
            return false;

        var isIncrement = node.Operator == TokenType.PlusPlus;

        result = (node.IsPrefix, isIncrement) switch
        {
            (true, true) => LinqExpression.PreIncrementAssign(promoted.Variable),
            (true, false) => LinqExpression.PreDecrementAssign(promoted.Variable),
            (false, true) => LinqExpression.PostIncrementAssign(promoted.Variable),
            (false, false) => LinqExpression.PostDecrementAssign(promoted.Variable),
        };

        return true;
    }
}
