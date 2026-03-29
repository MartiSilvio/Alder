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
            return LinqExpression.TypeIs(
                ctx.EmitBoxed(node.Expression),
                resolvedType);
        }

        return LinqExpression.Call(
            MatchPatternMethod,
            ctx.EmitBoxed(node.Expression),
            LinqExpression.Constant(node.Pattern, typeof(Pattern)),
            ctx.ContextParam,
            ctx.ConfigParam,
            ctx.CancellationTokenParam);
    }
}
