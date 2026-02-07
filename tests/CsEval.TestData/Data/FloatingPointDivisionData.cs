using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §12.10.3 -- Division operator, §12.10.4 -- Remainder operator.
/// Test data for floating-point division and modulo by zero semantics.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN per IEEE 754.
/// Shared across compiler backends.
/// </summary>
public static class FloatingPointDivisionData
{
    /// <summary>
    /// ECMA-334 §12.10.3 -- Floating-point division by zero parity cases.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> DivisionByZeroParityCases() =>
    [
        new("1.0 / 0.0") { TestName = "Division_DoubleByZero_PositiveInfinity" },
        new("-1.0 / 0.0") { TestName = "Division_NegativeDoubleByZero_NegativeInfinity" },
        new("1.0f / 0.0f") { TestName = "Division_FloatByZero_PositiveInfinity" },
        new("10 / 0.0") { TestName = "Division_IntByZeroDouble_Infinity" },
        new("10.0 / 0") { TestName = "Division_DoubleByZeroInt_Infinity" },
    ];

    /// <summary>
    /// ECMA-334 §12.10.3 -- NaN-producing expressions (need special IsNaN handling).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> NaNProducingCases() =>
    [
        new("0.0 / 0.0") { TestName = "Division_ZeroByZero_NaN" },
        new("5.0 % 0.0") { TestName = "Modulo_DoubleByZero_NaN" },
        new("5.0f % 0.0f") { TestName = "Modulo_FloatByZero_NaN" },
    ];

    /// <summary>
    /// ECMA-334 §12.10.3 -- NaN comparison semantics (double).
    /// Signature: (string expr, bool expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NaNComparisonCases() =>
    [
        new("double.NaN == double.NaN", false) { TestName = "NaN_EqualsItself_False" },
        new("double.NaN != double.NaN", true) { TestName = "NaN_NotEqualsItself_True" },
        new("double.NaN < 0", false) { TestName = "NaN_LessThanZero_False" },
        new("double.NaN > 0", false) { TestName = "NaN_GreaterThanZero_False" },
        new("double.NaN <= 0", false) { TestName = "NaN_LessOrEqualZero_False" },
        new("double.NaN >= 0", false) { TestName = "NaN_GreaterOrEqualZero_False" },
        new("0 < double.NaN", false) { TestName = "Zero_LessThanNaN_False" },
        new("0 > double.NaN", false) { TestName = "Zero_GreaterThanNaN_False" },
    ];

    /// <summary>
    /// ECMA-334 §12.10.3 -- Infinity arithmetic.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> InfinityArithmeticCases() =>
    [
        new("double.PositiveInfinity + 1", double.PositiveInfinity) { TestName = "Infinity_PlusOne" },
        new("double.NegativeInfinity - 1", double.NegativeInfinity) { TestName = "NegativeInfinity_MinusOne" },
        new("double.PositiveInfinity * 2", double.PositiveInfinity) { TestName = "Infinity_TimesTwo" },
        new("double.PositiveInfinity / 2", double.PositiveInfinity) { TestName = "Infinity_DividedByTwo" },
        new("1.0 / double.PositiveInfinity", 0.0) { TestName = "One_DividedByInfinity_Zero" },
        new("double.PositiveInfinity == double.PositiveInfinity", true) { TestName = "Infinity_EqualsItself" },
        new("double.PositiveInfinity > double.MaxValue", true) { TestName = "Infinity_GreaterThanMaxValue" },
    ];

    /// <summary>
    /// ECMA-334 §12.12.3 -- float.NaN comparison semantics.
    /// Signature: (string expr, bool expected)
    /// </summary>
    public static IEnumerable<TestCaseData> FloatNaNComparisonCases() =>
    [
        new("float.NaN == float.NaN", false) { TestName = "FloatNaN_EqualsItself_False" },
        new("float.NaN != float.NaN", true) { TestName = "FloatNaN_NotEqualsItself_True" },
        new("float.NaN < 0f", false) { TestName = "FloatNaN_LessThanZero_False" },
        new("float.NaN > 0f", false) { TestName = "FloatNaN_GreaterThanZero_False" },
        new("float.NaN <= 0f", false) { TestName = "FloatNaN_LessOrEqualZero_False" },
        new("float.NaN >= 0f", false) { TestName = "FloatNaN_GreaterOrEqualZero_False" },
        new("0f < float.NaN", false) { TestName = "ZeroFloat_LessThanNaN_False" },
        new("0f > float.NaN", false) { TestName = "ZeroFloat_GreaterThanNaN_False" },
    ];

    /// <summary>
    /// ECMA-334 §12.12.3 -- Cross-type NaN comparison.
    /// Signature: (string expr, bool expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CrossTypeNaNCases() =>
    [
        new("float.NaN == 0.0", false) { TestName = "CrossType_FloatNaN_EqualsDoubleZero_False" },
        new("float.NaN != 0.0", true) { TestName = "CrossType_FloatNaN_NotEqualsDoubleZero_True" },
        new("float.NaN < 0.0", false) { TestName = "CrossType_FloatNaN_LessThanDoubleZero_False" },
    ];

    /// <summary>
    /// ECMA-334 §8.3.7 -- IEEE 754 infinity division (value cases).
    /// Signature: (string expr, double expected)
    /// </summary>
    public static IEnumerable<TestCaseData> IEEE754InfinityDivisionCases() =>
    [
        new("1.0 / 0.0", double.PositiveInfinity) { TestName = "IEEE754_Double_PositiveInfinity" },
        new("-1.0 / 0.0", double.NegativeInfinity) { TestName = "IEEE754_Double_NegativeInfinity" },
    ];
}
