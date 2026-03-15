using System.Runtime.CompilerServices;
using CsEval.Binding;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;

namespace CsEval.Runtime;

internal static class ExecutionRuntime
{
    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException(DiagnosticDescriptors.SandboxAssignmentBlocked, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureMethodCallsAllowed(
        CsEvalOptions options,
        string methodName,
        Type? staticDeclaringType = null,
        bool isModuleCall = false)
    {
        if (isModuleCall)
            return;

        if (options.Sandbox.AllowMethodCalls)
            return;

        if (staticDeclaringType != null)
            throw new CsEvalException(DiagnosticDescriptors.SandboxMethodCallBlocked, $"{staticDeclaringType.Name}.{methodName}");

        throw new CsEvalException(DiagnosticDescriptors.SandboxMethodCallBlocked, methodName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureMemberReadAllowed(
        CsEvalOptions options,
        string memberName,
        bool isStatic,
        bool isField,
        Type? staticDeclaringType = null)
    {
        if (!isStatic)
        {
            if (!options.Sandbox.AllowPropertyRead)
                throw new CsEvalException(DiagnosticDescriptors.SandboxPropertyAccessBlocked, memberName);
            return;
        }

        if (isField)
        {
            if (!options.Sandbox.AllowStaticFieldRead)
                throw new CsEvalException(DiagnosticDescriptors.SandboxStaticFieldAccessBlocked, staticDeclaringType?.Name ?? "type", memberName);
            return;
        }

        if (!options.Sandbox.AllowStaticPropertyRead)
            throw new CsEvalException(DiagnosticDescriptors.SandboxStaticPropertyAccessBlocked, staticDeclaringType?.Name ?? "type", memberName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureMemberTargetNotNull(object? target, string memberName)
    {
        if (target == null)
            throw new CsEvalException(DiagnosticDescriptors.NullMemberAccess, "property", memberName);
        return target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureCallTargetNotNull(object? target, string methodName)
    {
        if (target == null)
            throw new CsEvalException(DiagnosticDescriptors.NullMethodCall, methodName);
        return target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object EnsureIndexTargetNotNull(object? target)
    {
        if (target == null)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);
        return target;
    }

    public static void CheckNullCoalesceAssignAllowed(string name, CsEvalContext context)
    {
        if (context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new CsEvalException(
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
            throw new CsEvalException(DiagnosticDescriptors.LockRequiresNonNull);

        return lockObj;
    }

    public static Exception ValidateThrowOperand(object? value)
    {
        if (value is Exception exception)
            return exception;

        throw new CsEvalException(DiagnosticDescriptors.ThrowExpressionMustBeException);
    }

    public static bool EvaluateCatchWhenGuard(
        BoundExpr guardExpression,
        string? catchVariableName,
        object? caughtException,
        CsEvalContext context,
        CsEvalOptions options,
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
            throw new CsEvalExecutionLimitException(
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
            throw new CsEvalExecutionLimitException(
                ExecutionLimitType.Timeout,
                (long)maxTimeout.TotalMilliseconds,
                (long)state.Timer.ElapsedMilliseconds,
                state.StatementCount,
                state.Timer.Elapsed);
        }
    }

    public static IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not IEnumerable enumerable)
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));

        return enumerable.GetEnumerator();
    }
}
