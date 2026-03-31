using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IdentifierEmitter : INodeEmitter<BoundIdentifierExpr>
{
    public LinqExpression Emit(BoundIdentifierExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetLambdaParameter(node.Name, out var lambdaParam))
            return lambdaParam;

        if (node.LocalId is { } localId &&
            ctx.PromotedLocals != null &&
            ctx.PromotedLocals.TryGetValue(localId, out var promoted))
        {
            return promoted.Variable;
        }

        if (ctx.HoistedIdentifiers != null &&
            ctx.HoistedIdentifiers.TryGetValue(node.Name, out var hoisted))
        {
            return hoisted.Variable;
        }

        if (node.StaticType.ClrType != typeof(object) && !TypeHelpers.IsValueTupleType(node.StaticType.ClrType))
        {
            return LinqExpression.Call(
                ctx.ContextParam,
                GetVariableTypedMethodFor(node.StaticType.ClrType),
                LinqExpression.Constant(node.Name));
        }

        return LinqExpression.Call(
            ResolveIdentifierMethod,
            LinqExpression.Constant(node.Name),
            ctx.ContextParam,
            ctx.ConfigParam);
    }
}
