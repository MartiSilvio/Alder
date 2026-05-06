using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.UsingStatement)]
internal static class UsingEvaluator
{
    public static object? Evaluate(BoundUsingStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var resource = ctx.Evaluate(node.Resource, ct);
        try
        {
            return ctx.Evaluate(node.Body, ct);
        }
        finally
        {
            if (resource is IDisposable disposable)
                disposable.Dispose();
            else if (resource is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public static async ValueTask<object?> EvaluateAsync(BoundUsingStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var resource = await ctx.EvaluateAsync(node.Resource, ct);
        try
        {
            return await ctx.EvaluateAsync(node.Body, ct);
        }
        finally
        {
            if (resource is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (resource is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
