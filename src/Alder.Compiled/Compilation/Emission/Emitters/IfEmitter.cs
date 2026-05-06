using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.IfStatement)]
internal static class IfEmitter
{
    public static LinqExpression Emit(BoundIfStatementExpr node, EmissionContext ctx)
    {
        var condition = ctx.EmitBoolCondition(node.Condition);
        var thenBody = BlockEmitter.EmitScopedStatements(ctx, node.ThenStatements);
        var elseBody = node.ElseStatements.IsDefaultOrEmpty
            ? LinqExpression.Constant(null, typeof(object))
            : BlockEmitter.EmitScopedStatements(ctx, node.ElseStatements);

        if (thenBody.Type == elseBody.Type)
            return LinqExpression.Condition(condition, thenBody, elseBody);

        return LinqExpression.Condition(condition, EmitHelpers.AsObject(thenBody), EmitHelpers.AsObject(elseBody));
    }
}
