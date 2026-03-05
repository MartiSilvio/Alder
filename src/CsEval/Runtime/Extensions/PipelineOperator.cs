using CsEval.Diagnostics;

namespace CsEval.Runtime.Extensions;

/// <summary>
/// Pipeline operator (F#/Elixir): x |> f invokes f(x).
/// Dispatches through MethodInvoker.InvokeCall to support all callable types:
/// LambdaValue, CompiledLambdaValue, FunctionRef, Delegate, etc.
/// </summary>
internal static class PipelineOperator
{
    /// <summary>
    /// Invokes the pipeline: evaluates <paramref name="rightCallable"/> with
    /// <paramref name="leftValue"/> as its single argument.
    /// </summary>
    public static object? InvokePipeline(
        object? leftValue,
        object? rightCallable,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct)
    {
        if (rightCallable is null)
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "|>",
                leftValue?.GetType().Name ?? "null", "null");

        // Check if the right side is a known callable type before invoking.
        // MethodInvoker.InvokeCall handles: LambdaValue, CompiledLambdaValue, FunctionRef,
        // Delegate, ModuleMethodRef, StaticMethodRef, MethodRef.
        if (!IsCallable(rightCallable))
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "|>",
                leftValue?.GetType().Name ?? "null", rightCallable.GetType().Name);

        var args = new object?[] { leftValue };
        return MethodInvoker.InvokeCall(rightCallable, args, context, options, ct);
    }

    private static bool IsCallable(object value) => value is
        LambdaValue or
        CompiledLambdaValue or
        FunctionRef or
        Delegate or
        ModuleMethodRef or
        StaticMethodRef or
        MethodRef;
}
