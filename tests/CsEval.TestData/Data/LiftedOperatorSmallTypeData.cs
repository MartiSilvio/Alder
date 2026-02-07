using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.4.8 -- Lifted operator compliance tests for small and unsigned types.
/// byte?, sbyte?, short?, ushort? promote to int for arithmetic (S12.4.7).
/// uint?, ulong? have their own predefined operators.
/// Shared across compiler backends.
/// </summary>
public static class LiftedOperatorSmallTypeData
{
    /// <summary>
    /// Value-producing lifted operator expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.4.8 -- Lifted Arithmetic: uint? (null cases)
        new("{ uint? a = null; uint? b = 5u; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UInt_LeftNull_ReturnsNull" },
        new("{ uint? a = 5u; uint? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UInt_RightNull_ReturnsNull" },
        new("{ uint? a = null; uint? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UInt_BothNull_ReturnsNull" },

        new("{ uint? a = null; uint? b = 5u; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_UInt_LeftNull_ReturnsNull" },
        new("{ uint? a = 5u; uint? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_UInt_RightNull_ReturnsNull" },
        new("{ uint? a = null; uint? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_UInt_BothNull_ReturnsNull" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: ulong? (null cases)
        new("{ ulong? a = null; ulong? b = 5UL; return a + b; }", null) { TestName = "LiftedArithmetic_Add_ULong_LeftNull_ReturnsNull" },
        new("{ ulong? a = 5UL; ulong? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_ULong_RightNull_ReturnsNull" },
        new("{ ulong? a = null; ulong? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_ULong_BothNull_ReturnsNull" },

        new("{ ulong? a = null; ulong? b = 5UL; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_ULong_LeftNull_ReturnsNull" },
        new("{ ulong? a = 5UL; ulong? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_ULong_RightNull_ReturnsNull" },
        new("{ ulong? a = null; ulong? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_ULong_BothNull_ReturnsNull" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: byte? (promotes to int)
        new("{ byte? a = null; byte? b = (byte?)3; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Byte_LeftNull_ReturnsNull" },
        new("{ byte? a = (byte?)5; byte? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Byte_RightNull_ReturnsNull" },
        new("{ byte? a = null; byte? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Byte_BothNull_ReturnsNull" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)3; return a + b; }", 8) { TestName = "LiftedArithmetic_Add_Byte_BothNonNull_ReturnsIntSum" },

        new("{ byte? a = null; byte? b = (byte?)3; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Byte_LeftNull_ReturnsNull" },
        new("{ byte? a = (byte?)5; byte? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Byte_RightNull_ReturnsNull" },
        new("{ byte? a = null; byte? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Byte_BothNull_ReturnsNull" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)3; return a * b; }", 15) { TestName = "LiftedArithmetic_Multiply_Byte_BothNonNull_ReturnsIntProduct" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: sbyte? (promotes to int)
        new("{ sbyte? a = null; sbyte? b = (sbyte?)3; return a + b; }", null) { TestName = "LiftedArithmetic_Add_SByte_LeftNull_ReturnsNull" },
        new("{ sbyte? a = (sbyte?)5; sbyte? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_SByte_RightNull_ReturnsNull" },
        new("{ sbyte? a = null; sbyte? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_SByte_BothNull_ReturnsNull" },
        new("{ sbyte? a = (sbyte?)5; sbyte? b = (sbyte?)3; return a + b; }", 8) { TestName = "LiftedArithmetic_Add_SByte_BothNonNull_ReturnsIntSum" },

