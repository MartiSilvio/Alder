using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Alder.Binding;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed partial class EvaluationContext
{
    public AlderContext Context { get; set; }

    public ExecutionConstraintState? ConstraintState { get; }
    public Stack<Exception> CaughtExceptions { get; }

    public EvaluationTracer? Tracer { get; set; }
    public SourceText? SourceText { get; set; }
    public int BreakContextDepth { get; set; }
    public int LoopDepth { get; set; }
    public bool IsChecked { get; set; }
    public Func<object?, bool>? YieldCallback { get; set; }

    internal EvaluationContext(
        AlderContext context,
        ExecutionConstraintState? constraintState,
        Stack<Exception> caughtExceptions)
    {
        Context = context;
        ConstraintState = constraintState;
        CaughtExceptions = caughtExceptions;
    }

    internal BoundExpr? LastEvaluatedExpr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Evaluate(BoundExpr expr, CancellationToken ct)
    {
        if (Tracer != null)
            return EvaluateTraced(expr, ct);

        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        var result = Dispatch(expr, ct);
        LastEvaluatedExpr = saved;
        return result;
    }

    private object? EvaluateTraced(BoundExpr expr, CancellationToken ct)
    {
        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        Tracer!.Push(expr);
        object? result;
        try
        {
            result = Dispatch(expr, ct);
        }
        catch (Exception ex)
        {
            Tracer.PopError(ex);
            throw;
        }

        Tracer.Pop(result);
        LastEvaluatedExpr = saved;
        return result;
    }

    public async ValueTask<object?> EvaluateAsync(BoundExpr expr, CancellationToken ct)
    {
        if (Tracer != null)
            return await EvaluateTracedAsync(expr, ct);

        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        var result = await DispatchAsync(expr, ct);
        LastEvaluatedExpr = saved;
        return result;
    }

    private async ValueTask<object?> EvaluateTracedAsync(BoundExpr expr, CancellationToken ct)
    {
        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        Tracer!.Push(expr);
        object? result;
        try
        {
            result = await DispatchAsync(expr, ct);
        }
        catch (Exception ex)
        {
            Tracer.PopError(ex);
            throw;
        }

        Tracer.Pop(result);
        LastEvaluatedExpr = saved;
        return result;
    }

    public object? MatchPattern(object? value, Pattern pattern, CancellationToken ct)
        => PatternRuntime.MatchPattern(value, pattern, Context, ct);
}
