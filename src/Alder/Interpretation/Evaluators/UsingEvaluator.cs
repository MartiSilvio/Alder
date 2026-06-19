using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.UsingStatement)]
internal static class UsingEvaluator
{
    public static object? Evaluate(BoundUsingStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        try
        {
            var resource = ctx.Evaluate(node.Resource, ct);
            try
            {
                return ctx.Evaluate(node.Body, ct);
            }
            finally
            {
                ExecutionRuntime.DisposeResource(resource);
            }
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }

    public static async ValueTask<object?> EvaluateAsync(BoundUsingStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var previousContext = ctx.Context;
        ctx.Context = ctx.Context.CreateChild();
        try
        {
            var resource = await ctx.EvaluateAsync(node.Resource, ct);
            try
            {
                return await ctx.EvaluateAsync(node.Body, ct);
            }
            finally
            {
                await ExecutionRuntime.DisposeResourceAsync(resource);
            }
        }
        finally
        {
            ctx.Context = previousContext;
        }
    }
}
