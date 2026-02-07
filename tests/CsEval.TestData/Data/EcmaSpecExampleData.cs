using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 spec example test data -- executable test cases derived from ECMA-334 7th Edition.
/// Each method references the specific ECMA-334 section from which it is derived.
/// Shared across compiler backends.
/// </summary>
public static class EcmaSpecExampleData
{
    /// <summary>
    /// ECMA-334 S12.9.4 -- Logical negation operator.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> LogicalNegationCases() =>
    [
        new("!true", false) { TestName = "S12_9_4_LogicalNot_True" },
        new("!false", true) { TestName = "S12_9_4_LogicalNot_False" },
    ];

    /// <summary>
    /// ECMA-334 S12.13.4 -- Boolean logical operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BooleanLogicalCases() =>
    [
        new("true & true", true) { TestName = "S12_13_4_BoolAnd_TrueTrue" },
        new("true & false", false) { TestName = "S12_13_4_BoolAnd_TrueFalse" },
        new("false & true", false) { TestName = "S12_13_4_BoolAnd_FalseTrue" },
        new("false & false", false) { TestName = "S12_13_4_BoolAnd_FalseFalse" },
        new("true | false", true) { TestName = "S12_13_4_BoolOr_TrueFalse" },
        new("false | false", false) { TestName = "S12_13_4_BoolOr_FalseFalse" },
        new("true ^ true", false) { TestName = "S12_13_4_BoolXor_TrueTrue" },
        new("true ^ false", true) { TestName = "S12_13_4_BoolXor_TrueFalse" },
        new("true != false", true) { TestName = "S12_13_4_BoolXorEqualsNotEqual" },
    ];

    /// <summary>
    /// ECMA-334 S12.10.3 -- Division operator.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> DivisionCases() =>
    [
        new("7 / 2", 3) { TestName = "S12_10_3_IntegerDivision_TruncatesTowardZero" },
        new("-7 / 2", -3) { TestName = "S12_10_3_IntegerDivision_NegativeTruncatesTowardZero" },
    ];

    /// <summary>
    /// ECMA-334 S12.10.4 -- Remainder operator.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> RemainderCases() =>
    [
        new("7 % 3", 1) { TestName = "S12_10_4_IntegerRemainder_Positive" },
        new("-7 % 3", -1) { TestName = "S12_10_4_IntegerRemainder_NegativeLeftOperand" },
    ];

    /// <summary>
    /// ECMA-334 S12.10.5 -- String concatenation.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> StringConcatCases() =>
    [
        new("\"i = \" + 1", "i = 1") { TestName = "S12_10_5_StringConcat_IntToString" },
        new("\"b = \" + true", "b = True") { TestName = "S12_10_5_StringConcat_BoolToString" },
    ];

    /// <summary>
    /// ECMA-334 S12.11 -- Shift operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftCases() =>
    [
        new("1 << 4", 16) { TestName = "S12_11_LeftShift_Int" },
        new("16 >> 2", 4) { TestName = "S12_11_RightShift_Int" },
        new("-16 >> 2", -4) { TestName = "S12_11_ArithmeticRightShift_Negative" },
    ];

    /// <summary>
    /// ECMA-334 S12.12.3 -- Floating-point comparison.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> FloatingPointComparisonCases() =>
    [
        new("double.NaN == double.NaN", false) { TestName = "S12_12_3_NaN_Equals_False" },
        new("double.NaN != double.NaN", true) { TestName = "S12_12_3_NaN_NotEquals_True" },
        new("double.NaN < 0.0", false) { TestName = "S12_12_3_NaN_LessThan_False" },
        new("double.NaN > 0.0", false) { TestName = "S12_12_3_NaN_GreaterThan_False" },
        new("-0.0 == 0.0", true) { TestName = "S12_12_3_NegativeZero_Equals_PositiveZero" },
    ];

    /// <summary>
    /// ECMA-334 S12.12.5 -- Boolean equality operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BooleanEqualityCases() =>
    [
        new("true == true", true) { TestName = "S12_12_5_BoolEqual_TrueTrue" },
        new("true == false", false) { TestName = "S12_12_5_BoolEqual_TrueFalse" },
        new("false != false", false) { TestName = "S12_12_5_BoolNotEqual_FalseFalse" },
        new("true != false", true) { TestName = "S12_12_5_BoolNotEqual_TrueFalse" },
    ];

    /// <summary>
    /// ECMA-334 S12.14.2 -- Boolean conditional logical operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConditionalLogicalCases() =>
    [
        new("true && true", true) { TestName = "S12_14_2_ConditionalAnd_TrueTrue" },
        new("false && true", false) { TestName = "S12_14_2_ConditionalAnd_FalseTrue_ShortCircuit" },
        new("true || false", true) { TestName = "S12_14_2_ConditionalOr_TrueFalse_ShortCircuit" },
        new("false || false", false) { TestName = "S12_14_2_ConditionalOr_FalseFalse" },
    ];

    /// <summary>
    /// ECMA-334 S12.18 -- Conditional operator.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConditionalOperatorCases() =>
    [
        new("true ? 1 : 2", 1) { TestName = "S12_18_Conditional_TrueSelectsFirst" },
        new("false ? 1 : 2", 2) { TestName = "S12_18_Conditional_FalseSelectsSecond" },
    ];

    /// <summary>
    /// ECMA-334 S10.3.2 / S12.9.7 -- Explicit numeric conversions (cast).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExplicitConversionCases() =>
    [
        new("(int)42L", 42) { TestName = "S10_3_2_LongToInt_ExplicitCast" },
        new("(char)65", 'A') { TestName = "S10_3_2_IntToChar_ExplicitCast" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.2 -- Literals.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> LiteralCases() =>
    [
        new("42", 42) { TestName = "S12_8_2_IntLiteral" },
        new("42L", 42L) { TestName = "S12_8_2_LongLiteral" },
        new("42U", 42U) { TestName = "S12_8_2_UIntLiteral" },
        new("42UL", 42UL) { TestName = "S12_8_2_ULongLiteral" },
        new("3.14", 3.14) { TestName = "S12_8_2_DoubleLiteral" },
        new("3.14f", 3.14f) { TestName = "S12_8_2_FloatLiteral" },
        new("true", true) { TestName = "S12_8_2_BoolTrueLiteral" },
        new("false", false) { TestName = "S12_8_2_BoolFalseLiteral" },
        new("null", null) { TestName = "S12_8_2_NullLiteral" },
        new("'A'", 'A') { TestName = "S12_8_2_CharLiteral" },
    ];

    /// <summary>
    /// ECMA-334 S12.13.2 -- Integer logical operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> IntegerLogicalCases() =>
    [
        new("0xFF & 0x0F", 0x0F) { TestName = "S12_13_2_BitwiseAnd_Int" },
        new("0xF0 | 0x0F", 0xFF) { TestName = "S12_13_2_BitwiseOr_Int" },
        new("0xFF ^ 0x0F", 0xF0) { TestName = "S12_13_2_BitwiseXor_Int" },
    ];
}
