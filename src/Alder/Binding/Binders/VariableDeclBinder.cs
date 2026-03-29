using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class VariableDeclBinder : INodeBinder<VariableDeclExpr>
{
    public BoundExpr Bind(VariableDeclExpr expr, BindingContext context, BinderContext binder)
    {
        var declaredType = expr.DeclaredType != null
            ? context.RuntimeContext.TypeResolver.ResolveType(expr.DeclaredType.Value.Lexeme)
            : null;
        var initializer = expr.Initializer is CollectionExpr collectionExpr && declaredType != null
            ? CollectionExprBinder.BindCollectionWithTargetType(collectionExpr, context, binder, declaredType)
            : binder.Bind(expr.Initializer, context);
        var staticType = declaredType != null ? new BoundType(declaredType) : initializer.StaticType;
        var localId = context.DeclareLocal(expr.Name.Lexeme, staticType, expr.IsConst);
        return new BoundVariableDeclExpr(
            expr.Name.Lexeme,
            initializer,
            declaredType,
            staticType,
            IsConst: expr.IsConst,
            LocalId: localId);
    }
}
