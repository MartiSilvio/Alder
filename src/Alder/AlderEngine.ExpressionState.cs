using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Runtime;
using Binder = Alder.Binding.Binder;

namespace Alder;

public sealed partial class AlderEngine
{
    private readonly ConditionalWeakTable<AlderExpression, ExpressionRuntimeState> _expressionRuntimeStates = new();

    private ExpressionRuntimeState GetExpressionState(AlderExpression expression)
        => _expressionRuntimeStates.GetValue(expression, static _ => new ExpressionRuntimeState());

    internal CompiledExpressionInfo? GetCompiledInfo(AlderExpression expression)
        => GetExpressionState(expression).CompiledInfo;

    internal bool HasCompiledDelegate(AlderExpression expression)
        => GetCompiledInfo(expression)?.Delegate != null;

    internal bool? GetIsCompilable(AlderExpression expression)
        => GetCompiledInfo(expression)?.IsCompilable;

    internal string? GetCompilationFailureReason(AlderExpression expression)
        => GetCompiledInfo(expression)?.FailureReason;

    internal int GetBoundExecutionCount(AlderExpression expression)
        => GetExpressionState(expression).BoundExecutionCount;

    internal int GetBoundFallbackCount(AlderExpression expression)
        => GetExpressionState(expression).BoundFallbackCount;

    internal string? GetLastBoundFallbackReason(AlderExpression expression)
        => GetExpressionState(expression).LastBoundFallbackReason;

    private void SetCompiledInfo(AlderExpression expression, CompiledExpressionInfo? info)
        => GetExpressionState(expression).CompiledInfo = info;

    private BoundExpr GetOrCreateBoundExpression(AlderExpression expression, AlderContext context)
    {
        var state = GetExpressionState(expression);
        var currentVersion = context.GetTypeInferenceVersion();
        if (state.TryGetCachedBoundExpression(context, currentVersion, out var cached) && cached != null)
            return cached;

        var sourceText = new Text.SourceText(expression.Source);
        var bindingContext = new BindingContext(context);
        var binder = new Binder(sourceText);

        BoundExpr bound;
        try
        {
            bound = binder.Bind(expression.Ast, bindingContext);
        }
        catch (AlderException)
        {
            var recoveringBinder = new Binder(sourceText);
            bound = recoveringBinder.BindRecovering(expression.Ast, bindingContext);
            var allDiagnostics = recoveringBinder.GetAccumulatedDiagnostics();
            if (allDiagnostics.Count > 0)
            {
                var ex = new AlderException(DiagnosticDescriptors.BindingFailed, allDiagnostics[0].Message);
                ex.SetDiagnostics(allDiagnostics);
                throw ex;
            }

            throw;
        }

        if (bound.HasErrors)
        {
            var allDiagnostics = CollectTreeDiagnostics(bound);
            var ex = new AlderException(
                DiagnosticDescriptors.BindingFailed,
                allDiagnostics.Count > 0 ? allDiagnostics[0].Message : "Expression has binding errors");
            if (allDiagnostics.Count > 0)
                ex.SetDiagnostics(allDiagnostics);
            throw ex;
        }

        state.CacheBoundExpression(context, currentVersion, bound);
        return bound;
    }

    private bool TryGetOrCreateBoundExpression(
        AlderExpression expression,
        AlderContext context,
        out BoundExpr? bound,
        out string? failureReason)
    {
        var state = GetExpressionState(expression);
        if (state.TryGetBindingUnavailableReason(out failureReason))
        {
            bound = null;
            return false;
        }

        try
        {
            bound = GetOrCreateBoundExpression(expression, context);
            failureReason = null;
            return true;
        }
        catch (BindingNotSupportedException ex)
        {
            state.RecordBindingUnavailable(ex.Message);
            bound = null;
            failureReason = ex.Message;
            return false;
        }
    }

    private void RecordBoundExecution(AlderExpression expression)
        => GetExpressionState(expression).RecordBoundExecution();

    private void RecordBoundFallback(AlderExpression expression, string? reason)
        => GetExpressionState(expression).RecordBoundFallback(reason);

    private static IReadOnlyList<AlderDiagnostic> CollectTreeDiagnostics(BoundExpr root)
    {
        var collector = new DiagnosticCollector();
        collector.Walk(root);
        return collector.Diagnostics;
    }
}
