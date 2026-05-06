using Alder.Binding;

namespace Alder.Pipeline;

internal interface IBoundTreePass
{
    BoundExpr Execute(BoundExpr tree, PipelineContext context);
}
