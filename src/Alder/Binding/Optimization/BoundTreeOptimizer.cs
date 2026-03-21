namespace Alder.Binding.Optimization;

internal static class BoundTreeOptimizer
{
    private static readonly ConstantFoldingPass ConstantFolding = new();
    private static readonly DeadBranchEliminationPass DeadBranchElimination = new();
    private static readonly ConversionInsertionPass ConversionInsertion = new();

    internal static BoundExpr Optimize(BoundExpr tree)
    {
        tree = ConstantFolding.Rewrite(tree);
        tree = DeadBranchElimination.Rewrite(tree);
        tree = ConversionInsertion.Rewrite(tree);
        return tree;
    }
}
