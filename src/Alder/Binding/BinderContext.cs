using Alder.Parsing;

namespace Alder.Binding;

internal sealed class BinderContext
{
    private readonly Func<Expr, BindingContext, BinderContext, BoundExpr> _bind;

    internal BinderFlags Flags { get; }

    internal BinderContext(Func<Expr, BindingContext, BinderContext, BoundExpr> bind)
        : this(bind, BinderFlags.None)
    {
    }

    private BinderContext(Func<Expr, BindingContext, BinderContext, BoundExpr> bind, BinderFlags flags)
    {
        _bind = bind;
        Flags = flags;
    }

    public BoundExpr Bind(Expr expr, BindingContext context) => _bind(expr, context, this);

    internal BinderContext WithAdditionalFlags(BinderFlags flags)
        => Includes(flags) ? this : new(_bind, Flags | flags);

    internal BinderContext WithFlags(BinderFlags flags)
        => Flags == flags ? this : new(_bind, flags);

    internal bool Includes(BinderFlags flag) => (Flags & flag) != 0;
}
