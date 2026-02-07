using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §12.4.7 -- Numeric promotion compliance test data.
/// Validates that CsEval matches Roslyn output for both value AND type
/// across all three compilation modes.
/// Shared across compiler backends.
///
/// References:
///   - ECMA-334 §12.4.7.2: Unary numeric promotion
///   - ECMA-334 §12.4.7.3: Binary numeric promotion
///   - ECMA-334 §12.11: Shift operators (left operand promotion)
/// </summary>
public static class NumericPromotionData
{
    /// <summary>
    /// ECMA-334 §12.4.7.2 -- Unary numeric promotion for bitwise NOT (~).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BitwiseNotParityCases() =>
    [
        new("~(byte)5") { TestName = "BitwiseNot_Byte_ReturnsInt" },
        new("~(sbyte)5") { TestName = "BitwiseNot_SByte_ReturnsInt" },
        new("~(short)5") { TestName = "BitwiseNot_Short_ReturnsInt" },
        new("~(ushort)5") { TestName = "BitwiseNot_UShort_ReturnsInt" },
        new("~(char)'A'") { TestName = "BitwiseNot_Char_ReturnsInt" },
        new("~5") { TestName = "BitwiseNot_Int_StaysInt" },
        new("~5L") { TestName = "BitwiseNot_Long_StaysLong" },
        new("~5U") { TestName = "BitwiseNot_UInt_StaysUInt" },
        new("~5UL") { TestName = "BitwiseNot_ULong_StaysULong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary minus (-).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> NegateParityCases() =>
    [
        new("-(byte)5") { TestName = "Negate_Byte_ReturnsInt" },
        new("-(sbyte)5") { TestName = "Negate_SByte_ReturnsInt" },
        new("-(short)5") { TestName = "Negate_Short_ReturnsInt" },
        new("-(ushort)5") { TestName = "Negate_UShort_ReturnsInt" },
        new("-(char)'A'") { TestName = "Negate_Char_ReturnsInt" },
        new("-5") { TestName = "Negate_Int_StaysInt" },
        new("-5L") { TestName = "Negate_Long_StaysLong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary plus (+).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> UnaryPlusParityCases() =>
    [
        new("+(byte)5") { TestName = "UnaryPlus_Byte_ReturnsInt" },
        new("+(sbyte)5") { TestName = "UnaryPlus_SByte_ReturnsInt" },
        new("+(short)5") { TestName = "UnaryPlus_Short_ReturnsInt" },
        new("+(ushort)5") { TestName = "UnaryPlus_UShort_ReturnsInt" },
        new("+(char)'A'") { TestName = "UnaryPlus_Char_ReturnsInt" },
        new("+5") { TestName = "UnaryPlus_Int_StaysInt" },
        new("+5L") { TestName = "UnaryPlus_Long_StaysLong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 8 -- Binary numeric promotion: both promoted to int.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule8ParityCases() =>
    [
        new("(byte)5 + (byte)3") { TestName = "BinaryPromo_BytePlusByte_IsInt" },
        new("(sbyte)5 + (sbyte)3") { TestName = "BinaryPromo_SBytePlusSByte_IsInt" },
        new("(short)5 + (short)3") { TestName = "BinaryPromo_ShortPlusShort_IsInt" },
        new("(ushort)5 + (ushort)3") { TestName = "BinaryPromo_UShortPlusUShort_IsInt" },
        new("(char)'A' + (char)'B'") { TestName = "BinaryPromo_CharPlusChar_IsInt" },
        new("(byte)5 + (short)3") { TestName = "BinaryPromo_BytePlusShort_IsInt" },
        new("(sbyte)5 + (ushort)3") { TestName = "BinaryPromo_SBytePlusUShort_IsInt" },
        new("(byte)5 + (char)'A'") { TestName = "BinaryPromo_BytePlusChar_IsInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 5 -- Binary numeric promotion: long.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule5ParityCases() =>
    [
        new("5 + 3L") { TestName = "BinaryPromo_IntPlusLong_IsLong" },
        new("5L + 3") { TestName = "BinaryPromo_LongPlusInt_IsLong" },
        new("(byte)5 + 3L") { TestName = "BinaryPromo_BytePlusLong_IsLong" },
        new("(short)5 + 3L") { TestName = "BinaryPromo_ShortPlusLong_IsLong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 6 -- Binary numeric promotion: uint + signed -> long.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule6ParityCases() =>
    [
        new("5U + (short)3") { TestName = "BinaryPromo_UIntPlusShort_IsLong" },
        new("5U + (sbyte)3") { TestName = "BinaryPromo_UIntPlusSByte_IsLong" },
        new("(short)3 + 5U") { TestName = "BinaryPromo_ShortPlusUInt_IsLong" },
        new("(sbyte)3 + 5U") { TestName = "BinaryPromo_SBytePlusUInt_IsLong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 7 -- Binary numeric promotion: uint.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule7ParityCases() =>
    [
        new("5U + 3U") { TestName = "BinaryPromo_UIntPlusUInt_IsUInt" },
        new("5U + (byte)3") { TestName = "BinaryPromo_UIntPlusByte_IsUInt" },
        new("5U + (ushort)3") { TestName = "BinaryPromo_UIntPlusUShort_IsUInt" },
        new("5U + (char)'A'") { TestName = "BinaryPromo_UIntPlusChar_IsUInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 4 -- Binary numeric promotion: ulong.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule4ParityCases() =>
    [
        new("5UL + 3UL") { TestName = "BinaryPromo_ULongPlusULong_IsULong" },
        new("5UL + 3U") { TestName = "BinaryPromo_ULongPlusUInt_IsULong" },
        new("5UL + (byte)3") { TestName = "BinaryPromo_ULongPlusByte_IsULong" },
        new("5UL + (ushort)3") { TestName = "BinaryPromo_ULongPlusUShort_IsULong" },
        new("5UL + (char)'A'") { TestName = "BinaryPromo_ULongPlusChar_IsULong" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 3 -- Binary numeric promotion: float.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule3ParityCases() =>
    [
        new("5 + 3.0f") { TestName = "BinaryPromo_IntPlusFloat_IsFloat" },
        new("5L + 3.0f") { TestName = "BinaryPromo_LongPlusFloat_IsFloat" },
        new("(byte)5 + 3.0f") { TestName = "BinaryPromo_BytePlusFloat_IsFloat" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 2 -- Binary numeric promotion: double.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule2ParityCases() =>
    [
        new("5 + 3.0") { TestName = "BinaryPromo_IntPlusDouble_IsDouble" },
        new("5L + 3.0") { TestName = "BinaryPromo_LongPlusDouble_IsDouble" },
        new("5.0f + 3.0") { TestName = "BinaryPromo_FloatPlusDouble_IsDouble" },
        new("(byte)5 + 3.0") { TestName = "BinaryPromo_BytePlusDouble_IsDouble" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 Rule 1 -- Binary numeric promotion: decimal.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryRule1ParityCases() =>
    [
        new("5 + 3.0m") { TestName = "BinaryPromo_IntPlusDecimal_IsDecimal" },
        new("5L + 3.0m") { TestName = "BinaryPromo_LongPlusDecimal_IsDecimal" },
        new("(byte)5 + 3.0m") { TestName = "BinaryPromo_BytePlusDecimal_IsDecimal" },
    ];

    /// <summary>
    /// ECMA-334 §12.11 -- Shift operators: left operand promotion (left shift).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftLeftParityCases() =>
    [
        new("(byte)5 << 2") { TestName = "Shift_ByteLeft_ReturnsInt" },
        new("(sbyte)5 << 2") { TestName = "Shift_SByteLeft_ReturnsInt" },
        new("(short)5 << 2") { TestName = "Shift_ShortLeft_ReturnsInt" },
        new("(ushort)5 << 2") { TestName = "Shift_UShortLeft_ReturnsInt" },
        new("(char)'A' << 2") { TestName = "Shift_CharLeft_ReturnsInt" },
        new("5 << 2") { TestName = "Shift_IntLeft_StaysInt" },
        new("5L << 2") { TestName = "Shift_LongLeft_StaysLong" },
        new("5U << 2") { TestName = "Shift_UIntLeft_StaysUInt" },
        new("5UL << 2") { TestName = "Shift_ULongLeft_StaysULong" },
    ];

    /// <summary>
    /// ECMA-334 §12.11 -- Shift operators: left operand promotion (right shift).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftRightParityCases() =>
    [
        new("(byte)32 >> 2") { TestName = "RightShift_Byte_ReturnsInt" },
        new("(sbyte)32 >> 2") { TestName = "RightShift_SByte_ReturnsInt" },
        new("(short)32 >> 2") { TestName = "RightShift_Short_ReturnsInt" },
        new("(ushort)32 >> 2") { TestName = "RightShift_UShort_ReturnsInt" },
        new("(char)'A' >> 2") { TestName = "RightShift_Char_ReturnsInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.4.7.3 -- Binary promotion across multiple operators.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> OtherOperatorsParityCases() =>
    [
        new("(byte)10 - (byte)3") { TestName = "Subtraction_ByteMinusByte_IsInt" },
        new("(byte)5 * (byte)3") { TestName = "Multiply_ByteTimesByte_IsInt" },
        new("(byte)10 / (byte)3") { TestName = "Divide_ByteDivByte_IsInt" },
        new("(byte)10 % (byte)3") { TestName = "Modulo_ByteModByte_IsInt" },
        new("(short)10 - (short)3") { TestName = "Subtraction_ShortMinusShort_IsInt" },
        new("(short)5 * (short)3") { TestName = "Multiply_ShortTimesShort_IsInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.13 -- Bitwise binary operators with promotion.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> BitwiseBinaryParityCases() =>
    [
        new("(byte)5 & (byte)3") { TestName = "BitwiseAnd_ByteAndByte_IsInt" },
        new("(short)5 | (short)3") { TestName = "BitwiseOr_ShortOrShort_IsInt" },
        new("(byte)5 ^ (byte)3") { TestName = "BitwiseXor_ByteXorByte_IsInt" },
        new("(char)'A' & (char)'B'") { TestName = "BitwiseAnd_CharAndChar_IsInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.12 -- Relational operators with promotion.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> RelationalParityCases() =>
    [
        new("(byte)5 < (byte)10") { TestName = "LessThan_ByteVsByte_IsPromotedToInt" },
        new("(short)5 > (short)3") { TestName = "GreaterThan_ShortVsShort_IsPromotedToInt" },
        new("5U == 5") { TestName = "Equality_UIntVsInt_PromotesToLong" },
        new("5U != 3") { TestName = "NotEqual_UIntVsInt_PromotesToLong" },
        new("5U < 10") { TestName = "LessThan_UIntVsInt_PromotesToLong" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.11 -- Implicit constant expression conversions.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConstantExprConversionCases() =>
    [
        new("5U + 3", 8U) { TestName = "ConstantExpr_UIntPlusIntLiteral_ReturnsUInt" },
        new("3 + 5U", 8U) { TestName = "ConstantExpr_IntLiteralPlusUInt_ReturnsUInt" },
        new("5UL + 3", 8UL) { TestName = "ConstantExpr_ULongPlusIntLiteral_ReturnsULong" },
        new("3 + 5UL", 8UL) { TestName = "ConstantExpr_IntLiteralPlusULong_ReturnsULong" },
        new("5U + 0", 5U) { TestName = "ConstantExpr_UIntPlusZero_ReturnsUInt" },
        new("5U + (-1)", -1L + 5U) { TestName = "ConstantExpr_UIntPlusNegativeInt_FallsToLong" },
        new("5U * 2", 10U) { TestName = "ConstantExpr_UIntTimesIntLiteral_ReturnsUInt" },
        new("10U - 3", 7U) { TestName = "ConstantExpr_UIntMinusIntLiteral_ReturnsUInt" },
        new("10U / 2", 5U) { TestName = "ConstantExpr_UIntDivIntLiteral_ReturnsUInt" },
        new("10U % 3", 1U) { TestName = "ConstantExpr_UIntModIntLiteral_ReturnsUInt" },
    ];
}
