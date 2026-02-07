using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.4.8 -- Lifted operator compliance tests for int?, long?, double?, decimal?, float?.
/// Covers arithmetic, equality, relational, bitwise, shift, unary, and three-value bool? logic.
/// Shared across compiler backends.
/// </summary>
public static class LiftedOperatorData
{
    /// <summary>
    /// Value-producing lifted operator expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.4.8 -- Lifted Arithmetic: int?
        new("{ int? a = null; int? b = 5; return a + b; }", null) { TestName = "LiftedArithmetic_Add_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_BothNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a + b; }", 8) { TestName = "LiftedArithmetic_Add_BothNonNull_ReturnsSum" },

        new("{ int? a = null; int? b = 5; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_BothNull_ReturnsNull" },
        new("{ int? a = 10; int? b = 3; return a - b; }", 7) { TestName = "LiftedArithmetic_Subtract_BothNonNull_ReturnsDifference" },

        new("{ int? a = null; int? b = 5; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_BothNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a * b; }", 15) { TestName = "LiftedArithmetic_Multiply_BothNonNull_ReturnsProduct" },

        new("{ int? a = null; int? b = 5; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_LeftNull_ReturnsNull" },
        new("{ int? a = 10; int? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_BothNull_ReturnsNull" },
        new("{ int? a = 10; int? b = 2; return a / b; }", 5) { TestName = "LiftedArithmetic_Divide_BothNonNull_ReturnsQuotient" },

