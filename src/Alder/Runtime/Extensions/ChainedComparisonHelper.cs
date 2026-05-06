using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Evaluates chained comparisons: 0 &lt; x &lt; 10, a &lt;= b &lt;= c, etc.
/// Each middle operand is evaluated exactly once. Short-circuits on first false.
/// Inspired by Python/Julia mathematical comparison chaining.
/// </summary>
internal static class ChainedComparisonHelper
{
    /// <summary>
    /// Performs a pairwise comparison using the specified operator token type.
    /// Delegates to the same Operators methods used by standard binary comparisons.
    /// </summary>
    public static bool PerformComparison(object? left, object? right, TokenType opType, StringComparison comparison)
    {
        var result = opType switch
        {
            TokenType.Less => Operators.LessThan(left, right, comparison),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, comparison),
            TokenType.Greater => Operators.GreaterThan(left, right, comparison),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, comparison),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            _ => throw new AlderException(DiagnosticDescriptors.UnsupportedChainedComparisonOperator, opType)
        };

        return (bool)result;
    }

    /// <summary>
    /// Evaluates a chained comparison from pre-evaluated operands and operator token types.
    /// Used by the IL compiler to call at runtime after operands are evaluated.
    /// Operands must already be evaluated; this method only performs the comparisons.
    /// </summary>
    public static object EvaluateChain(object?[] operands, int[] operatorTypes, StringComparison comparison)
    {
        for (int i = 0; i < operatorTypes.Length; i++)
        {
            if (!PerformComparison(operands[i], operands[i + 1], (TokenType)operatorTypes[i], comparison))
                return BoxedConstants.False;
        }

        return BoxedConstants.True;
    }
}
