using Alder.Binding;

namespace Alder.Pipeline;

internal sealed class BoundTreePipeline
{
    private readonly IBoundTreePass[] _passes;

    private BoundTreePipeline(IBoundTreePass[] passes) => _passes = passes;

    internal BoundExpr Execute(BoundExpr tree, PipelineContext context)
    {
        foreach (var pass in _passes)
            tree = pass.Execute(tree, context);
        return tree;
    }

    internal static BoundTreePipeline Create(params IBoundTreePass[] passes) => new(passes);
}