        new("{ int? a = null; int? b = 5; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_LeftNull_ReturnsNull" },
        new("{ int? a = 10; int? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_BothNull_ReturnsNull" },
        new("{ int? a = 10; int? b = 3; return a % b; }", 1) { TestName = "LiftedArithmetic_Modulo_BothNonNull_ReturnsRemainder" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: long?
        new("{ long? a = null; long? b = 5L; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Long_LeftNull_ReturnsNull" },
        new("{ long? a = 5L; long? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Long_RightNull_ReturnsNull" },
        new("{ long? a = null; long? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Long_BothNull_ReturnsNull" },
        new("{ long? a = 5L; long? b = 3L; return a + b; }", 8L) { TestName = "LiftedArithmetic_Add_Long_BothNonNull_ReturnsSum" },

        new("{ long? a = null; long? b = 5L; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Long_LeftNull_ReturnsNull" },
        new("{ long? a = 5L; long? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Long_RightNull_ReturnsNull" },
        new("{ long? a = null; long? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Long_BothNull_ReturnsNull" },
        new("{ long? a = 10L; long? b = 3L; return a - b; }", 7L) { TestName = "LiftedArithmetic_Subtract_Long_BothNonNull_ReturnsDifference" },

        new("{ long? a = null; long? b = 5L; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Long_LeftNull_ReturnsNull" },
        new("{ long? a = 5L; long? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Long_RightNull_ReturnsNull" },
        new("{ long? a = null; long? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Long_BothNull_ReturnsNull" },
        new("{ long? a = 5L; long? b = 3L; return a * b; }", 15L) { TestName = "LiftedArithmetic_Multiply_Long_BothNonNull_ReturnsProduct" },

        new("{ long? a = null; long? b = 5L; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Long_LeftNull_ReturnsNull" },
        new("{ long? a = 10L; long? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Long_RightNull_ReturnsNull" },
        new("{ long? a = null; long? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Long_BothNull_ReturnsNull" },
        new("{ long? a = 10L; long? b = 2L; return a / b; }", 5L) { TestName = "LiftedArithmetic_Divide_Long_BothNonNull_ReturnsQuotient" },

        new("{ long? a = null; long? b = 5L; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Long_LeftNull_ReturnsNull" },
        new("{ long? a = 10L; long? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Long_RightNull_ReturnsNull" },
        new("{ long? a = null; long? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Long_BothNull_ReturnsNull" },
        new("{ long? a = 10L; long? b = 3L; return a % b; }", 1L) { TestName = "LiftedArithmetic_Modulo_Long_BothNonNull_ReturnsRemainder" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: double?
        new("{ double? a = null; double? b = 5.0; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Double_LeftNull_ReturnsNull" },
        new("{ double? a = 5.0; double? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Double_RightNull_ReturnsNull" },
        new("{ double? a = null; double? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Double_BothNull_ReturnsNull" },
        new("{ double? a = 5.0; double? b = 3.0; return a + b; }", 8.0) { TestName = "LiftedArithmetic_Add_Double_BothNonNull_ReturnsSum" },

        new("{ double? a = null; double? b = 5.0; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Double_LeftNull_ReturnsNull" },
        new("{ double? a = 5.0; double? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Double_RightNull_ReturnsNull" },
        new("{ double? a = null; double? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_Double_BothNull_ReturnsNull" },
        new("{ double? a = 10.0; double? b = 3.0; return a - b; }", 7.0) { TestName = "LiftedArithmetic_Subtract_Double_BothNonNull_ReturnsDifference" },

        new("{ double? a = null; double? b = 5.0; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Double_LeftNull_ReturnsNull" },
        new("{ double? a = 5.0; double? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Double_RightNull_ReturnsNull" },
        new("{ double? a = null; double? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Double_BothNull_ReturnsNull" },
        new("{ double? a = 5.0; double? b = 3.0; return a * b; }", 15.0) { TestName = "LiftedArithmetic_Multiply_Double_BothNonNull_ReturnsProduct" },

        new("{ double? a = null; double? b = 5.0; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Double_LeftNull_ReturnsNull" },
        new("{ double? a = 10.0; double? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Double_RightNull_ReturnsNull" },
        new("{ double? a = null; double? b = null; return a / b; }", null) { TestName = "LiftedArithmetic_Divide_Double_BothNull_ReturnsNull" },
        new("{ double? a = 10.0; double? b = 2.0; return a / b; }", 5.0) { TestName = "LiftedArithmetic_Divide_Double_BothNonNull_ReturnsQuotient" },

        new("{ double? a = null; double? b = 5.0; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Double_LeftNull_ReturnsNull" },
        new("{ double? a = 10.0; double? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Double_RightNull_ReturnsNull" },
        new("{ double? a = null; double? b = null; return a % b; }", null) { TestName = "LiftedArithmetic_Modulo_Double_BothNull_ReturnsNull" },
        new("{ double? a = 10.0; double? b = 3.0; return a % b; }", 1.0) { TestName = "LiftedArithmetic_Modulo_Double_BothNonNull_ReturnsRemainder" },

        // ECMA-334 S12.4.8 -- Lifted Equality: int?
        new("{ int? a = null; int? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_BothNull_ReturnsTrue" },
        new("{ int? a = null; int? b = 5; return a == b; }", false) { TestName = "LiftedEquality_Eq_LeftNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_RightNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 5; return a == b; }", true) { TestName = "LiftedEquality_Eq_EqualValues_ReturnsTrue" },
        new("{ int? a = 5; int? b = 3; return a == b; }", false) { TestName = "LiftedEquality_Eq_DifferentValues_ReturnsFalse" },

        new("{ int? a = null; int? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_BothNull_ReturnsFalse" },
        new("{ int? a = null; int? b = 5; return a != b; }", true) { TestName = "LiftedEquality_Neq_LeftNull_ReturnsTrue" },
        new("{ int? a = 5; int? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_RightNull_ReturnsTrue" },
        new("{ int? a = 5; int? b = 5; return a != b; }", false) { TestName = "LiftedEquality_Neq_EqualValues_ReturnsFalse" },
        new("{ int? a = 5; int? b = 3; return a != b; }", true) { TestName = "LiftedEquality_Neq_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: long?
        new("{ long? a = null; long? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Long_BothNull_ReturnsTrue" },
        new("{ long? a = null; long? b = 5L; return a == b; }", false) { TestName = "LiftedEquality_Eq_Long_LeftNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Long_RightNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = 5L; return a == b; }", true) { TestName = "LiftedEquality_Eq_Long_EqualValues_ReturnsTrue" },
        new("{ long? a = 5L; long? b = 3L; return a == b; }", false) { TestName = "LiftedEquality_Eq_Long_DifferentValues_ReturnsFalse" },

        new("{ long? a = null; long? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Long_BothNull_ReturnsFalse" },
        new("{ long? a = null; long? b = 5L; return a != b; }", true) { TestName = "LiftedEquality_Neq_Long_LeftNull_ReturnsTrue" },
        new("{ long? a = 5L; long? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Long_RightNull_ReturnsTrue" },
        new("{ long? a = 5L; long? b = 5L; return a != b; }", false) { TestName = "LiftedEquality_Neq_Long_EqualValues_ReturnsFalse" },
        new("{ long? a = 5L; long? b = 3L; return a != b; }", true) { TestName = "LiftedEquality_Neq_Long_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: double?
        new("{ double? a = null; double? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Double_BothNull_ReturnsTrue" },
        new("{ double? a = null; double? b = 5.0; return a == b; }", false) { TestName = "LiftedEquality_Eq_Double_LeftNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Double_RightNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = 5.0; return a == b; }", true) { TestName = "LiftedEquality_Eq_Double_EqualValues_ReturnsTrue" },
        new("{ double? a = 5.0; double? b = 3.0; return a == b; }", false) { TestName = "LiftedEquality_Eq_Double_DifferentValues_ReturnsFalse" },

        new("{ double? a = null; double? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Double_BothNull_ReturnsFalse" },
        new("{ double? a = null; double? b = 5.0; return a != b; }", true) { TestName = "LiftedEquality_Neq_Double_LeftNull_ReturnsTrue" },
        new("{ double? a = 5.0; double? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Double_RightNull_ReturnsTrue" },
        new("{ double? a = 5.0; double? b = 5.0; return a != b; }", false) { TestName = "LiftedEquality_Neq_Double_EqualValues_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = 3.0; return a != b; }", true) { TestName = "LiftedEquality_Neq_Double_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: decimal?
        new("{ decimal? a = null; decimal? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Decimal_BothNull_ReturnsTrue" },
        new("{ decimal? a = null; decimal? b = 5m; return a == b; }", false) { TestName = "LiftedEquality_Eq_Decimal_LeftNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Decimal_RightNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = 5m; return a == b; }", true) { TestName = "LiftedEquality_Eq_Decimal_EqualValues_ReturnsTrue" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a == b; }", false) { TestName = "LiftedEquality_Eq_Decimal_DifferentValues_ReturnsFalse" },

        new("{ decimal? a = null; decimal? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Decimal_BothNull_ReturnsFalse" },
        new("{ decimal? a = null; decimal? b = 5m; return a != b; }", true) { TestName = "LiftedEquality_Neq_Decimal_LeftNull_ReturnsTrue" },
        new("{ decimal? a = 5m; decimal? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Decimal_RightNull_ReturnsTrue" },
        new("{ decimal? a = 5m; decimal? b = 5m; return a != b; }", false) { TestName = "LiftedEquality_Neq_Decimal_EqualValues_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a != b; }", true) { TestName = "LiftedEquality_Neq_Decimal_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: float?
        new("{ float? a = null; float? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Float_BothNull_ReturnsTrue" },
        new("{ float? a = null; float? b = 5.0f; return a == b; }", false) { TestName = "LiftedEquality_Eq_Float_LeftNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Float_RightNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = 5.0f; return a == b; }", true) { TestName = "LiftedEquality_Eq_Float_EqualValues_ReturnsTrue" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a == b; }", false) { TestName = "LiftedEquality_Eq_Float_DifferentValues_ReturnsFalse" },

        new("{ float? a = null; float? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Float_BothNull_ReturnsFalse" },
        new("{ float? a = null; float? b = 5.0f; return a != b; }", true) { TestName = "LiftedEquality_Neq_Float_LeftNull_ReturnsTrue" },
        new("{ float? a = 5.0f; float? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Float_RightNull_ReturnsTrue" },
        new("{ float? a = 5.0f; float? b = 5.0f; return a != b; }", false) { TestName = "LiftedEquality_Neq_Float_EqualValues_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a != b; }", true) { TestName = "LiftedEquality_Neq_Float_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: int?
        new("{ int? a = null; int? b = 5; return a < b; }", false) { TestName = "LiftedRelational_LessThan_LeftNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_RightNull_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_BothNull_ReturnsFalse" },
        new("{ int? a = 3; int? b = 5; return a < b; }", true) { TestName = "LiftedRelational_LessThan_LessThan_ReturnsTrue" },
        new("{ int? a = 5; int? b = 3; return a < b; }", false) { TestName = "LiftedRelational_LessThan_GreaterThan_ReturnsFalse" },

        new("{ int? a = null; int? b = 5; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_LeftNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_RightNull_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_BothNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 3; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_GreaterThan_ReturnsTrue" },

        new("{ int? a = null; int? b = 5; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_LeftNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_RightNull_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_BothNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 5; return a <= b; }", true) { TestName = "LiftedRelational_LessOrEqual_Equal_ReturnsTrue" },

        new("{ int? a = null; int? b = 5; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_LeftNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_RightNull_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_BothNull_ReturnsFalse" },
        new("{ int? a = 5; int? b = 5; return a >= b; }", true) { TestName = "LiftedRelational_GreaterOrEqual_Equal_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: long?
        new("{ long? a = null; long? b = 5L; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Long_LeftNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Long_RightNull_ReturnsFalse" },
        new("{ long? a = null; long? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Long_BothNull_ReturnsFalse" },
        new("{ long? a = 3L; long? b = 5L; return a < b; }", true) { TestName = "LiftedRelational_LessThan_Long_LessThan_ReturnsTrue" },
        new("{ long? a = 5L; long? b = 3L; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Long_GreaterThan_ReturnsFalse" },

        new("{ long? a = null; long? b = 5L; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Long_LeftNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Long_RightNull_ReturnsFalse" },
        new("{ long? a = null; long? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Long_BothNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = 3L; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_Long_GreaterThan_ReturnsTrue" },

        new("{ long? a = null; long? b = 5L; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Long_LeftNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Long_RightNull_ReturnsFalse" },
        new("{ long? a = null; long? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Long_BothNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = 5L; return a <= b; }", true) { TestName = "LiftedRelational_LessOrEqual_Long_Equal_ReturnsTrue" },

        new("{ long? a = null; long? b = 5L; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Long_LeftNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Long_RightNull_ReturnsFalse" },
        new("{ long? a = null; long? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Long_BothNull_ReturnsFalse" },
        new("{ long? a = 5L; long? b = 5L; return a >= b; }", true) { TestName = "LiftedRelational_GreaterOrEqual_Long_Equal_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: double?
        new("{ double? a = null; double? b = 5.0; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Double_LeftNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Double_RightNull_ReturnsFalse" },
        new("{ double? a = null; double? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Double_BothNull_ReturnsFalse" },
        new("{ double? a = 3.0; double? b = 5.0; return a < b; }", true) { TestName = "LiftedRelational_LessThan_Double_LessThan_ReturnsTrue" },
        new("{ double? a = 5.0; double? b = 3.0; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Double_GreaterThan_ReturnsFalse" },

        new("{ double? a = null; double? b = 5.0; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Double_LeftNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Double_RightNull_ReturnsFalse" },
        new("{ double? a = null; double? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Double_BothNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = 3.0; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_Double_GreaterThan_ReturnsTrue" },

        new("{ double? a = null; double? b = 5.0; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Double_LeftNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Double_RightNull_ReturnsFalse" },
        new("{ double? a = null; double? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Double_BothNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = 5.0; return a <= b; }", true) { TestName = "LiftedRelational_LessOrEqual_Double_Equal_ReturnsTrue" },

        new("{ double? a = null; double? b = 5.0; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Double_LeftNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Double_RightNull_ReturnsFalse" },
        new("{ double? a = null; double? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Double_BothNull_ReturnsFalse" },
        new("{ double? a = 5.0; double? b = 5.0; return a >= b; }", true) { TestName = "LiftedRelational_GreaterOrEqual_Double_Equal_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: decimal?
        new("{ decimal? a = null; decimal? b = 5m; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Decimal_LeftNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Decimal_RightNull_ReturnsFalse" },
        new("{ decimal? a = null; decimal? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Decimal_BothNull_ReturnsFalse" },
        new("{ decimal? a = 3m; decimal? b = 5m; return a < b; }", true) { TestName = "LiftedRelational_LessThan_Decimal_LessThan_ReturnsTrue" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Decimal_GreaterThan_ReturnsFalse" },

        new("{ decimal? a = null; decimal? b = 5m; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Decimal_LeftNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Decimal_RightNull_ReturnsFalse" },
        new("{ decimal? a = null; decimal? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Decimal_BothNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_Decimal_GreaterThan_ReturnsTrue" },

        new("{ decimal? a = null; decimal? b = 5m; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Decimal_LeftNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Decimal_RightNull_ReturnsFalse" },
        new("{ decimal? a = null; decimal? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Decimal_BothNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = 5m; return a <= b; }", true) { TestName = "LiftedRelational_LessOrEqual_Decimal_Equal_ReturnsTrue" },

        new("{ decimal? a = null; decimal? b = 5m; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Decimal_LeftNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Decimal_RightNull_ReturnsFalse" },
        new("{ decimal? a = null; decimal? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Decimal_BothNull_ReturnsFalse" },
        new("{ decimal? a = 5m; decimal? b = 5m; return a >= b; }", true) { TestName = "LiftedRelational_GreaterOrEqual_Decimal_Equal_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: float?
        new("{ float? a = null; float? b = 5.0f; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Float_LeftNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Float_RightNull_ReturnsFalse" },
        new("{ float? a = null; float? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Float_BothNull_ReturnsFalse" },
        new("{ float? a = 3.0f; float? b = 5.0f; return a < b; }", true) { TestName = "LiftedRelational_LessThan_Float_LessThan_ReturnsTrue" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Float_GreaterThan_ReturnsFalse" },

        new("{ float? a = null; float? b = 5.0f; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Float_LeftNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Float_RightNull_ReturnsFalse" },
        new("{ float? a = null; float? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Float_BothNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_Float_GreaterThan_ReturnsTrue" },

        new("{ float? a = null; float? b = 5.0f; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Float_LeftNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Float_RightNull_ReturnsFalse" },
        new("{ float? a = null; float? b = null; return a <= b; }", false) { TestName = "LiftedRelational_LessOrEqual_Float_BothNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = 5.0f; return a <= b; }", true) { TestName = "LiftedRelational_LessOrEqual_Float_Equal_ReturnsTrue" },

        new("{ float? a = null; float? b = 5.0f; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Float_LeftNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Float_RightNull_ReturnsFalse" },
        new("{ float? a = null; float? b = null; return a >= b; }", false) { TestName = "LiftedRelational_GreaterOrEqual_Float_BothNull_ReturnsFalse" },
        new("{ float? a = 5.0f; float? b = 5.0f; return a >= b; }", true) { TestName = "LiftedRelational_GreaterOrEqual_Float_Equal_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Bitwise: int?
        new("{ int? a = null; int? b = 5; return a & b; }", null) { TestName = "LiftedBitwise_And_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a & b; }", null) { TestName = "LiftedBitwise_And_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a & b; }", null) { TestName = "LiftedBitwise_And_BothNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a & b; }", 1) { TestName = "LiftedBitwise_And_BothNonNull_ReturnsResult" },

        new("{ int? a = null; int? b = 5; return a | b; }", null) { TestName = "LiftedBitwise_Or_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a | b; }", null) { TestName = "LiftedBitwise_Or_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a | b; }", null) { TestName = "LiftedBitwise_Or_BothNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a | b; }", 7) { TestName = "LiftedBitwise_Or_BothNonNull_ReturnsResult" },

        new("{ int? a = null; int? b = 5; return a ^ b; }", null) { TestName = "LiftedBitwise_Xor_LeftNull_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a ^ b; }", null) { TestName = "LiftedBitwise_Xor_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a ^ b; }", null) { TestName = "LiftedBitwise_Xor_BothNull_ReturnsNull" },
        new("{ int? a = 5; int? b = 3; return a ^ b; }", 6) { TestName = "LiftedBitwise_Xor_BothNonNull_ReturnsResult" },

        // ECMA-334 S12.4.8 -- Lifted Shift: int?
        new("{ int? a = null; int? b = 2; return a << b; }", null) { TestName = "LiftedShift_Left_LeftNull_ReturnsNull" },
        new("{ int? a = 1; int? b = null; return a << b; }", null) { TestName = "LiftedShift_Left_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a << b; }", null) { TestName = "LiftedShift_Left_BothNull_ReturnsNull" },
        new("{ int? a = 1; int? b = 2; return a << b; }", 4) { TestName = "LiftedShift_Left_BothNonNull_ReturnsResult" },

        new("{ int? a = null; int? b = 2; return a >> b; }", null) { TestName = "LiftedShift_Right_LeftNull_ReturnsNull" },
        new("{ int? a = 8; int? b = null; return a >> b; }", null) { TestName = "LiftedShift_Right_RightNull_ReturnsNull" },
        new("{ int? a = null; int? b = null; return a >> b; }", null) { TestName = "LiftedShift_Right_BothNull_ReturnsNull" },
        new("{ int? a = 8; int? b = 2; return a >> b; }", 2) { TestName = "LiftedShift_Right_BothNonNull_ReturnsResult" },

        // ECMA-334 S12.4.8 -- Lifted Unary: int?
        new("{ int? a = null; return -a; }", null) { TestName = "LiftedUnary_Negate_Null_ReturnsNull" },
        new("{ int? a = 5; return -a; }", -5) { TestName = "LiftedUnary_Negate_NonNull_ReturnsNegated" },

        new("{ int? a = null; return +a; }", null) { TestName = "LiftedUnary_Plus_Null_ReturnsNull" },
        new("{ int? a = 5; return +a; }", 5) { TestName = "LiftedUnary_Plus_NonNull_ReturnsSame" },

        new("{ int? a = null; return ~a; }", null) { TestName = "LiftedUnary_BitwiseNot_Null_ReturnsNull" },
        new("{ int? a = 5; return ~a; }", -6) { TestName = "LiftedUnary_BitwiseNot_NonNull_ReturnsComplement" },

        new("{ bool? a = null; return !a; }", null) { TestName = "LiftedUnary_LogicalNot_Null_ReturnsNull" },
        new("{ bool? a = true; return !a; }", false) { TestName = "LiftedUnary_LogicalNot_True_ReturnsFalse" },
        new("{ bool? a = false; return !a; }", true) { TestName = "LiftedUnary_LogicalNot_False_ReturnsTrue" },

        // ECMA-334 S12.13.5 -- Three-Value bool? Logic: &
        new("{ bool? a = false; bool? b = null; return a & b; }", false) { TestName = "BoolThreeValue_And_FalseNull_ReturnsFalse" },
        new("{ bool? a = null; bool? b = false; return a & b; }", false) { TestName = "BoolThreeValue_And_NullFalse_ReturnsFalse" },
        new("{ bool? a = true; bool? b = null; return a & b; }", null) { TestName = "BoolThreeValue_And_TrueNull_ReturnsNull" },
        new("{ bool? a = null; bool? b = true; return a & b; }", null) { TestName = "BoolThreeValue_And_NullTrue_ReturnsNull" },
        new("{ bool? a = null; bool? b = null; return a & b; }", null) { TestName = "BoolThreeValue_And_NullNull_ReturnsNull" },
        new("{ bool? a = true; bool? b = true; return a & b; }", true) { TestName = "BoolThreeValue_And_TrueTrue_ReturnsTrue" },
        new("{ bool? a = true; bool? b = false; return a & b; }", false) { TestName = "BoolThreeValue_And_TrueFalse_ReturnsFalse" },
        new("{ bool? a = false; bool? b = false; return a & b; }", false) { TestName = "BoolThreeValue_And_FalseFalse_ReturnsFalse" },
        new("{ bool? a = false; bool? b = true; return a & b; }", false) { TestName = "BoolThreeValue_And_FalseTrue_ReturnsFalse" },

        // ECMA-334 S12.13.5 -- Three-Value bool? Logic: |
        new("{ bool? a = true; bool? b = null; return a | b; }", true) { TestName = "BoolThreeValue_Or_TrueNull_ReturnsTrue" },
        new("{ bool? a = null; bool? b = true; return a | b; }", true) { TestName = "BoolThreeValue_Or_NullTrue_ReturnsTrue" },
        new("{ bool? a = false; bool? b = null; return a | b; }", null) { TestName = "BoolThreeValue_Or_FalseNull_ReturnsNull" },
        new("{ bool? a = null; bool? b = false; return a | b; }", null) { TestName = "BoolThreeValue_Or_NullFalse_ReturnsNull" },
        new("{ bool? a = null; bool? b = null; return a | b; }", null) { TestName = "BoolThreeValue_Or_NullNull_ReturnsNull" },
        new("{ bool? a = true; bool? b = true; return a | b; }", true) { TestName = "BoolThreeValue_Or_TrueTrue_ReturnsTrue" },
        new("{ bool? a = true; bool? b = false; return a | b; }", true) { TestName = "BoolThreeValue_Or_TrueFalse_ReturnsTrue" },
        new("{ bool? a = false; bool? b = false; return a | b; }", false) { TestName = "BoolThreeValue_Or_FalseFalse_ReturnsFalse" },
        new("{ bool? a = false; bool? b = true; return a | b; }", true) { TestName = "BoolThreeValue_Or_FalseTrue_ReturnsTrue" },

        // ECMA-334 S12.13.5 -- Three-Value bool? Logic: ^
        new("{ bool? a = null; bool? b = null; return a ^ b; }", null) { TestName = "BoolThreeValue_Xor_NullNull_ReturnsNull" },
        new("{ bool? a = true; bool? b = null; return a ^ b; }", null) { TestName = "BoolThreeValue_Xor_TrueNull_ReturnsNull" },
        new("{ bool? a = null; bool? b = false; return a ^ b; }", null) { TestName = "BoolThreeValue_Xor_NullFalse_ReturnsNull" },
        new("{ bool? a = true; bool? b = false; return a ^ b; }", true) { TestName = "BoolThreeValue_Xor_TrueFalse_ReturnsTrue" },
        new("{ bool? a = true; bool? b = true; return a ^ b; }", false) { TestName = "BoolThreeValue_Xor_TrueTrue_ReturnsFalse" },
        new("{ bool? a = false; bool? b = false; return a ^ b; }", false) { TestName = "BoolThreeValue_Xor_FalseFalse_ReturnsFalse" },
        new("{ bool? a = false; bool? b = true; return a ^ b; }", true) { TestName = "BoolThreeValue_Xor_FalseTrue_ReturnsTrue" },
        new("{ bool? a = null; bool? b = true; return a ^ b; }", null) { TestName = "BoolThreeValue_Xor_NullTrue_ReturnsNull" },
        new("{ bool? a = false; bool? b = null; return a ^ b; }", null) { TestName = "BoolThreeValue_Xor_FalseNull_ReturnsNull" },
    ];

    /// <summary>
    /// Parity-only lifted operator expressions (no expected value -- types not representable in TestCase).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        // ECMA-334 S12.4.8 -- Lifted Arithmetic: decimal? (decimal literals not supported in TestCase)
        new("{ decimal? a = null; decimal? b = 5m; return a + b; }") { TestName = "LiftedArithmetic_Add_Decimal_LeftNull_ReturnsNull" },
        new("{ decimal? a = 5m; decimal? b = null; return a + b; }") { TestName = "LiftedArithmetic_Add_Decimal_RightNull_ReturnsNull" },
        new("{ decimal? a = null; decimal? b = null; return a + b; }") { TestName = "LiftedArithmetic_Add_Decimal_BothNull_ReturnsNull" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a + b; }") { TestName = "LiftedArithmetic_Add_Decimal_BothNonNull_ReturnsSum" },

        new("{ decimal? a = null; decimal? b = 5m; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Decimal_LeftNull_ReturnsNull" },
        new("{ decimal? a = 5m; decimal? b = null; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Decimal_RightNull_ReturnsNull" },
        new("{ decimal? a = null; decimal? b = null; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Decimal_BothNull_ReturnsNull" },
        new("{ decimal? a = 10m; decimal? b = 3m; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Decimal_BothNonNull_ReturnsDifference" },

        new("{ decimal? a = null; decimal? b = 5m; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Decimal_LeftNull_ReturnsNull" },
        new("{ decimal? a = 5m; decimal? b = null; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Decimal_RightNull_ReturnsNull" },
        new("{ decimal? a = null; decimal? b = null; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Decimal_BothNull_ReturnsNull" },
        new("{ decimal? a = 5m; decimal? b = 3m; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Decimal_BothNonNull_ReturnsProduct" },

        new("{ decimal? a = null; decimal? b = 5m; return a / b; }") { TestName = "LiftedArithmetic_Divide_Decimal_LeftNull_ReturnsNull" },
        new("{ decimal? a = 10m; decimal? b = null; return a / b; }") { TestName = "LiftedArithmetic_Divide_Decimal_RightNull_ReturnsNull" },
        new("{ decimal? a = null; decimal? b = null; return a / b; }") { TestName = "LiftedArithmetic_Divide_Decimal_BothNull_ReturnsNull" },
        new("{ decimal? a = 10m; decimal? b = 2m; return a / b; }") { TestName = "LiftedArithmetic_Divide_Decimal_BothNonNull_ReturnsQuotient" },

        new("{ decimal? a = null; decimal? b = 5m; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Decimal_LeftNull_ReturnsNull" },
        new("{ decimal? a = 10m; decimal? b = null; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Decimal_RightNull_ReturnsNull" },
        new("{ decimal? a = null; decimal? b = null; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Decimal_BothNull_ReturnsNull" },
        new("{ decimal? a = 10m; decimal? b = 3m; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Decimal_BothNonNull_ReturnsRemainder" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: float? (float literals not supported in TestCase)
        new("{ float? a = null; float? b = 5.0f; return a + b; }") { TestName = "LiftedArithmetic_Add_Float_LeftNull_ReturnsNull" },
        new("{ float? a = 5.0f; float? b = null; return a + b; }") { TestName = "LiftedArithmetic_Add_Float_RightNull_ReturnsNull" },
        new("{ float? a = null; float? b = null; return a + b; }") { TestName = "LiftedArithmetic_Add_Float_BothNull_ReturnsNull" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a + b; }") { TestName = "LiftedArithmetic_Add_Float_BothNonNull_ReturnsSum" },

        new("{ float? a = null; float? b = 5.0f; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Float_LeftNull_ReturnsNull" },
        new("{ float? a = 5.0f; float? b = null; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Float_RightNull_ReturnsNull" },
        new("{ float? a = null; float? b = null; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Float_BothNull_ReturnsNull" },
        new("{ float? a = 10.0f; float? b = 3.0f; return a - b; }") { TestName = "LiftedArithmetic_Subtract_Float_BothNonNull_ReturnsDifference" },

        new("{ float? a = null; float? b = 5.0f; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Float_LeftNull_ReturnsNull" },
        new("{ float? a = 5.0f; float? b = null; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Float_RightNull_ReturnsNull" },
        new("{ float? a = null; float? b = null; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Float_BothNull_ReturnsNull" },
        new("{ float? a = 5.0f; float? b = 3.0f; return a * b; }") { TestName = "LiftedArithmetic_Multiply_Float_BothNonNull_ReturnsProduct" },

        new("{ float? a = null; float? b = 5.0f; return a / b; }") { TestName = "LiftedArithmetic_Divide_Float_LeftNull_ReturnsNull" },
        new("{ float? a = 10.0f; float? b = null; return a / b; }") { TestName = "LiftedArithmetic_Divide_Float_RightNull_ReturnsNull" },
        new("{ float? a = null; float? b = null; return a / b; }") { TestName = "LiftedArithmetic_Divide_Float_BothNull_ReturnsNull" },
        new("{ float? a = 10.0f; float? b = 2.0f; return a / b; }") { TestName = "LiftedArithmetic_Divide_Float_BothNonNull_ReturnsQuotient" },

        new("{ float? a = null; float? b = 5.0f; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Float_LeftNull_ReturnsNull" },
        new("{ float? a = 10.0f; float? b = null; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Float_RightNull_ReturnsNull" },
        new("{ float? a = null; float? b = null; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Float_BothNull_ReturnsNull" },
        new("{ float? a = 10.0f; float? b = 3.0f; return a % b; }") { TestName = "LiftedArithmetic_Modulo_Float_BothNonNull_ReturnsRemainder" },
    ];
}
