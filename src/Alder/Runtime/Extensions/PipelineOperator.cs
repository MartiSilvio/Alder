using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Implements the pipeline operator by invoking the right-hand callable with the left-hand value.
/// Dispatch goes through <see cref="MethodInvoker.InvokeCall"/> so all Alder callable forms share one runtime path.
/// </summary>
internal static class PipelineOperator
{
    /// <summary>
    /// Invokes <paramref name="rightCallable"/> with <paramref name="leftValue"/> as its single argument.
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

        // Guard this explicitly so non-callable values fail with the pipeline operator diagnostic.
        if (!MethodInvoker.IsCallable(rightCallable))
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.PipeGreater),
                TypeNameFormatter.Of(leftValue),
                rightCallable.GetType().Name);

        var args = new object?[] { leftValue };
        return MethodInvoker.InvokeCall(rightCallable, args, context, ct: ct);
    }
}
