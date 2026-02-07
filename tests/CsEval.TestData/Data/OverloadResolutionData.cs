using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.6.4 -- Overload resolution.
/// Test data for exact type matching, widening conversion preference,
/// multiple parameter overloads, string method overloads, and Math method overloads.
/// Shared across compiler backends.
/// </summary>
public static class OverloadResolutionData
{
    /// <summary>
    /// Exact match overload resolution cases (ECMA-334 S12.6.4.3).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExactMatchCases() =>
    [
        new("Math.Abs(5)", 5) { TestName = "MathAbs_Int_SelectsIntOverload" },
        new("Math.Abs(5L)", 5L) { TestName = "MathAbs_Long_SelectsLongOverload" },
        new("Math.Abs(5.0)", 5.0) { TestName = "MathAbs_Double_SelectsDoubleOverload" },
        new("Math.Abs(5.0f)", 5.0f) { TestName = "MathAbs_Float_SelectsFloatOverload" },
    ];

    /// <summary>
    /// Widening conversion with negative values (ECMA-334 S12.6.4.5).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NegativeValueCases() =>
    [
        new("Math.Abs(-5)", 5) { TestName = "MathAbs_NegativeInt_ReturnsPositiveInt" },
        new("Math.Abs(-5L)", 5L) { TestName = "MathAbs_NegativeLong_ReturnsPositiveLong" },
        new("Math.Abs(-5.5)", 5.5) { TestName = "MathAbs_NegativeDouble_ReturnsPositiveDouble" },
    ];

    /// <summary>
    /// Bankers rounding cases for Math.Round (ECMA-334 S12.6.4).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BankersRoundingCases() =>
    [
        new("Math.Round(3.5)", 4.0) { TestName = "MathRound_BankersRounding_3_5" },
        new("Math.Round(4.5)", 4.0) { TestName = "MathRound_BankersRounding_4_5" },
        new("Math.Round(2.5)", 2.0) { TestName = "MathRound_BankersRounding_2_5" },
    ];

    /// <summary>
    /// Math.Max/Min overload selection cases.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MathMinMaxCases() =>
    [
        new("Math.Max(5, 10)", 10) { TestName = "MathMax_Int_ReturnsInt" },
        new("Math.Max(5L, 10L)", 10L) { TestName = "MathMax_Long_ReturnsLong" },
        new("Math.Max(5.0, 10.0)", 10.0) { TestName = "MathMax_Double_ReturnsDouble" },
        new("Math.Min(5, 10)", 5) { TestName = "MathMin_Int_ReturnsInt" },
        new("Math.Min(5L, 10L)", 5L) { TestName = "MathMin_Long_ReturnsLong" },
        new("Math.Min(5.0, 10.0)", 5.0) { TestName = "MathMin_Double_ReturnsDouble" },
    ];

    /// <summary>
    /// String method overload selection cases (ECMA-334 S12.6.4).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> StringConcatCases() =>
    [
        new("String.Concat(\"hello\", \" \", \"world\")", "hello world") { TestName = "StringConcat_ThreeStrings" },
        new("String.Concat(\"a\", \"b\")", "ab") { TestName = "StringConcat_TwoStrings" },
    ];

    /// <summary>
    /// Convert method overload selection cases.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConvertCases() =>
    [
        new("Convert.ToInt32(\"42\")", 42) { TestName = "ConvertToInt32_String_ReturnsInt" },
        new("Convert.ToDouble(\"3.14\")", 3.14) { TestName = "ConvertToDouble_String_ReturnsDouble" },
    ];
}
