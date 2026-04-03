using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MethodGroup)]
internal static class MethodGroupEvaluator
{
    public static object? Evaluate(BoundMethodGroupExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx, ct);

        if (node.IsStatic) return new StaticMethodRef(node.DeclaringType, node.MethodName);
        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", node.MethodName);
        return new MethodRef(target, node.MethodName);
    }
}
