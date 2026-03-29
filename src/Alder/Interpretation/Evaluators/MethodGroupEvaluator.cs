using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class MethodGroupEvaluator : INodeEvaluator<BoundMethodGroupExpr>
{
    public object? Evaluate(BoundMethodGroupExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx);

        if (node.IsStatic) return new StaticMethodRef(node.DeclaringType, node.MethodName);
        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", node.MethodName);
        return new MethodRef(target, node.MethodName);
    }
}
