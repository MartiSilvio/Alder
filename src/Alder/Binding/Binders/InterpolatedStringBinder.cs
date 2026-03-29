using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class InterpolatedStringBinder : INodeBinder<InterpolatedStringExpr>
{
    public BoundExpr Bind(InterpolatedStringExpr expr, BindingContext context, BinderContext binder)
    {
        var parts = expr.Parts
            .Select(part => part switch
            {
                TextPart text => (BoundInterpolatedPart)new BoundInterpolatedTextPart(text.Text),
                ExpressionPart expressionPart => new BoundInterpolatedExpressionPart(
                    binder.Bind(expressionPart.Expression, context),
                    expressionPart.AlignmentSpecifier,
                    expressionPart.FormatSpecifier),
                _ => throw new BindingNotSupportedException(
                    $"Interpolated part '{part.GetType().Name}' is not supported")
            })
            .ToImmutableArray();

        return new BoundInterpolatedStringExpr(parts, new BoundType(typeof(string)));
    }
}
