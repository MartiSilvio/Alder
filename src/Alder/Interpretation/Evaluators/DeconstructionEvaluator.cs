using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DeconstructionAssignment)]
internal static class DeconstructionEvaluator
{
    public static object? Evaluate(BoundDeconstructionExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.ValueExpression);

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
                ctx.Context.DefineNew(node.VariableNames[i], elementValue, elementType);
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
                    ctx.Context.DefineNew(node.VariableNames[i], elementValue, elementType);
                }

                return value;
            }
        }

        throw new AlderException(DiagnosticDescriptors.DeconstructionFailed, value?.GetType().Name ?? "null");
    }
}
