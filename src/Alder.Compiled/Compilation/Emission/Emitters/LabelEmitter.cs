using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class LabelEmitter : INodeEmitter<BoundLabelExpr>
{
    public LinqExpression Emit(BoundLabelExpr node, EmissionContext ctx)
    {
        return LinqExpression.Constant(null, typeof(object));
    }
}
