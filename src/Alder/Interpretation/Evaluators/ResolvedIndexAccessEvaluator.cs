using System.Collections;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ResolvedIndexAccess)]
internal static class ResolvedIndexAccessEvaluator
{
    public static object? Evaluate(BoundResolvedIndexAccessExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = ctx.Evaluate(node.Index);

        if (node.IsDirectCollectionAccess)
        {
            if (node.TargetType == typeof(string))
            {
                var s = (string)target;
                var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), s.Length, ctx.Config.LanguageMode);
                return (object)s[i];
            }

            if (typeof(IList).IsAssignableFrom(node.TargetType))
            {
                var list = (IList)target;
                var i = MemberAccess.NormalizeIndex(Convert.ToInt32(index), list.Count, ctx.Config.LanguageMode);
                return TypeHelpers.GuardReflectionLeak(list[i], $"index [{i}]");
            }

            if (typeof(IDictionary<string, object?>).IsAssignableFrom(node.TargetType))
            {
                var dict = (IDictionary<string, object?>)target;
                return index is string key && dict.TryGetValue(key, out var value)
                    ? TypeHelpers.GuardReflectionLeak(value, $"index [{key}]")
                    : null;
            }
        }

        return MemberAccess.GetIndex(target, index, ctx.Config, ctx.Context);
    }
}
