using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IdentifierExpr))]
internal static class IdentifierBinder
{
    public static BoundExpr Bind(IdentifierExpr expr, BindingContext context, BinderContext binder)
    {
        var name = expr.Name.Lexeme;

        if (context.TryGetLocal(name, out var localType, out var localId))
            return new BoundIdentifierExpr(name, localType, localId);

        if (context.TryGetImplicitReceiver(out var receiverName, out var receiverType, out var receiverLocalId))
        {
            var receiver = new BoundIdentifierExpr(
                receiverName,
                receiverType,
                receiverLocalId >= 0 ? receiverLocalId : null);

            var implicitMember = MemberAccessBinder.BindSingleMemberAccess(
                receiver,
                name,
                nullSafe: false,
                context);

            if (implicitMember is not BoundDynamicMemberAccessExpr)
                return implicitMember;
        }

        if (context.RuntimeContext.Functions.ContainsKey(name) ||
            context.RuntimeContext.Modules.ContainsKey(name))
        {
            return new BoundIdentifierExpr(name, BoundType.Unknown);
        }

        var runtimeProbe = new RuntimeBindingProbeService(context.RuntimeContext);
        runtimeProbe.TryGetValueType(name, out var staticType);

        // ECMA-334 §12.8.7.2: an identifier that resolves to a type in expression position (e.g.
        // `DayOfWeek.Wednesday`, `Task.FromResult(...)`, `Math.PI`) is a type reference targeting
        // static-member access on the wrapped type. Distinct from a runtime `Type` value — see
        // BoundTypeRefExpr for the rationale.
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundTypeRefExpr(resolvedType, new BoundType(typeof(Type)));
        return new BoundIdentifierExpr(name, staticType);
    }
}