        new("{ sbyte? a = null; sbyte? b = (sbyte?)3; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_SByte_LeftNull_ReturnsNull" },
        new("{ sbyte? a = (sbyte?)10; sbyte? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_SByte_RightNull_ReturnsNull" },
        new("{ sbyte? a = null; sbyte? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_SByte_BothNull_ReturnsNull" },
        new("{ sbyte? a = (sbyte?)10; sbyte? b = (sbyte?)3; return a - b; }", 7) { TestName = "LiftedArithmetic_Subtract_SByte_BothNonNull_ReturnsIntDifference" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: short? (promotes to int)
        new("{ short? a = null; short? b = (short?)3; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Short_LeftNull_ReturnsNull" },
        new("{ short? a = (short?)5; short? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Short_RightNull_ReturnsNull" },
        new("{ short? a = null; short? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_Short_BothNull_ReturnsNull" },
        new("{ short? a = (short?)5; short? b = (short?)3; return a + b; }", 8) { TestName = "LiftedArithmetic_Add_Short_BothNonNull_ReturnsIntSum" },

        new("{ short? a = null; short? b = (short?)3; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Short_LeftNull_ReturnsNull" },
        new("{ short? a = (short?)5; short? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Short_RightNull_ReturnsNull" },
        new("{ short? a = null; short? b = null; return a * b; }", null) { TestName = "LiftedArithmetic_Multiply_Short_BothNull_ReturnsNull" },
        new("{ short? a = (short?)5; short? b = (short?)3; return a * b; }", 15) { TestName = "LiftedArithmetic_Multiply_Short_BothNonNull_ReturnsIntProduct" },

        // ECMA-334 S12.4.8 -- Lifted Arithmetic: ushort? (promotes to int)
        new("{ ushort? a = null; ushort? b = (ushort?)3; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UShort_LeftNull_ReturnsNull" },
        new("{ ushort? a = (ushort?)5; ushort? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UShort_RightNull_ReturnsNull" },
        new("{ ushort? a = null; ushort? b = null; return a + b; }", null) { TestName = "LiftedArithmetic_Add_UShort_BothNull_ReturnsNull" },
        new("{ ushort? a = (ushort?)5; ushort? b = (ushort?)3; return a + b; }", 8) { TestName = "LiftedArithmetic_Add_UShort_BothNonNull_ReturnsIntSum" },

        new("{ ushort? a = null; ushort? b = (ushort?)3; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_UShort_LeftNull_ReturnsNull" },
        new("{ ushort? a = (ushort?)10; ushort? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_UShort_RightNull_ReturnsNull" },
        new("{ ushort? a = null; ushort? b = null; return a - b; }", null) { TestName = "LiftedArithmetic_Subtract_UShort_BothNull_ReturnsNull" },
        new("{ ushort? a = (ushort?)10; ushort? b = (ushort?)3; return a - b; }", 7) { TestName = "LiftedArithmetic_Subtract_UShort_BothNonNull_ReturnsIntDifference" },

        // ECMA-334 S12.4.8 -- Lifted Unary: non-int types
        new("{ long? a = null; return -a; }", null) { TestName = "LiftedUnary_Negate_Long_Null_ReturnsNull" },
        new("{ long? a = 5L; return -a; }", -5L) { TestName = "LiftedUnary_Negate_Long_NonNull_ReturnsNegated" },

        new("{ double? a = null; return -a; }", null) { TestName = "LiftedUnary_Negate_Double_Null_ReturnsNull" },

        new("{ float? a = null; return -a; }", null) { TestName = "LiftedUnary_Negate_Float_Null_ReturnsNull" },

        new("{ short? a = null; return -a; }", null) { TestName = "LiftedUnary_Negate_Short_Null_ReturnsNull" },
        new("{ short? a = (short?)5; return -a; }", -5) { TestName = "LiftedUnary_Negate_Short_NonNull_ReturnsIntNegated" },

        new("{ long? a = null; return ~a; }", null) { TestName = "LiftedUnary_BitwiseNot_Long_Null_ReturnsNull" },
        new("{ long? a = 5L; return ~a; }", -6L) { TestName = "LiftedUnary_BitwiseNot_Long_NonNull_ReturnsComplement" },

        new("{ long? a = null; return +a; }", null) { TestName = "LiftedUnary_Plus_Long_Null_ReturnsNull" },
        new("{ long? a = 5L; return +a; }", 5L) { TestName = "LiftedUnary_Plus_Long_NonNull_ReturnsSame" },

        // ECMA-334 S12.4.8 -- Lifted Equality: uint?
        new("{ uint? a = null; uint? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_UInt_BothNull_ReturnsTrue" },
        new("{ uint? a = null; uint? b = 5u; return a == b; }", false) { TestName = "LiftedEquality_Eq_UInt_LeftNull_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_UInt_RightNull_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = 5u; return a == b; }", true) { TestName = "LiftedEquality_Eq_UInt_EqualValues_ReturnsTrue" },
        new("{ uint? a = 5u; uint? b = 3u; return a == b; }", false) { TestName = "LiftedEquality_Eq_UInt_DifferentValues_ReturnsFalse" },

        new("{ uint? a = null; uint? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_UInt_BothNull_ReturnsFalse" },
        new("{ uint? a = null; uint? b = 5u; return a != b; }", true) { TestName = "LiftedEquality_Neq_UInt_LeftNull_ReturnsTrue" },
        new("{ uint? a = 5u; uint? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_UInt_RightNull_ReturnsTrue" },
        new("{ uint? a = 5u; uint? b = 5u; return a != b; }", false) { TestName = "LiftedEquality_Neq_UInt_EqualValues_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = 3u; return a != b; }", true) { TestName = "LiftedEquality_Neq_UInt_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: ulong?
        new("{ ulong? a = null; ulong? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_ULong_BothNull_ReturnsTrue" },
        new("{ ulong? a = null; ulong? b = 5UL; return a == b; }", false) { TestName = "LiftedEquality_Eq_ULong_LeftNull_ReturnsFalse" },
        new("{ ulong? a = 5UL; ulong? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_ULong_RightNull_ReturnsFalse" },
        new("{ ulong? a = 5UL; ulong? b = 5UL; return a == b; }", true) { TestName = "LiftedEquality_Eq_ULong_EqualValues_ReturnsTrue" },
        new("{ ulong? a = 5UL; ulong? b = 3UL; return a == b; }", false) { TestName = "LiftedEquality_Eq_ULong_DifferentValues_ReturnsFalse" },

        new("{ ulong? a = null; ulong? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_ULong_BothNull_ReturnsFalse" },
        new("{ ulong? a = null; ulong? b = 5UL; return a != b; }", true) { TestName = "LiftedEquality_Neq_ULong_LeftNull_ReturnsTrue" },
        new("{ ulong? a = 5UL; ulong? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_ULong_RightNull_ReturnsTrue" },
        new("{ ulong? a = 5UL; ulong? b = 5UL; return a != b; }", false) { TestName = "LiftedEquality_Neq_ULong_EqualValues_ReturnsFalse" },
        new("{ ulong? a = 5UL; ulong? b = 3UL; return a != b; }", true) { TestName = "LiftedEquality_Neq_ULong_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: short?
        new("{ short? a = null; short? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Short_BothNull_ReturnsTrue" },
        new("{ short? a = null; short? b = (short?)5; return a == b; }", false) { TestName = "LiftedEquality_Eq_Short_LeftNull_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Short_RightNull_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = (short?)5; return a == b; }", true) { TestName = "LiftedEquality_Eq_Short_EqualValues_ReturnsTrue" },
        new("{ short? a = (short?)5; short? b = (short?)3; return a == b; }", false) { TestName = "LiftedEquality_Eq_Short_DifferentValues_ReturnsFalse" },

        new("{ short? a = null; short? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Short_BothNull_ReturnsFalse" },
        new("{ short? a = null; short? b = (short?)5; return a != b; }", true) { TestName = "LiftedEquality_Neq_Short_LeftNull_ReturnsTrue" },
        new("{ short? a = (short?)5; short? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Short_RightNull_ReturnsTrue" },
        new("{ short? a = (short?)5; short? b = (short?)5; return a != b; }", false) { TestName = "LiftedEquality_Neq_Short_EqualValues_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = (short?)3; return a != b; }", true) { TestName = "LiftedEquality_Neq_Short_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Equality: byte?
        new("{ byte? a = null; byte? b = null; return a == b; }", true) { TestName = "LiftedEquality_Eq_Byte_BothNull_ReturnsTrue" },
        new("{ byte? a = null; byte? b = (byte?)5; return a == b; }", false) { TestName = "LiftedEquality_Eq_Byte_LeftNull_ReturnsFalse" },
        new("{ byte? a = (byte?)5; byte? b = null; return a == b; }", false) { TestName = "LiftedEquality_Eq_Byte_RightNull_ReturnsFalse" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)5; return a == b; }", true) { TestName = "LiftedEquality_Eq_Byte_EqualValues_ReturnsTrue" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)3; return a == b; }", false) { TestName = "LiftedEquality_Eq_Byte_DifferentValues_ReturnsFalse" },

        new("{ byte? a = null; byte? b = null; return a != b; }", false) { TestName = "LiftedEquality_Neq_Byte_BothNull_ReturnsFalse" },
        new("{ byte? a = null; byte? b = (byte?)5; return a != b; }", true) { TestName = "LiftedEquality_Neq_Byte_LeftNull_ReturnsTrue" },
        new("{ byte? a = (byte?)5; byte? b = null; return a != b; }", true) { TestName = "LiftedEquality_Neq_Byte_RightNull_ReturnsTrue" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)5; return a != b; }", false) { TestName = "LiftedEquality_Neq_Byte_EqualValues_ReturnsFalse" },
        new("{ byte? a = (byte?)5; byte? b = (byte?)3; return a != b; }", true) { TestName = "LiftedEquality_Neq_Byte_DifferentValues_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: uint?
        new("{ uint? a = null; uint? b = 5u; return a < b; }", false) { TestName = "LiftedRelational_LessThan_UInt_LeftNull_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_UInt_RightNull_ReturnsFalse" },
        new("{ uint? a = null; uint? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_UInt_BothNull_ReturnsFalse" },
        new("{ uint? a = 3u; uint? b = 5u; return a < b; }", true) { TestName = "LiftedRelational_LessThan_UInt_LessThan_ReturnsTrue" },

        new("{ uint? a = null; uint? b = 5u; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_UInt_LeftNull_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_UInt_RightNull_ReturnsFalse" },
        new("{ uint? a = null; uint? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_UInt_BothNull_ReturnsFalse" },
        new("{ uint? a = 5u; uint? b = 3u; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_UInt_GreaterThan_ReturnsTrue" },

        // ECMA-334 S12.4.8 -- Lifted Relational: short?
        new("{ short? a = null; short? b = (short?)5; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Short_LeftNull_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Short_RightNull_ReturnsFalse" },
        new("{ short? a = null; short? b = null; return a < b; }", false) { TestName = "LiftedRelational_LessThan_Short_BothNull_ReturnsFalse" },
        new("{ short? a = (short?)3; short? b = (short?)5; return a < b; }", true) { TestName = "LiftedRelational_LessThan_Short_LessThan_ReturnsTrue" },

        new("{ short? a = null; short? b = (short?)5; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Short_LeftNull_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Short_RightNull_ReturnsFalse" },
        new("{ short? a = null; short? b = null; return a > b; }", false) { TestName = "LiftedRelational_GreaterThan_Short_BothNull_ReturnsFalse" },
        new("{ short? a = (short?)5; short? b = (short?)3; return a > b; }", true) { TestName = "LiftedRelational_GreaterThan_Short_GreaterThan_ReturnsTrue" },
    ];

    /// <summary>
    /// Parity-only lifted operator expressions (no expected value -- types not representable in TestCase).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        // uint? non-null arithmetic (uint not supported as TestCase expected value)
        new("{ uint? a = 5u; uint? b = 3u; return a + b; }") { TestName = "LiftedArithmetic_Add_UInt_BothNonNull_ReturnsSum" },
        new("{ uint? a = 5u; uint? b = 3u; return a * b; }") { TestName = "LiftedArithmetic_Multiply_UInt_BothNonNull_ReturnsProduct" },

        // ulong? non-null arithmetic (ulong not supported as TestCase expected value)
        new("{ ulong? a = 5UL; ulong? b = 3UL; return a + b; }") { TestName = "LiftedArithmetic_Add_ULong_BothNonNull_ReturnsSum" },
        new("{ ulong? a = 5UL; ulong? b = 3UL; return a * b; }") { TestName = "LiftedArithmetic_Multiply_ULong_BothNonNull_ReturnsProduct" },

        // double? unary negate non-null (double negate uses parity-only)
        new("{ double? a = 3.14; return -a; }") { TestName = "LiftedUnary_Negate_Double_NonNull_ReturnsNegated" },

        // float? unary negate non-null (float not supported as TestCase expected value)
        new("{ float? a = 2.5f; return -a; }") { TestName = "LiftedUnary_Negate_Float_NonNull_ReturnsNegated" },
    ];
}
