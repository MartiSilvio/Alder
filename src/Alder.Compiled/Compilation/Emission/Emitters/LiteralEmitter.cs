using System.Linq.Expressions;
using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class LiteralEmitter : INodeEmitter<BoundLiteralExpr>
{
    public LinqExpression Emit(BoundLiteralExpr node, EmissionContext ctx)
    {
        if (node.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(node.Value, node.Value.GetType());
    }
}
