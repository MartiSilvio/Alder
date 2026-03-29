using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal sealed class LockEvaluator : INodeEvaluator<BoundLockStatementExpr>
{
    public object? Evaluate(BoundLockStatementExpr node, EvaluationContext ctx)
    {
        var lockObject = ctx.Evaluate(node.LockObject);
        if (lockObject == null)
            throw new AlderException(DiagnosticDescriptors.LockRequiresNonNull);

        lock (lockObject)
        {
            return ctx.Evaluate(node.Body);
        }
    }
}
