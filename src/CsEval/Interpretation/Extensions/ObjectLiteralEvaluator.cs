using System.Dynamic;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation.Extensions;

/// <summary>
/// Evaluator for CsEval anonymous object literal expressions: new { Name = "John" }.
/// Creates ExpandoObject with property spread support.
/// Called explicitly from Evaluator.VisitObjectLiteral.
/// </summary>
internal static class ObjectLiteralEvaluator
{
    internal static object? EvaluateObjectLiteral(ObjectLiteralExpr expr, Func<Expr, object?> evaluate, CsEvalContext context)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDot && value is SpreadExpr spread)
            {
                var spreadValue = evaluate(spread.Expression);
                if (spreadValue is IDictionary<string, object?> dict)
                {
                    foreach (var kvp in dict)
                        result[kvp.Key] = kvp.Value;
                }
                else if (spreadValue != null)
                {
                    var type = spreadValue.GetType();
                    foreach (var prop in context.TypeCache.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.CanRead)
                            result[prop.Name] = context.TypeCache.GetPropertyValue(prop, spreadValue);
                    }
                }
            }
            else
            {
                result[key.Lexeme] = evaluate(value);
            }
        }
        return result;
    }
}
