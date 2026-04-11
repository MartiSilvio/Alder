using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.Label)]
internal static class LabelEmitter
{
    public static LinqExpression Emit(BoundLabelExpr node, EmissionContext ctx)
    {
        return LinqExpression.Constant(null, typeof(object));
    }
}
