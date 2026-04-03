using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Conversion)]
internal static class CastEvaluator
{
    public static object? Evaluate(BoundCastExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return TypeHelpers.ExplicitCast(value, node.TargetType, node.SourceStaticType, ctx.IsChecked);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundCastExpr node, EvaluationContext ctx)
    {
        var value = await ctx.EvaluateAsync(node.Expression);
        return TypeHelpers.ExplicitCast(value, node.TargetType, node.SourceStaticType, ctx.IsChecked);
    }
}
