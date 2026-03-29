using Alder.Binding.BoundNodes;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IncrementDecrementEmitter : INodeEmitter<BoundIncrementDecrementExpr>
{
    public LinqExpression Emit(BoundIncrementDecrementExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            if (TryEmitPure(node, promoted, ctx, out var pureResult))
                return pureResult;

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
                        ctx.ConfigParam,
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
            ctx.ConfigParam,
            LinqExpression.Constant(ctx.IsChecked));
    }

    private static bool TryEmitPure(
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
        var one = LinqExpression.Constant(Convert.ChangeType(1, promoted.VariableType), promoted.VariableType);
        var newValue = isIncrement
            ? LinqExpression.Add(promoted.Variable, one)
            : LinqExpression.Subtract(promoted.Variable, one);

        if (node.IsPrefix)
        {
            result = LinqExpression.Block(
                promoted.Variable.Type,
                LinqExpression.Assign(promoted.Variable, newValue),
                promoted.Variable);
        }
        else
        {
            var oldVar = LinqExpression.Variable(promoted.Variable.Type, "incrOld");
            result = LinqExpression.Block(
                promoted.Variable.Type,
                [oldVar],
                LinqExpression.Assign(oldVar, promoted.Variable),
                LinqExpression.Assign(promoted.Variable, newValue),
                oldVar);
        }
        return true;
    }
}
