using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class CheckedEmitter : INodeEmitter<BoundCheckedExpr>
{
    private readonly Action<bool> _setChecked;
    private readonly Func<bool> _getChecked;

    internal CheckedEmitter(Func<bool> getChecked, Action<bool> setChecked)
    {
        _getChecked = getChecked;
        _setChecked = setChecked;
    }

    public Expression Emit(BoundCheckedExpr node, EmissionContext ctx)
    {
        var previous = _getChecked();
        _setChecked(node.IsChecked);
        try
        {
            return ctx.Emit(node.Expression);
        }
        finally
        {
            _setChecked(previous);
        }
    }
}

internal sealed class ChainedComparisonEmitter : INodeEmitter<BoundChainedComparisonExpr>
{
    public Expression Emit(BoundChainedComparisonExpr node, EmissionContext ctx)
    {
        var resultLabel = LinqExpression.Label(typeof(bool), "chainResult");
        var variables = new List<ParameterExpression>();
        var body = new List<LinqExpression>();

        var firstValue = LinqExpression.Variable(typeof(object), "v0");
        variables.Add(firstValue);
        body.Add(LinqExpression.Assign(firstValue, EmitHelpers.AsObject(ctx.Emit(node.Operands[0]))));

        for (var i = 0; i < node.Operators.Length; i++)
        {
            var nextValue = LinqExpression.Variable(typeof(object), $"v{i + 1}");
            variables.Add(nextValue);
            body.Add(LinqExpression.Assign(nextValue, EmitHelpers.AsObject(ctx.Emit(node.Operands[i + 1]))));

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

internal sealed class RangeEmitter : INodeEmitter<BoundRangeExpr>
{
    public Expression Emit(BoundRangeExpr node, EmissionContext ctx)
    {
        var startExpr = node.Start != null
            ? EmitHelpers.AsObject(ctx.Emit(node.Start))
            : LinqExpression.Constant(null, typeof(object));
        var endExpr = node.End != null
            ? EmitHelpers.AsObject(ctx.Emit(node.End))
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

internal sealed class IndexFromEndEmitter : INodeEmitter<BoundIndexFromEndExpr>
{
    public Expression Emit(BoundIndexFromEndExpr node, EmissionContext ctx)
    {
        var operand = ctx.Emit(node.Operand);
        var intOperand = operand.Type == typeof(int) ? operand : LinqExpression.Convert(operand, typeof(int));
        return LinqExpression.New(
            typeof(Index).GetConstructor([typeof(int), typeof(bool)])!,
            intOperand,
            LinqExpression.Constant(true));
    }
}

internal sealed class SliceEmitter : INodeEmitter<BoundSliceExpr>
{
    public Expression Emit(BoundSliceExpr node, EmissionContext ctx)
    {
        var target = EmitHelpers.AsObject(ctx.Emit(node.Target));
        var start = node.Start != null ? EmitHelpers.AsObject(ctx.Emit(node.Start)) : LinqExpression.Constant(null, typeof(object));
        var end = node.End != null ? EmitHelpers.AsObject(ctx.Emit(node.End)) : LinqExpression.Constant(null, typeof(object));

        if (node.Step != null)
        {
            return LinqExpression.Call(
                GetSliceStepMethod,
                target, start, end,
                EmitHelpers.AsObject(ctx.Emit(node.Step)));
        }

        return LinqExpression.Call(GetSliceMethod, target, start, end);
    }
}
