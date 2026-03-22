using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;

namespace Alder.Runtime.Semantics;

internal static class ExecutionRuntime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureMemberTargetNotNull(object? target, string memberName)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", memberName);
        return target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureCallTargetNotNull(object? target, string methodName)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, methodName);
        return target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureIndexTargetNotNull(object? target)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);
        return target;
    }

    public static void CheckNullCoalesceAssignAllowed(string name, AlderContext context)
    {
        if (context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.QuestionQuestionEqual),
                varType.Name,
                varType.Name);
    }

    public static void DisposeResource(object? resource)
    {
        if (resource is IDisposable disposable)
            disposable.Dispose();
        else if (resource is IAsyncDisposable asyncDisposable)
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public static object ValidateLockObject(object? lockObj)
    {
        if (lockObj == null)
            throw new AlderException(DiagnosticDescriptors.LockRequiresNonNull);

        return lockObj;
    }

    public static Exception ValidateThrowOperand(object? value)
    {
        if (value is Exception exception)
            return exception;

        throw new AlderException(DiagnosticDescriptors.ThrowExpressionMustBeException);
    }

    public static bool EvaluateCatchWhenGuard(
        BoundExpr guardExpression,
        string? catchVariableName,
        object? caughtException,
        AlderContext context,
        AlderOptions options,
        CancellationToken ct)
    {
        var guardContext = context.CreateChild();
        if (!string.IsNullOrEmpty(catchVariableName))
            guardContext.Define(catchVariableName, caughtException);

        try
        {
            var evaluator = new BoundEvaluator(guardContext, options, cancellationToken: ct);
            var guardResult = evaluator.Evaluate(guardExpression);
            return TypeHelpers.RequireBoolean(guardResult);
        }
        catch
        {
            // ECMA-334 §13.11: when-guard that throws means the catch clause doesn't match
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckExecutionConstraints(
        ExecutionConstraintState? state,
        ExecutionConstraints? constraints,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (state == null || constraints == null)
            return;

        state.StatementCount++;

        if (constraints.MaxStatements is { } maxStatements and > 0
            && state.StatementCount > maxStatements)
        {
            throw new AlderExecutionLimitException(
                ExecutionLimitType.Statements,
                maxStatements,
                state.StatementCount,
                state.StatementCount,
                state.Timer?.Elapsed ?? TimeSpan.Zero);
        }

        if (state.Timer != null
            && constraints.MaxTimeout is { } maxTimeout
            && state.Timer.Elapsed > maxTimeout)
        {
            throw new AlderExecutionLimitException(
                ExecutionLimitType.Timeout,
                (long)maxTimeout.TotalMilliseconds,
                (long)state.Timer.ElapsedMilliseconds,
                state.StatementCount,
                state.Timer.Elapsed);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckLoopIterationConstraint(
        ExecutionConstraintState? state,
        ExecutionConstraints? constraints)
    {
        if (state == null || constraints == null)
            return;

        var maxIterations = constraints.MaxLoopIterations;
        if (maxIterations <= 0)
            return;

        state.LoopIterationCount++;

        if (state.LoopIterationCount > maxIterations)
        {
            throw new AlderExecutionLimitException(
                ExecutionLimitType.LoopIterations,
                maxIterations,
                state.LoopIterationCount,
                state.StatementCount,
                state.Timer?.Elapsed ?? TimeSpan.Zero);
        }
    }

    public static IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not IEnumerable enumerable)
            throw new AlderException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));

        return enumerable.GetEnumerator();
    }
}
