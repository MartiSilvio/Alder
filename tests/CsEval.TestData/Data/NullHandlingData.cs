using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.17 -- Null coalescing operator (??), S12.21 -- Null coalescing assignment (??=),
/// S12.8.8 -- Null-conditional member access (?.), S12.8.12 -- Null-conditional element access (?[]),
/// S12.4.8 -- Lifted operators for nullable value types.
/// Shared across compiler backends.
/// </summary>
public static class NullHandlingData
{
    /// <summary>
    /// Value-producing null handling expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.21 -- Null Coalescing Assignment (??=)
        new("""
            {
                int? x = null;
                x ??= 42;
                return x;
            }
            """, 42) { TestName = "NullCoalesceAssign_AssignsWhenNull" },
        new("""
            {
                int? x = null;
                return x ??= 42;
            }
            """, 42) { TestName = "NullCoalesceAssign_ReturnsAssignedValue" },
        new("""
            {
                var x = "hello";
                return x ??= "world";
            }
            """, "hello") { TestName = "NullCoalesceAssign_ReturnsExistingValue" },
        new("""
            {
                int? x = null;
                x ??= 5 + 5;
                return x;
            }
            """, 10) { TestName = "NullCoalesceAssign_WithExpression" },
        new("""
            {
                int? x = null;
                if (true) {
                    x ??= 100;
                }
                return x;
            }
            """, 100) { TestName = "NullCoalesceAssign_InIfStatement" },

        // ECMA-334 S12.17 -- Null Coalescing Operator (??) Right Associativity
        new("null ?? null ?? \"c\"", "c") { TestName = "NullCoalesce_Chained_BothNull" },
        new("null ?? \"b\" ?? \"c\"", "b") { TestName = "NullCoalesce_Chained_FirstNull" },
        new("\"a\" ?? \"b\" ?? \"c\"", "a") { TestName = "NullCoalesce_Chained_NoneNull" },
        new("null ?? null ?? null ?? \"d\"", "d") { TestName = "NullCoalesce_Chained_ThreeNulls" },

        // ECMA-334 S12.4.8 -- Lifted Operators (Nullable) Arithmetic
        new("{ int? a = 5; int? b = null; return a + b; }", null) { TestName = "LiftedAdd_WithNull_ReturnsNull" },
        new("{ int? a = null; int? b = 3; return a * b; }", null) { TestName = "LiftedMultiply_WithNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a + b; }", 8) { TestName = "LiftedAdd_BothNonNull_ReturnsSum" },
        new("{ int? a = null; int? b = null; return a + b; }", null) { TestName = "LiftedAdd_BothNull_ReturnsNull" },

        // ECMA-334 S12.4.8 -- Lifted Operators (Nullable) Comparison
        new("{ int? a = 5; int? b = null; return a > b; }", false) { TestName = "LiftedComparison_WithNull_ReturnsFalse" },
        new("{ int? a = null; int? b = 3; return a < b; }", false) { TestName = "LiftedLessThan_WithNull_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a >= b; }", false) { TestName = "LiftedGreaterEqual_BothNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 3; return a > b; }", true) { TestName = "LiftedGreater_BothNonNull_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Operators (Nullable) Equality
        new("{ int? a = null; int? b = null; return a == b; }", true) { TestName = "LiftedEquality_BothNull_ReturnsTrue" },
        new("{ int? a = 5; int? b = null; return a == b; }", false) { TestName = "LiftedEquality_OneNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 5; return a == b; }", true) { TestName = "LiftedEquality_Equal_ReturnsTrue" },
        new("{ int? a = 5; int? b = 3; return a != b; }", true) { TestName = "LiftedInequality_NotEqual_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Bitwise Operators
        new("{ int? a = 5; int? b = null; return a & b; }", null) { TestName = "LiftedBitwiseAnd_WithNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a & b; }", 1) { TestName = "LiftedBitwiseAnd_BothNonNull_ReturnsResult" },
        new("{ int? a = 5; int? b = null; return a | b; }", null) { TestName = "LiftedBitwiseOr_WithNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a | b; }", 7) { TestName = "LiftedBitwiseOr_BothNonNull_ReturnsResult" },
        new("{ int? a = 5; int? b = null; return a ^ b; }", null) { TestName = "LiftedBitwiseXor_WithNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a ^ b; }", 6) { TestName = "LiftedBitwiseXor_BothNonNull_ReturnsResult" },
        new("{ int? a = null; return ~a; }", null) { TestName = "LiftedBitwiseNot_Null_ReturnsNull" },
        new("{ int? a = 5; return ~a; }", -6) { TestName = "LiftedBitwiseNot_NonNull_ReturnsResult" },

        // ECMA-334 S12.4.8 -- Lifted Shift Operators
        new("{ int? a = 1; int? b = null; return a << b; }", null) { TestName = "LiftedLeftShift_NullCount_ReturnsNull" },
        new("{ int? a = null; return a << 2; }", null) { TestName = "LiftedLeftShift_NullValue_ReturnsNull" },
        new("{ int? a = 1; return a << 2; }", 4) { TestName = "LiftedLeftShift_NonNull_ReturnsResult" },
        new("{ int? a = 8; int? b = null; return a >> b; }", null) { TestName = "LiftedRightShift_NullCount_ReturnsNull" },
        new("{ int? a = 8; return a >> 2; }", 2) { TestName = "LiftedRightShift_NonNull_ReturnsResult" },

        // ECMA-334 S12.4.8 -- Lifted Unary Operators
        new("{ int? a = null; return -a; }", null) { TestName = "LiftedUnaryMinus_Null_ReturnsNull" },
        new("{ int? a = 5; return -a; }", -5) { TestName = "LiftedUnaryMinus_NonNull_ReturnsResult" },
        new("{ int? a = null; return +a; }", null) { TestName = "LiftedUnaryPlus_Null_ReturnsNull" },
        new("{ int? a = 5; return +a; }", 5) { TestName = "LiftedUnaryPlus_NonNull_ReturnsResult" },

        // ECMA-334 S12.4.2 -- Precedence with Null-Coalescing and Ternary
        new("{ string? n = null; return true ? n ?? \"fallback\" : \"else\"; }", "fallback") { TestName = "Precedence_TernaryWithNullCoalesce_InTrue" },
        new("{ string? n = null; return false ? \"then\" : n ?? \"fallback\"; }", "fallback") { TestName = "Precedence_TernaryWithNullCoalesce_InFalse" },

        // ECMA-334 S12.8.8 -- Null-Conditional with Null Coalescing
        new("{ string? s = null; return s?.Length ?? 0; }", 0) { TestName = "NullConditional_WithNullCoalesce_Null" },
        new("{ string? s = \"hello\"; return s?.Length ?? 0; }", 5) { TestName = "NullConditional_WithNullCoalesce_NonNull" },
    ];
}
