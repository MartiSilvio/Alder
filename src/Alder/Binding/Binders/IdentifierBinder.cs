using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IdentifierBinder : INodeBinder<IdentifierExpr>
{
    public BoundExpr Bind(IdentifierExpr expr, BindingContext context, BinderContext binder)
    {
        var name = expr.Name.Lexeme;

        if (context.RuntimeContext.Functions.ContainsKey(name) ||
            context.RuntimeContext.Modules.ContainsKey(name))
        {
            return new BoundIdentifierExpr(name, BoundType.Unknown);
        }

        if (context.TryGetLocal(name, out var localType, out var localId))
            return new BoundIdentifierExpr(name, localType, localId);

        context.TryGetVariableType(name, out var staticType);

        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
        return new BoundIdentifierExpr(name, staticType);
    }
}
