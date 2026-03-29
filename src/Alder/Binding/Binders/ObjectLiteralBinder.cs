using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ObjectLiteralBinder : INodeBinder<ObjectLiteralExpr>
{
    public BoundExpr Bind(ObjectLiteralExpr expr, BindingContext context, BinderContext binder)
    {
        var properties = expr.Properties
            .Select(property =>
            {
                var (key, value) = property;
                if (key.Type == TokenType.DotDot && value is SpreadExpr spread)
                {
                    return new BoundObjectLiteralProperty(
                        PropertyName: null,
                        Value: binder.Bind(spread.Expression, context),
                        IsSpread: true);
                }

                return new BoundObjectLiteralProperty(
                    PropertyName: key.Lexeme,
                    Value: binder.Bind(value, context),
                    IsSpread: false);
            })
            .ToImmutableArray();

        var hasSpread = properties.Any(static p => p.IsSpread);
        var staticType = hasSpread
            ? new BoundType(typeof(System.Dynamic.ExpandoObject))
            : new BoundStructuralType(
                typeof(System.Dynamic.ExpandoObject),
                properties
                    .Where(static p => p.PropertyName != null)
                    .ToImmutableDictionary(static p => p.PropertyName!, static p => p.Value.StaticType.ClrType));
        return new BoundObjectLiteralExpr(properties, staticType);
    }
}
