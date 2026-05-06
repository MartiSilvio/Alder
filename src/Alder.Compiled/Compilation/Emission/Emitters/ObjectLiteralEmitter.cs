using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ObjectLiteral)]
internal static class ObjectLiteralEmitter
{
    public static LinqExpression Emit(BoundObjectLiteralExpr node, EmissionContext ctx)
    {
        var structuralInfo = ((BoundStructuralType)node.StaticType).StructuralInfo
            ?? throw new InvalidOperationException("Structural object literal missing runtime type metadata.");
        var memberNames = structuralInfo.Members
            .Select(static member => LinqExpression.Constant(member.Name))
            .ToArray();
        var memberTypes = structuralInfo.Members
            .Select(static member => LinqExpression.Constant(member.Type, typeof(Type)))
            .ToArray();
        var values = new LinqExpression[node.Properties.Length];

        for (var i = 0; i < node.Properties.Length; i++)
        {
            var value = ctx.Emit(node.Properties[i].Value);
            values[i] = value.Type == typeof(object)
                ? value
                : LinqExpression.Convert(value, typeof(object));
        }

        return LinqExpression.Call(
            BoundRuntimeMethodCache.CreateStructuralObjectMethod,
            LinqExpression.NewArrayInit(typeof(string), memberNames),
            LinqExpression.NewArrayInit(typeof(Type), memberTypes),
            LinqExpression.NewArrayInit(typeof(object), values));
    }
}
