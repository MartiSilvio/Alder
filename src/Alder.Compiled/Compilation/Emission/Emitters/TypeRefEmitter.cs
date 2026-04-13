using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.TypeReference)]
internal static class TypeRefEmitter
{
    public static LinqExpression Emit(BoundTypeRefExpr node, EmissionContext ctx) =>
        LinqExpression.Constant(node.TargetType, typeof(Type));
}
