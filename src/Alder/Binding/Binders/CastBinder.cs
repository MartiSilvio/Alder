using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(CastExpr))]
internal static class CastBinder
{
    public static BoundExpr Bind(CastExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        var sourceStaticType = expression.StaticType is not BoundUnknownType
            ? expression.StaticType.ClrType
            : (Type?)null;

        // §10.3: reject statically-impossible explicit conversions at bind time so errors carry
        // the cast expression's span. Only applies when the source type is fully known.
        if (sourceStaticType != null && !TypeHelpers.HasExplicitConversion(sourceStaticType, targetType))
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, sourceStaticType.Name, targetType.Name);

        return new BoundCastExpr(expression, targetType, sourceStaticType, new BoundType(targetType));
    }
}
