using System.Linq.Expressions;
using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class LiteralEmitter : INodeEmitter<BoundLiteralExpr>
{
    public Expression Emit(BoundLiteralExpr node, EmissionContext ctx)
    {
        if (node.Value == null)
            return Expression.Constant(null, typeof(object));

        return Expression.Constant(node.Value, node.Value.GetType());
    }
}
