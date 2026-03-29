using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private Dictionary<int, Emission.PromotedLocal>? _promotedLocals;

    private static Dictionary<int, Emission.PromotedLocal> BuildLocalPromotionPlan(BoundExpr root)
    {
        var walker = new PromotionWalker();
        walker.Walk(root);

        if (walker.HasLambda)
            return new Dictionary<int, Emission.PromotedLocal>();

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
        internal readonly Dictionary<int, Emission.PromotedLocal> Result = new();
        internal bool HasLambda;

        protected override bool OnVisit(BoundExpr node)
        {
            if (HasLambda) return false;

            switch (node)
            {
                case BoundLambdaExpr:
                    HasLambda = true;
                    return false;
                case BoundVariableDeclExpr { IsConst: false } decl
                    when decl.StaticType.ClrType != typeof(object) && decl.LocalId is { } id:
                {
                    var variableType = decl.DeclaredType ?? decl.StaticType.ClrType;
                    var storageType = variableType.IsValueType && !TypeHelpers.IsValueTupleType(variableType)
                        ? variableType : typeof(object);
                    Result[id] = new Emission.PromotedLocal(
                        decl.Name,
                        LinqExpression.Variable(storageType, $"local_{decl.Name}"),
                        variableType);
                    break;
                }
            }

            return true;
        }
    }
}
