namespace CsEval.Parsing;

internal static class AstDepthValidator
{
    public static void EnsureWithinLimit(Expr root, int maxDepth)
    {
        var validator = new Walker(maxDepth > 0 ? maxDepth : global::CsEval.CsEvalOptions.Default.MaxExpressionDepth);
        try
        {
            validator.Validate(root);
        }
        catch (NotSupportedException)
        {
            // Synthetic Expr nodes used by tests/extensions may not implement visitor traversal.
            // Let binding surface unsupported-node diagnostics in those cases.
        }
    }

    private sealed class Walker : AstWalker<bool>
    {
        public Walker(int maxDepth)
        {
            MaxVisitDepth = maxDepth;
        }

        protected override bool DefaultValue => true;

        public void Validate(Expr root)
        {
            Visit(root);
        }
    }
}
