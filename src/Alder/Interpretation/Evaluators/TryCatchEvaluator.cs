using System.Runtime.ExceptionServices;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.TryStatement)]
internal static class TryCatchEvaluator
{
    public static object? Evaluate(BoundTryCatchFinallyExpr node, EvaluationContext ctx)
    {
        object? result = null;
        Exception? unhandledException = null;
        ControlFlowSignal? pendingSignal = null;

        try
        {
            foreach (var statement in node.TryBody)
            {
                result = ctx.Evaluate(statement);
                if (result is ControlFlowSignal signal)
                {
                    pendingSignal = signal;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            var (handled, catchResult, catchSignal) = TryMatchCatchClause(node.CatchClauses, ex, ctx);
            if (handled)
            {
                result = catchResult;
                pendingSignal = catchSignal;
            }
            else
            {
                unhandledException = ex;
            }
        }
        finally
        {
            foreach (var statement in node.FinallyBody)
            {
                ctx.Evaluate(statement);
            }
        }

        if (unhandledException != null)
            ExceptionDispatchInfo.Capture(unhandledException).Throw();

        if (pendingSignal != null)
            return pendingSignal;

        return result;
    }

    private static (bool Handled, object? Result, ControlFlowSignal? Signal) TryMatchCatchClause(
        IReadOnlyList<BoundCatchClause> catchClauses,
        Exception ex,
        EvaluationContext ctx)
    {
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.ExceptionTypeName != null)
            {
                var catchType = ctx.Context.TypeResolver.ResolveType(catchClause.ExceptionTypeName);
                if (!catchType.IsInstanceOfType(ex))
                    continue;
            }

            var previousContext = ctx.Context;
            ctx.Context = ctx.Context.CreateChild();
            try
            {
                if (catchClause.VariableName != null)
                    ctx.Context.DefineNew(catchClause.VariableName, ex, ex.GetType());

                if (catchClause.WhenGuard != null)
                {
                    bool guardMatched;
                    try
                    {
                        var guardResult = ctx.Evaluate(catchClause.WhenGuard);
                        guardMatched = TypeHelpers.RequireBoolean(guardResult);
                    }
                    catch
                    {
                        guardMatched = false;
                    }

                    if (!guardMatched)
                        continue;
                }

                ctx.CaughtExceptions.Push(ex);
                try
                {
                    object? result = null;
                    ControlFlowSignal? signal = null;
                    foreach (var statement in catchClause.Body)
                    {
                        result = ctx.Evaluate(statement);
                        if (result is ControlFlowSignal controlFlowSignal)
                        {
                            signal = controlFlowSignal;
                            break;
                        }
                    }

                    return (true, result, signal);
                }
                finally
                {
                    ctx.CaughtExceptions.Pop();
                }
            }
            finally
            {
                ctx.Context = previousContext;
            }
        }

        return (false, null, null);
    }
}
