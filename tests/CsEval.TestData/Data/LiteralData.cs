using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §6.4.5 -- Literals (integer, real, boolean, character, string, null).
/// Test data for integer literals (§6.4.5.2), real literals (§6.4.5.3), boolean literals (§6.4.5.6),
/// character literals (§6.4.5.5), escape sequences, hex/binary literals,
/// digit separators, exponent notation, and leading decimal literals (§6.4.5.4).
/// Shared across compiler backends.
/// </summary>
public static class LiteralData
{
    /// <summary>
    /// ECMA-334 §6.4.5.2, §6.4.5.3, §6.4.5.6 -- Basic literals.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BasicCases() =>
    [
        new("42", 42) { TestName = "Integer" },
        new("3.14", 3.14) { TestName = "Double" },
        new("\"hello\"", "hello") { TestName = "String" },
        new("true", true) { TestName = "True" },
        new("false", false) { TestName = "False" },
        new("0xFF", 255) { TestName = "HexLiteral" },
        new("0x1A", 26) { TestName = "HexLiteral2" },
        new("0b1010", 10) { TestName = "BinaryLiteral" },
        new("0b11111111", 255) { TestName = "BinaryLiteral2" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Character escape sequences.
    /// Signature: (string expr, string expected)
    /// </summary>
    public static IEnumerable<TestCaseData> EscapeSequenceCases() =>
    [
        new(@"""\0""", "\0") { TestName = "EscapeNull" },
        new(@"""\a""", "\a") { TestName = "EscapeAlert" },
        new(@"""\b""", "\b") { TestName = "EscapeBackspace" },
        new(@"""\f""", "\f") { TestName = "EscapeFormFeed" },
        new(@"""\v""", "\v") { TestName = "EscapeVerticalTab" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.2 -- Hex and binary integer arithmetic.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> HexBinaryArithmeticCases() =>
    [
        new("0xFF + 1", 256) { TestName = "HexArithmetic" },
        new("0b1010 * 2", 20) { TestName = "BinaryArithmetic" },
        new("0xFF == 255", true) { TestName = "HexComparison" },
        new("0b1010 == 10", true) { TestName = "BinaryComparison" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Character literals.
    /// Signature: (string expr, char expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CharLiteralCases() =>
    [
        new("'a'", 'a') { TestName = "CharLiteralA" },
        new("'Z'", 'Z') { TestName = "CharLiteralZ" },
        new("'0'", '0') { TestName = "CharDigit" },
        new(@"'\n'", '\n') { TestName = "CharEscapeNewline" },
        new(@"'\t'", '\t') { TestName = "CharEscapeTab" },
    ];

    /// <summary>
    /// ECMA-334 §12.12 -- Char comparison operations.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CharComparisonCases() =>
    [
        new("'a' == 'a'", true) { TestName = "CharComparison" },
        new("'a' < 'b'", true) { TestName = "CharLessThan" },
        new("'a' != 'b'", true) { TestName = "CharNotEqual" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.2, §6.4.5.3 -- Digit separators (decimal).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> DigitSeparatorDecimalCases() =>
    [
        new("1_000_000", 1000000) { TestName = "DigitSeparator_Millions" },
        new("1_2_3", 123) { TestName = "DigitSeparator_Arbitrary" },
        new("100_000L", 100000L) { TestName = "DigitSeparator_Long" },
        new("1_000u", 1000u) { TestName = "DigitSeparator_UInt" },
        new("1_000_000_000_000UL", 1000000000000UL) { TestName = "DigitSeparator_ULong" },
        new("3.14_159", 3.14159) { TestName = "DigitSeparator_Double" },
        new("1_000.5", 1000.5) { TestName = "DigitSeparator_DoubleBeforeDecimal" },
        new("1_000.5_5", 1000.55) { TestName = "DigitSeparator_DoubleBothSides" },
        new("3.14_159f", 3.14159f) { TestName = "DigitSeparator_Float" },
        new("1_000.50m", 1000.50) { TestName = "DigitSeparator_Decimal" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.2, §6.4.5.3 -- Digit separators (hex).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> DigitSeparatorHexCases() =>
    [
        new("0xFF_FF", 0xFFFF) { TestName = "DigitSeparator_Hex" },
        new("0x00_FF_00_FF", 0x00FF00FF) { TestName = "DigitSeparator_HexBytes" },
        new("0xAB_CD_EF", 0xABCDEF) { TestName = "DigitSeparator_HexMixed" },
        new("0xFF_FF_FF_FF_FF_FF_FF_FFUL", 0xFFFFFFFFFFFFFFFFUL) { TestName = "DigitSeparator_HexULong" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.2, §6.4.5.3 -- Digit separators (binary).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> DigitSeparatorBinaryCases() =>
    [
        new("0b1111_0000", 0b11110000) { TestName = "DigitSeparator_Binary" },
        new("0b1010_1010_1010_1010", 0b1010101010101010) { TestName = "DigitSeparator_BinaryPattern" },
        new("0b1111_1111L", 0b11111111L) { TestName = "DigitSeparator_BinaryLong" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.3 -- Exponent notation (double).
    /// Signature: (string expr, double expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExponentDoubleCases() =>
    [
        new("1e10", 1e10) { TestName = "Exponent_Positive" },
        new("1E10", 1E10) { TestName = "Exponent_UpperCase" },
        new("1.5e3", 1.5e3) { TestName = "Exponent_WithDecimal" },
        new("1.5E-3", 1.5E-3) { TestName = "Exponent_Negative" },
        new("1e+5", 1e+5) { TestName = "Exponent_ExplicitPositive" },
        new("2.5e0", 2.5e0) { TestName = "Exponent_Zero" },
        new("6.022e23", 6.022e23) { TestName = "Exponent_Avogadro" },
        new("1.6e-19", 1.6e-19) { TestName = "Exponent_ElementaryCharge" },
        new("3.14159e2", 3.14159e2) { TestName = "Exponent_Pi100" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.3 -- Exponent notation (float).
    /// Signature: (string expr, float expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExponentFloatCases() =>
    [
        new("1e5f", 1e5f) { TestName = "Exponent_Float" },
        new("1.5e-2f", 1.5e-2f) { TestName = "Exponent_FloatNegative" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.3 -- Exponent notation (decimal).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExponentDecimalCases() =>
    [
        new("1e5m", 100000) { TestName = "Exponent_Decimal" },
        new("1.5e-2m", 0.015) { TestName = "Exponent_DecimalNegative" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.4 -- Leading decimal literals.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> LeadingDecimalCases() =>
    [
        new(".5", 0.5) { TestName = "LeadingDecimal_Half" },
        new(".123", 0.123) { TestName = "LeadingDecimal_Fraction" },
        new(".0", 0.0) { TestName = "LeadingDecimal_Zero" },
        new(".999", 0.999) { TestName = "LeadingDecimal_NineNine" },
        new(".5f", 0.5f) { TestName = "LeadingDecimal_Float" },
        new(".5m", 0.5) { TestName = "LeadingDecimal_Decimal" },
        new(".5e2", 50.0) { TestName = "LeadingDecimal_WithExponent" },
        new(".5e-2", 0.005) { TestName = "LeadingDecimal_WithNegativeExponent" },
        new(".123_456", 0.123456) { TestName = "LeadingDecimal_WithDigitSeparator" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.2, §6.4.5.3 -- Combined digit separators and exponents.
    /// Signature: (string expr, double expected)
    /// </summary>
    public static IEnumerable<TestCaseData> DigitSeparatorWithExponentCases() =>
    [
        new("1_000e3", 1000e3) { TestName = "DigitSeparator_WithExponent" },
        new("1.234_567e10", 1.234567e10) { TestName = "DigitSeparator_DecimalWithExponent" },
        new("6.022_140_76e23", 6.02214076e23) { TestName = "DigitSeparator_Avogadro" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5 -- Invalid literal error cases (lexer exceptions).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> InvalidLiteralLexerCases() =>
    [
        new("0xGG") { TestName = "InvalidHex" },
        new("1__000") { TestName = "DoubleUnderscore" },
        new("1000_") { TestName = "TrailingUnderscore" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5 -- Invalid literal error cases (parser exceptions).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> InvalidLiteralParserCases() =>
    [
        new("0b123") { TestName = "InvalidBinary_MixedDigits" },
    ];
}
