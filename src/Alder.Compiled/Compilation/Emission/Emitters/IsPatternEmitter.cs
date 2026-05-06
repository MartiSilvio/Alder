using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.IsPatternExpression)]
internal static class IsPatternEmitter
{
    public static LinqExpression Emit(BoundIsPatternExpr node, EmissionContext ctx)
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
            ctx.CancellationTokenParam);
    }
}
