namespace Alder.Binding;

internal abstract class BoundExprWalker
{
    protected virtual bool OnVisit(BoundExpr node) => true;

    public void Walk(BoundExpr root)
    {
        var stack = new Stack<BoundExpr>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!OnVisit(node))
                return;
            node.EnumerateChildren(child => stack.Push(child));
        }
    }
}

internal sealed class DiagnosticCollector : BoundExprWalker
{
    internal readonly List<AlderDiagnostic> Diagnostics = new();

    protected override bool OnVisit(BoundExpr node)
    {
        if (node.Diagnostic != null)
            Diagnostics.Add(node.Diagnostic);
        return true;
    }
}
