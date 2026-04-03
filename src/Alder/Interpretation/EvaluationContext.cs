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
    public AlderContext Context
    {
        get => _contextRef;
        set => _contextRef = value;
    }

    public AlderConfig Config { get; }
    public ExecutionConstraintState? ConstraintState { get; }
    public CancellationToken CancellationToken { get; }
    public Stack<Exception> CaughtExceptions { get; }

    public EvaluationTracer? Tracer { get; set; }
    public SourceText? SourceText { get; set; }
    public int BreakContextDepth { get; set; }
    public int LoopDepth { get; set; }
    public bool IsChecked { get; set; }

    private AlderContext _contextRef;

    internal EvaluationContext(
        AlderContext context,
        AlderConfig config,
        ExecutionConstraintState? constraintState,
        CancellationToken cancellationToken,
        Stack<Exception> caughtExceptions)
    {
        _contextRef = context;
        Config = config;
        ConstraintState = constraintState;
        CancellationToken = cancellationToken;
        CaughtExceptions = caughtExceptions;
    }

    internal BoundExpr? LastEvaluatedExpr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Evaluate(BoundExpr expr)
    {
        if (Tracer != null)
            return EvaluateTraced(expr);

        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        var result = Dispatch(expr);
        LastEvaluatedExpr = saved;
        return result;
    }

    private object? EvaluateTraced(BoundExpr expr)
    {
        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        Tracer!.Push(expr);
        object? result;
        try
        {
            result = Dispatch(expr);
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

    public async ValueTask<object?> EvaluateAsync(BoundExpr expr)
    {
        if (Tracer != null)
            return await EvaluateTracedAsync(expr);

        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        var result = await DispatchAsync(expr);
        LastEvaluatedExpr = saved;
        return result;
    }

    private async ValueTask<object?> EvaluateTracedAsync(BoundExpr expr)
    {
        var saved = LastEvaluatedExpr;
        if (!expr.Span.IsEmpty)
            LastEvaluatedExpr = expr;

        Tracer!.Push(expr);
        object? result;
        try
        {
            result = await DispatchAsync(expr);
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

    public object? MatchPattern(object? value, Pattern pattern)
        => PatternRuntime.MatchPattern(value, pattern, Context, Config, CancellationToken);
}
