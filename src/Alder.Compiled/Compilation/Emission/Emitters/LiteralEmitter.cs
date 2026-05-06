using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.Literal)]
internal static class LiteralEmitter
{
    public static LinqExpression Emit(BoundLiteralExpr node, EmissionContext ctx)
    {
        if (node.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(node.Value, node.Value.GetType());
    }
}
