using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class NamedArgumentEmitter : INodeEmitter<BoundNamedArgumentExpr>
{
    public Expression Emit(BoundNamedArgumentExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                NamedArgCtor,
                LinqExpression.Constant(node.Name),
                EmitHelpers.AsObject(ctx.Emit(node.Value))),
            typeof(object));
    }
}

internal sealed class OutArgEmitter : INodeEmitter<BoundOutArgExpr>
{
    public Expression Emit(BoundOutArgExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                OutArgMarkerCtor,
                LinqExpression.Constant(node.VariableName),
                LinqExpression.Constant(node.TypeName, typeof(string)),
                LinqExpression.Constant(node.IsDiscard)),
            typeof(object));
    }
}

internal sealed class SpreadEmitter : INodeEmitter<BoundSpreadExpr>
{
    public Expression Emit(BoundSpreadExpr node, EmissionContext ctx)
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }
}
