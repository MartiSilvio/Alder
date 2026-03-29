using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class UsingEvaluator
{
    public static object? Evaluate(BoundUsingStatementExpr node, EvaluationContext ctx)
    {
        var resource = ctx.Evaluate(node.Resource);
        try
        {
            return ctx.Evaluate(node.Body);
        }
        finally
        {
            if (resource is IDisposable disposable)
                disposable.Dispose();
            else if (resource is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
