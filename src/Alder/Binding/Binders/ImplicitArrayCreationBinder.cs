using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ImplicitArrayCreationExpr))]
internal static class ImplicitArrayCreationBinder
{
    public static BoundExpr Bind(ImplicitArrayCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var elements = expr.Elements
            .Select(element => binder.Bind(element, context))
            .ToImmutableArray();

        var elementType = CollectionExprBinder.FindBestCommonType(elements)
            ?? throw new AlderException(DiagnosticDescriptors.NoBestTypeForImplicitArray, expr.Span, null, null);

        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.Array, null, new BoundType(arrayType));
    }
}
