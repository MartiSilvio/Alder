using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class ThrowEmitter : INodeEmitter<BoundThrowExpr>
{
    public LinqExpression Emit(BoundThrowExpr node, EmissionContext ctx)
    {
        var resultType = node.StaticType.ClrType != typeof(object) ? node.StaticType.ClrType : typeof(object);

        if (node.Expression != null)
        {
            var exceptionExpr = ctx.Emit(node.Expression);
            var validated = LinqExpression.Call(ValidateThrowOperandMethod, EmitHelpers.AsObject(exceptionExpr));
            return LinqExpression.Block(
                resultType,
                LinqExpression.Throw(validated, resultType),
                LinqExpression.Default(resultType));
        }

        if (ctx.CatchDepth == 0)
        {
            return LinqExpression.Throw(
                LinqExpression.Constant(new AlderException(Diagnostics.DiagnosticDescriptors.ThrowOutsideCatch)),
                resultType);
        }

        return LinqExpression.Rethrow(resultType);
    }
}
