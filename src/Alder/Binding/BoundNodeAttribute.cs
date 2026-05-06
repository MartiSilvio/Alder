namespace Alder.Binding;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class BoundNodeAttribute : Attribute
{
    public BoundNodeAttribute(BoundNodeKind kind, string visitorName)
    {
        Kind = kind;
        VisitorName = visitorName;
    }

    public BoundNodeKind Kind { get; }
    public string VisitorName { get; }

    /// <summary>
    /// When true, the rewriter uses iterative left-spine flattening instead of recursive descent.
    /// Prevents stack overflow on deeply nested left-associative chains.
    /// </summary>
    public bool ChainFlatten { get; init; }

    /// <summary>
    /// When true, emits a <c>protected virtual BoundExpr Revisit{VisitorName}(BoundExpr node)</c> hook
    /// for subclass rewriters (e.g. constant folding). Only meaningful when <see cref="ChainFlatten"/> is true.
    /// </summary>
    public bool HasRevisitHook { get; init; }

    /// <summary>
    /// When true, the generator skips rewriter generation for this node.
    /// The rewriter method must be provided in a hand-written partial file.
    /// </summary>
    public bool ManualRewrite { get; init; }
}
