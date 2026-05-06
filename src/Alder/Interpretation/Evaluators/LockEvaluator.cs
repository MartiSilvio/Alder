using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.LockStatement)]
internal static class LockEvaluator
{
    public static object? Evaluate(BoundLockStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var lockObject = ctx.Evaluate(node.LockObject, ct);
        if (lockObject == null)
            throw new AlderException(DiagnosticDescriptors.LockRequiresNonNull);

        lock (lockObject)
        {
            return ctx.Evaluate(node.Body, ct);
        }
    }

    public static async ValueTask<object?> EvaluateAsync(BoundLockStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var lockObject = await ctx.EvaluateAsync(node.LockObject, ct);
        if (lockObject == null)
            throw new AlderException(DiagnosticDescriptors.LockRequiresNonNull);

        lock (lockObject)
        {
            // §12.9.8.1: an await_expression shall not occur inside the block of a lock_statement
            return ctx.Evaluate(node.Body, ct);
        }
    }
}
