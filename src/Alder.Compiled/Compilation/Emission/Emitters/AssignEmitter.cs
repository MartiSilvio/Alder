using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.AssignmentOperator)]
internal static class AssignEmitter
{
    public static LinqExpression Emit(BoundAssignExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            var storageType = promoted.Variable.Type;
            var valueType = node.Value.StaticType.ClrType;
            if (valueType != typeof(object) && promoted.VariableType.IsAssignableFrom(valueType))
            {
                return LinqExpression.Block(
                    storageType,
                    LinqExpression.Assign(promoted.Variable, ctx.EmitAs(node.Value, storageType)),
                    promoted.Variable);
            }

            var validatedVar = LinqExpression.Variable(typeof(object), "assignValue");
            return LinqExpression.Block(
                storageType,
                [validatedVar],
                LinqExpression.Assign(validatedVar, ctx.EmitBoxed(node.Value)),
                LinqExpression.Assign(
                    validatedVar,
                    LinqExpression.Call(
                        ValidateVariableAssignmentLocalMethod,
                        LinqExpression.Constant(node.Name),
                        validatedVar,
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)))),
                LinqExpression.Assign(promoted.Variable,
                    EmitHelpers.EnsureTypedExpression(validatedVar, storageType)),
                promoted.Variable);
        }

        var nonPromotedValue = LinqExpression.Variable(typeof(object), "assignValue");
        return LinqExpression.Block(
            typeof(object),
            [nonPromotedValue],
            LinqExpression.Assign(nonPromotedValue, ctx.EmitBoxed(node.Value)),
            LinqExpression.Assign(
                nonPromotedValue,
                LinqExpression.Call(
                    ValidateVariableAssignmentMethod,
                    LinqExpression.Constant(node.Name),
                    nonPromotedValue,
                    ctx.ContextParam)),
            LinqExpression.Call(ctx.ContextParam, ContextSetMethod, LinqExpression.Constant(node.Name), nonPromotedValue),
            nonPromotedValue);
    }
}
