using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(VariableDeclExpr))]
internal static class VariableDeclBinder
{
    public static BoundExpr Bind(VariableDeclExpr expr, BindingContext context, BinderContext binder)
    {
        var declaredType = expr.DeclaredType != null
            ? context.RuntimeContext.TypeResolver.ResolveType(expr.DeclaredType.Value.Lexeme)
            : null;
        
        BoundExpr initializer;
        
        switch (expr.Initializer)
        {
            case CollectionExpr collectionExpr when declaredType != null:
                initializer = CollectionExprBinder.BindCollectionWithTargetType(collectionExpr, context, binder, declaredType);
                break;
            case ObjectCreationExpr { TypeName: "" } targetTypedNew when expr.DeclaredType != null:
            {
                var typedNew = targetTypedNew with { TypeName = expr.DeclaredType.Value.Lexeme };
                initializer = binder.Bind(typedNew, context);
                break;
            }
            default:
                initializer = binder.Bind(expr.Initializer, context);
                break;
        }
        
        var staticType = declaredType != null
            ? CreateBoundType(declaredType, expr.TupleElementNames)
            : initializer.StaticType;
        var localId = context.DeclareLocal(expr.Name.Lexeme, staticType, expr.IsConst);
        return new BoundVariableDeclExpr(
            expr.Name.Lexeme,
            initializer,
            declaredType,
            staticType,
            IsConst: expr.IsConst,
            LocalId: localId);
    }

    private static BoundType CreateBoundType(Type clrType, IReadOnlyList<string?>? tupleElementNames)
    {
        if (tupleElementNames == null || !TypeHelpers.IsValueTupleType(clrType))
            return new BoundType(clrType);

        var genericArgs = clrType.GetGenericArguments();
        var members = ImmutableDictionary.CreateBuilder<string, Type>();
        for (var i = 0; i < tupleElementNames.Count && i < genericArgs.Length; i++)
        {
            if (tupleElementNames[i] is { } name)
                members[name] = genericArgs[i];
        }

        return members.Count > 0
            ? new BoundStructuralType(clrType, members.ToImmutable(), [..tupleElementNames])
            : new BoundType(clrType);
    }
}
