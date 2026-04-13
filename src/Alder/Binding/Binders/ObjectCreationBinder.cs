using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ObjectCreationExpr))]
internal static class ObjectCreationBinder
{
    public static BoundExpr Bind(ObjectCreationExpr expr, BindingContext context, BinderContext binder)
    {
        var arguments = expr.Arguments
            .Select(argument => binder.Bind(argument, context))
            .ToImmutableArray();
        var initializerEntries = expr.Initializer != null
            ? [
                ..expr.Initializer.Entries
                    .Select(entry => new BoundInitializerEntry(
                        entry.PropertyName,
                        entry.Value != null ? binder.Bind(entry.Value, context) : null,
                        entry.IndexerKey != null ? binder.Bind(entry.IndexerKey, context) : null,
                        entry.Elements != null
                            ? [..entry.Elements.Select(e => binder.Bind(e, context))]
                            : default))
            ]
            : ImmutableArray<BoundInitializerEntry>.Empty;
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(expr.TypeName);
        if (resolvedType != null)
        {
            // §12.8.16.2: static classes surface as `abstract sealed` in IL — Roslyn emits CS0712 for them.
            if (resolvedType is { IsAbstract: true, IsSealed: true })
                throw new AlderException(DiagnosticDescriptors.CannotInstantiateStaticClass, resolvedType.Name);
            if (resolvedType.IsInterface || resolvedType.IsAbstract)
                throw new AlderException(DiagnosticDescriptors.CannotInstantiateAbstractOrInterface, resolvedType.Name);
        }

        var staticType = resolvedType != null ? new BoundType(resolvedType) : BoundType.Unknown;
        return new BoundObjectCreationExpr(expr.TypeName, arguments, initializerEntries, staticType);
    }
}
