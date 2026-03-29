using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IsPatternEmitter : INodeEmitter<BoundIsPatternExpr>
{
    public LinqExpression Emit(BoundIsPatternExpr node, EmissionContext ctx)
    {
        if (node.Pattern is TypePattern { VariableName: null } typePattern
            && TypeResolver.TryResolveKeywordType(typePattern.TypeToken.Lexeme, out var resolvedType))
        {
            return LinqExpression.Convert(
                LinqExpression.TypeIs(
                    EmitHelpers.AsObject(ctx.Emit(node.Expression)),
                    resolvedType),
                typeof(object));
        }

        return LinqExpression.Convert(
            LinqExpression.Call(
                MatchPatternMethod,
                EmitHelpers.AsObject(ctx.Emit(node.Expression)),
                LinqExpression.Constant(node.Pattern, typeof(Pattern)),
                ctx.ContextParam,
                ctx.ConfigParam,
                ctx.CancellationTokenParam),
            typeof(object));
    }
}
