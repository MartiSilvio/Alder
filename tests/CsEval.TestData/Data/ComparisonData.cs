using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.12 -- Relational and type-testing operators.
/// Test data for equality (S12.12.7), relational comparisons (S12.12.2),
/// numeric promotion in mixed-type comparisons (S12.4.7.3), and null equality (S12.12.8).
/// Shared across compiler backends.
/// </summary>
public static class ComparisonData
{
    /// <summary>
    /// Value-producing equality expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> EqualityCases() =>
    [
        // ECMA-334 S12.12.7 -- Equality Operators
        new("1 == 1", true) { TestName = "Equal_SameInt" },
        new("1 == 2", false) { TestName = "Equal_DifferentInt" },
        new(@"""a"" == ""a""", true) { TestName = "Equal_SameString" },
        new("1 != 2", true) { TestName = "NotEqual_DifferentInt" },
        new("1 != 1", false) { TestName = "NotEqual_SameInt" },
    ];

    /// <summary>
    /// Value-producing relational comparison expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> RelationalCases() =>
    [
        // ECMA-334 S12.12.2 -- Integer Relational Operators
        new("1 < 2", true) { TestName = "LessThan_True" },
        new("2 < 1", false) { TestName = "LessThan_False" },
        new("2 > 1", true) { TestName = "GreaterThan_True" },
        new("1 > 2", false) { TestName = "GreaterThan_False" },
        new("1 <= 2", true) { TestName = "LessOrEqual_LessThan" },
        new("2 <= 2", true) { TestName = "LessOrEqual_Equal" },
        new("3 <= 2", false) { TestName = "LessOrEqual_GreaterThan" },
        new("2 >= 1", true) { TestName = "GreaterOrEqual_GreaterThan" },
        new("2 >= 2", true) { TestName = "GreaterOrEqual_Equal" },
        new("1 >= 2", false) { TestName = "GreaterOrEqual_LessThan" },
    ];

    /// <summary>
    /// Value-producing mixed-type comparison expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MixedTypeCases() =>
    [
        // ECMA-334 S12.4.7.3 -- Mixed Type Comparisons (Numeric Promotion)
        new("1 < 2L", true) { TestName = "LessThan_IntLong" },
        new("1L < 2", true) { TestName = "LessThan_LongInt" },
        new("1 < 2.0", true) { TestName = "LessThan_IntDouble" },
        new("1.0f < 2.0", true) { TestName = "LessThan_FloatDouble" },
    ];

    /// <summary>
    /// Value-producing null equality expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NullEqualityCases() =>
    [
        // ECMA-334 S12.12.8 -- Reference Equality and Null
        new("null == null", true) { TestName = "Equal_NullNull" },
        new("null != null", false) { TestName = "NotEqual_NullNull" },
    ];

    /// <summary>
    /// Value-producing char and bool comparison expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CharAndBoolCases() =>
    [
        // ECMA-334 S12.4.7.3: char operands are promoted to int for comparison
        new("'a' < 'b'", true) { TestName = "LessThan_CharChar" },
        new("'Z' < 'a'", true) { TestName = "LessThan_UpperLowerChar" },
        new("'a' == 'a'", true) { TestName = "Equal_CharChar" },
        // ECMA-334 S12.12.7: Boolean equality (no ordering operators for bool)
        new("true == true", true) { TestName = "Equal_BoolBool" },
        new("true != false", true) { TestName = "NotEqual_BoolBool" },
        new("(1 < 2) == true", true) { TestName = "ComparisonResult_IsBool" },
    ];

    /// <summary>
    /// Comparison expressions that should throw (decimal/float incompatibility).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        // ECMA-334 S12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
        new("1.0m < 1.0") { TestName = "LessThan_DecimalDouble" },
        new("1.0m > 1.0") { TestName = "GreaterThan_DecimalDouble" },
        new("1.0m <= 1.0") { TestName = "LessOrEqual_DecimalDouble" },
        new("1.0m >= 1.0") { TestName = "GreaterOrEqual_DecimalDouble" },
        new("1.0m == 1.0") { TestName = "Equal_DecimalDouble" },
        new("1.0m != 1.0") { TestName = "NotEqual_DecimalDouble" },
        new("1.0 < 1.0m") { TestName = "LessThan_DoubleDecimal" },
        new("1.0 > 1.0m") { TestName = "GreaterThan_DoubleDecimal" },
    ];
}
