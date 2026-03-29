using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Text;

namespace Alder.Binding.Binders;

internal sealed class CollectionExprBinder : INodeBinder<CollectionExpr>
{
    public BoundExpr Bind(CollectionExpr expr, BindingContext context, BinderContext binder)
    {
        return BindCollectionWithTargetType(expr, context, binder, null);
    }

    internal static BoundExpr BindCollectionWithTargetType(CollectionExpr expr, BindingContext context, BinderContext binder, Type? targetType)
    {
        var elements = expr.Elements
            .Select(element => binder.Bind(element, context))
            .ToImmutableArray();

        if (targetType != null)
            return CreateTargetTypedCollection(elements, targetType);

        if (context.LanguageMode == LanguageMode.Standard)
            throw new AlderException(DiagnosticDescriptors.NoTargetTypeForCollectionExpression, expr.Span, null, null);

        return CreateInferredArrayCollection(elements, expr.Span);
    }

    private static BoundCollectionCreationExpr CreateTargetTypedCollection(ImmutableArray<BoundExpr> elements, Type targetType)
    {
        var elementType = GetCollectionElementType(targetType)!;
        var kind = targetType.IsArray ? CollectionKind.Array : CollectionKind.TargetTypedCollection;
        return new BoundCollectionCreationExpr(elements, elementType, kind, targetType, new BoundType(targetType));
    }

    private static BoundCollectionCreationExpr CreateInferredArrayCollection(ImmutableArray<BoundExpr> elements, TextSpan span)
    {
        var elementType = FindBestCommonType(elements) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        var elementMemberTypes = InferCommonElementMemberTypes(elements);
        var staticType = elementMemberTypes != null
            ? (BoundType)new BoundStructuralType(arrayType, elementMemberTypes)
            : new BoundType(arrayType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.InferredArray, null, staticType);
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static ImmutableDictionary<string, Type>? InferCommonElementMemberTypes(ImmutableArray<BoundExpr> elements)
    {
        if (elements.Length == 0)
            return null;

        var first = elements[0].StaticType.MemberTypes;
        if (first == null)
            return null;

        for (var i = 1; i < elements.Length; i++)
        {
            var other = elements[i].StaticType.MemberTypes;
            if (other == null || other.Count != first.Count)
                return null;

            foreach (var kvp in first)
            {
                if (!other.TryGetValue(kvp.Key, out var otherType) || otherType != kvp.Value)
                    return null;
            }
        }

        return first;
    }

    /// <summary>
    /// ECMA-334 §12.6.3.15: Finding the best common type of a set of expressions.
    /// Collects all element static types as lower bounds, then finds the unique candidate
    /// to which all others implicitly convert (§12.6.3.12 fixing).
    /// Returns null if inference fails (no unique best type).
    /// </summary>
    internal static Type? FindBestCommonType(ImmutableArray<BoundExpr> elements)
    {
        if (elements.Length == 0)
            return null;

        var types = new List<Type>(elements.Length);
        var hasNullLiteral = false;
        foreach (var element in elements)
        {
            if (element is BoundLiteralExpr { Value: null })
            {
                hasNullLiteral = true;
                continue;
            }

            var clrType = element.StaticType.ClrType;
            if (!types.Contains(clrType))
                types.Add(clrType);
        }

        if (types.Count == 0)
            return hasNullLiteral ? typeof(object) : null;

        if (types.Count == 1)
            return LiftIfNeeded(types[0], hasNullLiteral);

        Type? bestType = null;
        foreach (var candidate in types)
        {
            var allConvert = true;
            foreach (var other in types)
            {
                if (other == candidate)
                    continue;
                if (!TypeHelpers.CanImplicitlyConvert(other, candidate))
                {
                    allConvert = false;
                    break;
                }
            }

            if (!allConvert)
                continue;

            if (bestType != null)
                return null;

            bestType = candidate;
        }

        return bestType != null ? LiftIfNeeded(bestType, hasNullLiteral) : null;

        static Type LiftIfNeeded(Type type, bool hasNull) =>
            hasNull && type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;
    }
}
