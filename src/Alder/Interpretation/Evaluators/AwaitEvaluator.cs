using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.AwaitExpression)]
internal static class AwaitEvaluator
{
    public static async ValueTask<object?> EvaluateAsync(BoundAwaitExpr node, EvaluationContext ctx)
    {
        var operand = await ctx.EvaluateAsync(node.Operand);

        if (operand == null)
            throw new AlderException(DiagnosticDescriptors.NotAwaitable, "null");

        return await TaskUnwrapper.AwaitDynamic(operand);
    }
}
