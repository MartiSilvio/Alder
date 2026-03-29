using System.Linq.Expressions;
using Alder.Binding;

namespace Alder.Compiled.Compilation.Emission;

internal interface INodeEmitter<in TNode> where TNode : BoundExpr
{
    Expression Emit(TNode node, EmissionContext ctx);
}
