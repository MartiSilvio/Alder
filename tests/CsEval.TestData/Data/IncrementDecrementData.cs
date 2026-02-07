using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.8.16, S12.8.17 -- Prefix and postfix increment/decrement operators.
/// Covers pre/post increment/decrement, loop usage, multiple operations, edge cases.
/// Shared across compiler backends.
/// </summary>
public static class IncrementDecrementData
{
    /// <summary>
    /// Value-producing increment/decrement expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // Prefix Increment
        new("{ var x = 5; var y = ++x; return y; }", 6) { TestName = "PrefixIncrement_Integer_IncrementsAndReturnsNewValue" },
        new("{ var x = 10; ++x; return x; }", 11) { TestName = "PrefixIncrement_UpdatesVariable" },
        new("{ var x = 3.5; var y = ++x; return y; }", 4.5) { TestName = "PrefixIncrement_Double_WorksCorrectly" },

        // Postfix Increment
        new("{ var x = 5; var y = x++; return y; }", 5) { TestName = "PostfixIncrement_Integer_ReturnsOldValue" },
        new("{ var x = 5; x++; return x; }", 6) { TestName = "PostfixIncrement_UpdatesVariable" },
        new("""
            {
                var x = 10;
                var captured = x++;
                return captured + x;
            }
            """, 21) { TestName = "PostfixIncrement_ReturnsDifferentThanVariable" },
        new("{ var x = 1.5; var old = x++; return old; }", 1.5) { TestName = "PostfixIncrement_Double_WorksCorrectly" },

        // Prefix Decrement
        new("{ var x = 10; var y = --x; return y; }", 9) { TestName = "PrefixDecrement_Integer_DecrementsAndReturnsNewValue" },
        new("{ var x = 5; --x; return x; }", 4) { TestName = "PrefixDecrement_UpdatesVariable" },
        new("{ var x = 0; --x; return x; }", -1) { TestName = "PrefixDecrement_ToNegative_WorksCorrectly" },
        new("{ var x = 5.5; var y = --x; return y; }", 4.5) { TestName = "PrefixDecrement_Double_WorksCorrectly" },

        // Postfix Decrement
        new("{ var x = 10; var y = x--; return y; }", 10) { TestName = "PostfixDecrement_Integer_ReturnsOldValue" },
        new("{ var x = 10; x--; return x; }", 9) { TestName = "PostfixDecrement_UpdatesVariable" },
        new("""
            {
                var x = 15;
                var captured = x--;
                return captured + x;
            }
            """, 29) { TestName = "PostfixDecrement_ReturnsDifferentThanVariable" },

        // In Loops
        new("""
            {
                var sum = 0;
                for (var i = 0; i < 5; ++i) {
                    sum += i;
                }
                return sum;
            }
            """, 10) { TestName = "PrefixIncrement_InForLoop_WorksCorrectly" },
        new("""
            {
                var sum = 0;
                for (var i = 0; i < 5; i++) {
                    sum += i;
                }
                return sum;
            }
            """, 10) { TestName = "PostfixIncrement_InForLoop_WorksCorrectly" },
        new("""
            {
                var count = 5;
                var sum = 0;
                while (count > 0) {
                    sum += --count;
                }
                return sum;
            }
            """, 10) { TestName = "PrefixDecrement_InWhileLoop_WorksCorrectly" },
        new("""
            {
                var count = 5;
                var sum = 0;
                while (count > 0) {
                    sum += count--;
                }
                return sum;
            }
            """, 15) { TestName = "PostfixDecrement_InWhileLoop_WorksCorrectly" },
        new("""
            {
                var i = 0;
                var sum = 0;
                do {
                    sum += i++;
                } while (i < 4);
                return sum;
            }
            """, 6) { TestName = "IncrementDecrement_InDoWhileLoop_WorksCorrectly" },

        // Multiple Operations
        new("{ var x = 0; x++; x++; x++; return x; }", 3) { TestName = "MultipleIncrements_OnSameVariable" },
        new("{ var x = 10; x--; x--; x--; return x; }", 7) { TestName = "MultipleDecrements_OnSameVariable" },
        new("{ var x = 5; x++; x--; ++x; --x; return x; }", 5) { TestName = "MixedIncrementDecrement_OnSameVariable" },
        new("""
            {
                var a = 0;
                var b = 10;
                a++;
                b--;
                a++;
                b--;
                return a + b;
            }
            """, 10) { TestName = "IncrementDecrement_MultipleVariables" },

        // With Other Operations
        new("{ var x = 5; var y = x++ + 10; return y; }", 15) { TestName = "Increment_WithAddition" },
        new("{ var x = 5; var y = ++x + 10; return y; }", 16) { TestName = "PrefixIncrement_WithAddition" },
        new("{ var x = 5; x++; x += 10; return x; }", 16) { TestName = "Increment_WithCompoundAssignment" },
        new("""
            {
                var x = 5;
                if (x++ > 4) {
                    return x;
                }
                return 0;
            }
            """, 6) { TestName = "Increment_WithConditional" },

        // Edge Cases
        new("{ var x = 0; return ++x; }", 1) { TestName = "Increment_FromZero" },
        new("{ var x = 1; return --x; }", 0) { TestName = "Decrement_ToZero" },
        new("{ var x = -5; return ++x; }", -4) { TestName = "Increment_NegativeNumber" },
        new("{ var x = -5; return --x; }", -6) { TestName = "Decrement_NegativeNumber" },

        // Prefix vs Postfix Comparison
        new("""
            {
                var a = 5;
                var b = 5;
                var prefixResult = ++a;
                var postfixResult = b++;
                return prefixResult - postfixResult;
            }
            """, 1) { TestName = "PrefixVsPostfix_InExpression" },
        new("""
            {
                var a = 5;
                var b = 5;
                ++a;
                b++;
                return a == b;
            }
            """, true) { TestName = "PrefixVsPostfix_SameEndValue" },
        new("""
            {
                var a = 10;
                var b = 10;
                var prefixResult = --a;
                var postfixResult = b--;
                return prefixResult + postfixResult;
            }
            """, 19) { TestName = "PrefixVsPostfix_DecrementComparison" },
    ];

    /// <summary>
    /// Parity-only increment/decrement expressions (no expected value -- types not representable in TestCase).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        // Long type prefix increment
        new("{ long x = 9000000000; ++x; return x; }") { TestName = "PrefixIncrement_Long_WorksCorrectly" },
        new("{ decimal x = 99.99m; ++x; return x; }") { TestName = "PrefixIncrement_Decimal_WorksCorrectly" },

        // Long type postfix increment
        new("{ long x = 5000000000; var old = x++; return old; }") { TestName = "PostfixIncrement_Long_WorksCorrectly" },

        // Long type prefix decrement
        new("{ long x = 9000000001; --x; return x; }") { TestName = "PrefixDecrement_Long_WorksCorrectly" },

        // Very large number increment
        new("{ long x = 9223372036854775806; ++x; return x; }") { TestName = "Increment_VeryLargeNumber" },
    ];
}
