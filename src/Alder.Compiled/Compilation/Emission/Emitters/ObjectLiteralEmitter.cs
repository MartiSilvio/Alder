using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ObjectLiteral)]
internal static class ObjectLiteralEmitter
{
    public static LinqExpression Emit(BoundObjectLiteralExpr node, EmissionContext ctx)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(ExpandoObjectCtor))
        };

        var itemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        foreach (var property in node.Properties)
        {
            if (property.IsSpread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoDictMethod, dictVar, ctx.EmitBoxed(property.Value), ctx.ContextParam));
                continue;
            }

            statements.Add(LinqExpression.Assign(
                LinqExpression.Property(dictVar, itemProperty, LinqExpression.Constant(property.PropertyName!)),
                ctx.EmitBoxed(property.Value)));
        }

        statements.Add(dictVar);
        return LinqExpression.Block(typeof(IDictionary<string, object?>), [dictVar], statements);
    }
}
