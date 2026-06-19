using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DeconstructionAssignment)]
internal static class DeconstructionEvaluator
{
    public static object? Evaluate(BoundDeconstructionExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.ValueExpression, ct);

        if (value is ITuple tuple)
        {
            if (tuple.Length != node.VariableNames.Length)
            {
                throw new AlderException(
                    DiagnosticDescriptors.DeconstructionCountMismatch, node.VariableNames.Length, tuple.Length);
            }

            for (var i = 0; i < node.VariableNames.Length; i++)
            {
                var elementValue = tuple[i];
                var elementType = elementValue?.GetType() ?? typeof(object);
                DefineVariable(node, ctx, i, elementValue, elementType);
            }

            return value;
        }

        if (value != null)
        {
            var deconstructed = ConstructionRuntime.TryDeconstruct(value, node.VariableNames.Length);
            if (deconstructed != null)
            {
                for (var i = 0; i < node.VariableNames.Length; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    DefineVariable(node, ctx, i, elementValue, elementType);
                }

                return value;
            }
        }

        throw new AlderException(DiagnosticDescriptors.DeconstructionFailed, value?.GetType().Name ?? "null");
    }

    public static async ValueTask<object?> EvaluateAsync(BoundDeconstructionExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.ValueExpression, ct);

        if (value is ITuple tuple)
        {
            if (tuple.Length != node.VariableNames.Length)
            {
                throw new AlderException(
                    DiagnosticDescriptors.DeconstructionCountMismatch, node.VariableNames.Length, tuple.Length);
            }

            for (var i = 0; i < node.VariableNames.Length; i++)
            {
                var elementValue = tuple[i];
                var elementType = elementValue?.GetType() ?? typeof(object);
                DefineVariable(node, ctx, i, elementValue, elementType);
            }

            return value;
        }

        if (value != null)
        {
            var deconstructed = ConstructionRuntime.TryDeconstruct(value, node.VariableNames.Length);
            if (deconstructed != null)
            {
                for (var i = 0; i < node.VariableNames.Length; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    DefineVariable(node, ctx, i, elementValue, elementType);
                }

                return value;
            }
        }

        throw new AlderException(DiagnosticDescriptors.DeconstructionFailed, value?.GetType().Name ?? "null");
    }

    private static void DefineVariable(
        BoundDeconstructionExpr node,
        EvaluationContext ctx,
        int index,
        object? value,
        Type elementType)
    {
        var name = node.VariableNames[index];
        if (name == TokenLexemes.DiscardIdentifier)
            return;

        ctx.Context.DefineNew(
            name,
            value,
            elementType,
            isReadOnly: node.Source.DeclaresIterationVariables);
    }
}
