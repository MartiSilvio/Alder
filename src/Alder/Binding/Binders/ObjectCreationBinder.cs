using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ObjectCreationBinder : INodeBinder<ObjectCreationExpr>
{
    public BoundExpr Bind(ObjectCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var arguments = expr.Arguments
            .Select(argument => binder.Bind(argument, context))
            .ToImmutableArray();
        var initializerEntries = expr.Initializer != null
            ? [
                ..expr.Initializer.Entries
                    .Select(entry => new BoundInitializerEntry(
                        entry.PropertyName,
                        binder.Bind(entry.Value, context),
                        entry.IndexerKey != null ? binder.Bind(entry.IndexerKey, context) : null))
            ]
            : ImmutableArray<BoundInitializerEntry>.Empty;
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(expr.TypeName);
        var staticType = resolvedType != null ? new BoundType(resolvedType) : BoundType.Unknown;
        return new BoundObjectCreationExpr(expr.TypeName, arguments, initializerEntries, staticType);
    }
}
