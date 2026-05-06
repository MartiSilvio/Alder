using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DeconstructionExpr))]
internal static class DeconstructionBinder
{
    public static BoundExpr Bind(DeconstructionExpr expr, BindingContext context, BinderContext binder)
    {
        var valueExpression = binder.Bind(expr.ValueExpression, context);

        // §12.7: implicitly-typed deconstruction requires each variable to have an inferable
        // source. When the source is a tuple of known arity, extra variables cannot be inferred
        // and Roslyn reports CS8130.
        var sourceType = valueExpression.StaticType.ClrType;
        if (TypeHelpers.IsValueTupleType(sourceType))
        {
            var tupleArity = sourceType.GetGenericArguments().Length;
            if (expr.VariableNames.Count > tupleArity)
            {
                var extra = expr.VariableNames[tupleArity];
                throw new AlderException(DiagnosticDescriptors.DeconstructionVariableTypeInference, extra);
            }
        }

        return new BoundDeconstructionExpr(expr, valueExpression, valueExpression.StaticType);
    }
}
