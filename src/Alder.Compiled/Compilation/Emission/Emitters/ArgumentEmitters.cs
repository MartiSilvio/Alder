using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Diagnostics;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.NamedArgument)]
internal static class NamedArgumentEmitter
{
    public static LinqExpression Emit(BoundNamedArgumentExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                NamedArgCtor,
                LinqExpression.Constant(node.Name),
                ctx.EmitBoxed(node.Value)),
            typeof(object));
    }
}

[EmitsNode(BoundNodeKind.OutArgument)]
internal static class OutArgEmitter
{
    public static LinqExpression Emit(BoundOutArgExpr node, EmissionContext ctx)
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

[EmitsNode(BoundNodeKind.SpreadElement)]
internal static class SpreadEmitter
{
    public static LinqExpression Emit(BoundSpreadExpr node, EmissionContext ctx)
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }
}
