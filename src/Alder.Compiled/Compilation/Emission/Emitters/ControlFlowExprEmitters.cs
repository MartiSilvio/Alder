using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.CheckedExpression)]
internal static class CheckedEmitter
{
    public static LinqExpression Emit(BoundCheckedExpr node, EmissionContext ctx)
    {
        var previous = ctx.IsChecked;
        ctx.IsChecked = node.IsChecked;
        try
        {
            return ctx.Emit(node.Expression);
        }
        finally
        {
            ctx.IsChecked = previous;
        }
    }
}

[EmitsNode(BoundNodeKind.ChainedComparisonOperator)]
internal static class ChainedComparisonEmitter
{
    public static LinqExpression Emit(BoundChainedComparisonExpr node, EmissionContext ctx)
    {
        var resultLabel = LinqExpression.Label(typeof(bool), "chainResult");
        var variables = new List<ParameterExpression>();
        var body = new List<LinqExpression>();

        var firstValue = LinqExpression.Variable(typeof(object), "v0");
        variables.Add(firstValue);
        body.Add(LinqExpression.Assign(firstValue, ctx.EmitBoxed(node.Operands[0])));

        for (var i = 0; i < node.Operators.Length; i++)
        {
            var nextValue = LinqExpression.Variable(typeof(object), $"v{i + 1}");
            variables.Add(nextValue);
            body.Add(LinqExpression.Assign(nextValue, ctx.EmitBoxed(node.Operands[i + 1])));

            var comparison = LinqExpression.Call(
                PerformComparisonMethod,
                variables[i],
                nextValue,
                LinqExpression.Constant(node.Operators[i]),
                LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison)));

            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(comparison),
                LinqExpression.Return(resultLabel, LinqExpression.Constant(false))));
        }

        body.Add(LinqExpression.Label(resultLabel, LinqExpression.Constant(true)));
        return LinqExpression.Block(typeof(bool), variables, body);
    }
}

[EmitsNode(BoundNodeKind.RangeExpression)]
internal static class RangeEmitter
{
    public static LinqExpression Emit(BoundRangeExpr node, EmissionContext ctx)
    {
        var startExpr = node.Start != null
            ? ctx.EmitBoxed(node.Start)
            : LinqExpression.Constant(null, typeof(object));
        var endExpr = node.End != null
            ? ctx.EmitBoxed(node.End)
            : LinqExpression.Constant(null, typeof(object));
        var rangeExpr = LinqExpression.Call(CreateSystemRangeMethod, startExpr, endExpr);

        if (!node.ExclusiveEnd)
        {
            return LinqExpression.New(
                typeof(InclusiveRange).GetConstructor([typeof(Range)])!,
                rangeExpr);
        }

        return rangeExpr;
    }
}

[EmitsNode(BoundNodeKind.FromEndIndexExpression)]
internal static class IndexFromEndEmitter
{
    public static LinqExpression Emit(BoundIndexFromEndExpr node, EmissionContext ctx)
    {
        var operand = ctx.Emit(node.Operand);
        var intOperand = operand.Type == typeof(int) ? operand : LinqExpression.Convert(operand, typeof(int));
        return LinqExpression.New(
            typeof(Index).GetConstructor([typeof(int), typeof(bool)])!,
            intOperand,
            LinqExpression.Constant(true));
    }
}

[EmitsNode(BoundNodeKind.SliceExpression)]
internal static class SliceEmitter
{
    public static LinqExpression Emit(BoundSliceExpr node, EmissionContext ctx)
    {
        var target = ctx.EmitBoxed(node.Target);
        var start = node.Start != null ? ctx.EmitBoxed(node.Start) : LinqExpression.Constant(null, typeof(object));
        var end = node.End != null ? ctx.EmitBoxed(node.End) : LinqExpression.Constant(null, typeof(object));

        if (node.Step != null)
        {
            return LinqExpression.Call(
                GetSliceStepMethod,
                target, start, end,
                ctx.EmitBoxed(node.Step));
        }

        return LinqExpression.Call(GetSliceMethod, target, start, end);
    }
}
