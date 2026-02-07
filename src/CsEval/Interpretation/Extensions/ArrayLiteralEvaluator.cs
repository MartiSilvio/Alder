using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;

namespace CsEval.Interpretation.Extensions;

/// <summary>
/// Evaluator for CsEval array literal expressions: [1, 2, 3].
/// Handles spread operator (..) within array elements.
/// Called explicitly from Evaluator.VisitArrayLiteral.
/// </summary>
internal static class ArrayLiteralEvaluator
{
    internal static object? EvaluateArrayLiteral(ArrayLiteralExpr expr, Func<Expr, object?> evaluate, CsEvalContext context)
    {
        var result = new List<object?>();
        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = evaluate(spread.Expression);
                if (spreadValue is IEnumerable enumerable and not string)
                {
                    foreach (var item in enumerable)
                        result.Add(item);
                }
                else
                {
                    throw new CsEvalException("Spread operator requires an iterable");
                }
            }
            else
            {
                result.Add(evaluate(element));
            }
        }
        return SpreadHelpers.CreateTypedArray(result);
    }
}
