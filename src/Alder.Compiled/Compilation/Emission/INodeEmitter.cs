using Alder.Binding;

namespace Alder.Compiled.Compilation.Emission;

internal interface INodeEmitter<in TNode> where TNode : BoundExpr
{
    LinqExpression Emit(TNode node, EmissionContext ctx);
}