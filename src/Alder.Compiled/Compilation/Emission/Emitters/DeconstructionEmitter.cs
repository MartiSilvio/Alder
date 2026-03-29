using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class DeconstructionEmitter : INodeEmitter<BoundDeconstructionExpr>
{
    public LinqExpression Emit(BoundDeconstructionExpr node, EmissionContext ctx)
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
