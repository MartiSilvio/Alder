using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;

namespace CsEval.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private Dictionary<int, PromotedLocal>? _promotedLocals;

    private sealed record PromotedLocal(string Name, ParameterExpression Variable, Type VariableType);

    private bool TryGetPromoted(int? localId, out PromotedLocal promoted)
    {
        if (localId is { } id && _promotedLocals != null && _promotedLocals.TryGetValue(id, out promoted!))
            return true;
        promoted = null!;
        return false;
    }

    private static Dictionary<int, PromotedLocal> BuildLocalPromotionPlan(BoundExpr root)
    {
        var walker = new PromotionWalker();
        walker.Visit(root);

        if (walker.HasLambda)
            return new Dictionary<int, PromotedLocal>();

        // If the same variable name appears in multiple declarations (different LocalIds),
        // the runtime would reject it via DefineNew's duplicate check. We must let the
        // runtime path handle that, so exclude all declarations with conflicting names.
        var result = walker.Result;
        var nameCount = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var promoted in result.Values)
        {
            nameCount.TryGetValue(promoted.Name, out var count);
            nameCount[promoted.Name] = count + 1;
        }

        var idsToRemove = new List<int>();
        foreach (var (id, promoted) in result)
        {
            if (nameCount[promoted.Name] > 1)
                idsToRemove.Add(id);
        }

        foreach (var id in idsToRemove)
            result.Remove(id);

        return result;
    }

    private sealed class PromotionWalker : BoundExprWalker
    {
        internal readonly Dictionary<int, PromotedLocal> Result = new();
        internal bool HasLambda;

        protected override object? VisitLambda(BoundLambdaExpr node)
        {
            HasLambda = true;
            return null;
        }

        protected override object? VisitVariableDecl(BoundVariableDeclExpr node)
        {
            if (!node.IsConst && node.StaticType != typeof(object) && node.LocalId is { } id)
            {
                var variableType = node.DeclaredType ?? node.StaticType;
                Result[id] = new PromotedLocal(
                    node.Name,
                    LinqExpression.Variable(typeof(object), $"local_{node.Name}"),
                    variableType);
            }

            return base.VisitVariableDecl(node);
        }

        protected override object? DefaultVisit(BoundExpr node)
        {
            if (HasLambda) return null;
            return base.DefaultVisit(node);
        }
    }
}
