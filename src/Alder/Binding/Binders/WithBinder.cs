using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(WithExpr))]
internal static class WithBinder
{
    public static BoundExpr Bind(WithExpr expr, BindingContext context, BinderContext binder)
    {
        var obj = binder.Bind(expr.Object, context);
        var initializers = ImmutableArray.CreateBuilder<BoundWithInitializer>(expr.Initializers.Count);

        foreach (var (key, value) in expr.Initializers)
        {
            var boundValue = binder.Bind(value, context);
            initializers.Add(new BoundWithInitializer(key.Lexeme, boundValue));
        }

        return new BoundWithExpr(obj, initializers.ToImmutable(), obj.StaticType)
        {
            Span = expr.Span
        };
    }
}
