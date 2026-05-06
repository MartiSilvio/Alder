using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.DeconstructionAssignment)]
internal static class DeconstructionEmitter
{
    public static LinqExpression Emit(BoundDeconstructionExpr node, EmissionContext ctx)
    {
        var variableNames = LinqExpression.NewArrayInit(
            typeof(string),
            node.VariableNames.Select(static name => LinqExpression.Constant(name)));
        return LinqExpression.Call(
            DeconstructTupleMethod,
            ctx.EmitBoxed(node.ValueExpression),
            variableNames,
            ctx.ContextParam);
    }
}
