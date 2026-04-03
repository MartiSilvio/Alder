using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime.Extensions;

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
        AlderContext context,
        CancellationToken ct)
    {
        if (rightCallable is null)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                TypeNameFormatter.Null);

        // Check if the right side is a known callable type before invoking.
        // MethodInvoker.InvokeCall handles: LambdaValue, CompiledLambdaValue, FunctionRef,
        // Delegate, ModuleMethodRef, StaticMethodRef, MethodRef.
        if (!IsCallable(rightCallable))
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                rightCallable.GetType().Name);

        var args = new object?[] { leftValue };
        return MethodInvoker.InvokeCall(rightCallable, args, context, ct: ct);
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
