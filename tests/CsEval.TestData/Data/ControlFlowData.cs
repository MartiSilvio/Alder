using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.18 -- Conditional operator (?:), S13.6 -- Selection statements (if/else).
/// Test data for ternary expressions, if/else statements, block expressions with return,
/// conditional type promotion (S12.4.7.3), and nested ternary evaluation.
/// Shared across compiler backends.
/// </summary>
public static class ControlFlowData
{
    /// <summary>
    /// Ternary, if/else, block expressions, type promotion, and nested ternary cases.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.18, S13.6 -- Ternary, If/Else, and Block Expressions
        new("true ? 1 : 2", 1) { TestName = "Ternary_TrueBranch" },
        new("false ? 1 : 2", 2) { TestName = "Ternary_FalseBranch" },
        new("{ var x = 5; return x * 2; }", 10) { TestName = "Block_WithReturn" },
        new("{ object x = null; if (x == null) return 42; return 0; }", 42) { TestName = "If_EarlyReturnWhenTrue" },
        new("{ var x = 10; if (x == null) return 42; return 0; }", 0) { TestName = "If_EarlyReturnWhenFalse" },
        new("{ var x = true; if (x) return 1; else return 2; }", 1) { TestName = "IfElse_TakesThenBranch" },
        new("{ var x = false; if (x) return 1; else return 2; }", 2) { TestName = "IfElse_TakesElseBranch" },
        new("""
            {
                var x = 5;
                if (x > 3) {
                    var y = x * 2;
                    return y;
                }
                return 0;
            }
            """,
            10) { TestName = "If_WithBlock" },
        new("""
            {
                var x = 1;
                if (x > 3) {
                    return 100;
                } else {
                    return 200;
                }
            }
            """,
            200) { TestName = "If_WithElseBlock" },
        new("""
            {
                var x = 10;
                var y = 20;
                if (x > 5) {
                    if (y > 15) return 1;
                    else return 2;
                }
                return 3;
            }
            """,
            1) { TestName = "If_Nested" },
        new("""
            {
                var x = 2;
                if (x == 1) return "one";
                else if (x == 2) return "two";
                else return "other";
            }
            """,
            "two") { TestName = "If_ElseIf" },
        new("""
            {
                var x = 5;
                if (x < 3) {
                    return 0;
                }
                return x * 2;
            }
            """,
            10) { TestName = "If_NoReturnContinuesExecution" },

        // ECMA-334 S12.18 -- Conditional Operator Type Inference
        new("true ? 1 : 2L", 1L) { TestName = "Ternary_IntLong_PromotesToLong" },
        new("false ? 1 : 2L", 2L) { TestName = "Ternary_IntLong_FalseBranch" },
        new("true ? 1.0f : 2.0", 1.0) { TestName = "Ternary_FloatDouble_PromotesToDouble" },
        new("true ? null : \"hello\"", null) { TestName = "Ternary_NullString_ReturnsNull" },
        new("false ? null : \"hello\"", "hello") { TestName = "Ternary_NullString_ReturnsString" },
        new("true ? 1 : 2", 1) { TestName = "Ternary_SameType_NoPromotion" },

        // ECMA-334 S12.18 -- Nested Ternary
        new("true ? true ? 1 : 2 : 3", 1) { TestName = "Ternary_Nested_InnerTrue" },
        new("true ? false ? 1 : 2 : 3", 2) { TestName = "Ternary_Nested_InnerFalse" },
        new("false ? 1 : true ? 2 : 3", 2) { TestName = "Ternary_Nested_ElseBranch" },
    ];
}
