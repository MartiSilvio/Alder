using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.VariableDeclaration)]
internal static class VariableDeclEmitter
{
    public static LinqExpression Emit(BoundVariableDeclExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            if (node.DeclaredType != null)
            {
                var validated = LinqExpression.Call(
                    ValidateAndCoerceTypeMethod,
                    LinqExpression.Constant(node.DeclaredType, typeof(Type)),
                    ctx.EmitBoxed(node.Initializer),
                    LinqExpression.Constant(node.Name),
                    LinqExpression.Constant(BoundExpr.IsConstantExpression(node.Initializer)));
                return LinqExpression.Assign(promoted.Variable,
                    EmitHelpers.EnsureTypedExpression(validated, promoted.Variable.Type));
            }
            return LinqExpression.Assign(promoted.Variable, ctx.EmitAs(node.Initializer, promoted.Variable.Type));
        }

        return LinqExpression.Call(
            DefineVariableMethod,
            LinqExpression.Constant(node.Name),
            ctx.EmitBoxed(node.Initializer),
            node.DeclaredType != null
                ? LinqExpression.Constant(node.DeclaredType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            ctx.ContextParam,
            LinqExpression.Constant(node.IsReadOnly),
            LinqExpression.Constant(BoundExpr.IsConstantExpression(node.Initializer)));
    }
}
