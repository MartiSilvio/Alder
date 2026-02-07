using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.21 -- Compound assignment operators (+=, -=, *=, /=, %=, &=, |=, ^=, <<=, >>=).
/// Covers arithmetic, bitwise, string concatenation, loops, conditionals, edge cases.
/// Shared across compiler backends.
/// </summary>
public static class CompoundAssignmentData
{
    /// <summary>
    /// Value-producing compound assignment expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // Basic Arithmetic
        new("{ var x = 10; x += 5; return x; }", 15) { TestName = "CompoundAssignment_PlusEquals_Integer_WorksCorrectly" },
        new("{ var x = 20; x -= 8; return x; }", 12) { TestName = "CompoundAssignment_MinusEquals_Integer_WorksCorrectly" },
        new("{ var x = 6; x *= 7; return x; }", 42) { TestName = "CompoundAssignment_StarEquals_Integer_WorksCorrectly" },
        new("{ var x = 100.0; x /= 4; return x; }", 25.0) { TestName = "CompoundAssignment_SlashEquals_Double_WorksCorrectly" },
        new("{ var x = 17; x %= 5; return x; }", 2) { TestName = "CompoundAssignment_PercentEquals_Integer_WorksCorrectly" },

        // Bitwise Operators
        new("{ var x = 15; x &= 9; return x; }", 9) { TestName = "CompoundAssignment_AmpEquals_BitwiseAnd_WorksCorrectly" },
        new("{ var x = 5; x |= 3; return x; }", 7) { TestName = "CompoundAssignment_PipeEquals_BitwiseOr_WorksCorrectly" },
        new("{ var x = 12; x ^= 5; return x; }", 9) { TestName = "CompoundAssignment_CaretEquals_BitwiseXor_WorksCorrectly" },
        new("{ var x = 1; x <<= 4; return x; }", 16) { TestName = "CompoundAssignment_LessLessEquals_LeftShift_WorksCorrectly" },
        new("{ var x = 32; x >>= 2; return x; }", 8) { TestName = "CompoundAssignment_GreaterGreaterEquals_RightShift_WorksCorrectly" },

        // String Concatenation
        new("{ var s = \"Hello\"; s += \" World\"; return s; }", "Hello World") { TestName = "CompoundAssignment_StringPlusEquals_Concatenates" },
        new("{ var s = \"A\"; s += \"B\"; s += \"C\"; s += \"D\"; return s; }", "ABCD") { TestName = "CompoundAssignment_StringPlusEquals_Multiple" },
        new("{ var s = \"Count: \"; s += 42; return s; }", "Count: 42") { TestName = "CompoundAssignment_StringPlusEquals_WithNumber" },

        // In Loops
        new("""
            {
                var sum = 0;
                var i = 1;
                while (i <= 5) {
                    sum += i;
                    i += 1;
                }
                return sum;
            }
            """, 15) { TestName = "CompoundAssignment_InWhileLoop_Accumulates" },
        new("""
            {
                var product = 1;
                for (var i = 1; i <= 5; i += 1) {
                    product *= i;
                }
                return product;
            }
            """, 120) { TestName = "CompoundAssignment_InForLoop_Accumulates" },
        new("""
            {
                var sum = 0;
                var i = 1;
                do {
                    sum += i;
                    i += 1;
                } while (i <= 3);
                return sum;
            }
            """, 6) { TestName = "CompoundAssignment_InDoWhileLoop_Accumulates" },

        // With Expressions
        new("""
            {
                var x = 10;
                var y = 5;
                x += y * 2;
                return x;
            }
            """, 20) { TestName = "CompoundAssignment_WithExpressionRHS_WorksCorrectly" },
        new("""
            {
                var x = 10.0;
                x += Math.Abs(-5);
                return x;
            }
            """, 15.0) { TestName = "CompoundAssignment_WithMethodCallRHS_WorksCorrectly" },

        // Chained Operations
        new("{ var x = 5; var y = x += 10; return y; }", 15) { TestName = "CompoundAssignment_ReturnsNewValue" },
        new("""
            {
                var a = 10;
                var b = 20;
                var c = 30;
                a += 1;
                b += 2;
                c += 3;
                return a + b + c;
            }
            """, 66) { TestName = "CompoundAssignment_MultipleVariables_Independent" },

        // In Conditionals
        new("""
            {
                var x = 10;
                if (true) {
                    x += 5;
                }
                return x;
            }
            """, 15) { TestName = "CompoundAssignment_InsideIfBlock_WorksCorrectly" },
        new("""
            {
                var x = 10;
                if (false) {
                    x += 100;
                } else {
                    x += 5;
                }
                return x;
            }
            """, 15) { TestName = "CompoundAssignment_InsideElseBlock_WorksCorrectly" },

        // Edge Cases
        new("{ var x = 0; x += 0; x -= 0; x *= 0; return x; }", 0) { TestName = "CompoundAssignment_ZeroValue_WorksCorrectly" },
        new("{ var x = -10; x += -5; return x; }", -15) { TestName = "CompoundAssignment_NegativeNumbers_WorksCorrectly" },

        new("""
            {
                var x = 255;
                x <<= 0;
                var noShift = x;
                x = 1;
                x <<= 63;
                var maxShift = x;
                return noShift;
            }
            """, 255) { TestName = "CompoundAssignment_ShiftOperations_EdgeCases" },
    ];

    /// <summary>
    /// Parity-only compound assignment expressions (no expected value -- types not representable in TestCase).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        // Long and Decimal types
        new("{ long x = 10000000000; x += 5000000000; return x; }") { TestName = "CompoundAssignment_Long_WorksCorrectly" },
        new("{ decimal x = 100.50m; x -= 25.25m; return x; }") { TestName = "CompoundAssignment_Decimal_WorksCorrectly" },

        // Multiple operators on same variable (result type depends on intermediate arithmetic)
        new("""
            {
                var x = 100.0;
                x += 50;
                x -= 25;
                x *= 2;
                x /= 4;
                return x;
            }
            """) { TestName = "CompoundAssignment_MultipleOnSameVariable_WorksCorrectly" },

        // All arithmetic and bitwise operators comprehensive
        new("""
            {
                var x = 100.0;
                x += 50;
                var afterPlus = x;
                x -= 25;
                var afterMinus = x;
                x *= 2;
                var afterMult = x;
                x /= 5;
                var afterDiv = x;
                x = 17;
                x %= 5;
                var afterMod = x;
                return afterPlus + afterMinus + afterMult + afterDiv + afterMod;
            }
            """) { TestName = "CompoundAssignment_AllArithmeticOperators_WorkCorrectly" },
        new("""
            {
                var andResult = 15;
                andResult &= 9;
                var orResult = 5;
                orResult |= 3;
                var xorResult = 12;
                xorResult ^= 5;
                var leftShift = 1;
                leftShift <<= 4;
                var rightShift = 32;
                rightShift >>= 2;
                return andResult + orResult + xorResult + leftShift + rightShift;
            }
            """) { TestName = "CompoundAssignment_AllBitwiseOperators_WorkCorrectly" },

        // Very large numbers
        new("{ var x = 9000000000000000000L; x += 1; return x; }") { TestName = "CompoundAssignment_VeryLargeNumbers_WorksCorrectly" },
    ];
}
