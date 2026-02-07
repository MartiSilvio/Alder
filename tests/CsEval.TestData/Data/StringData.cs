using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §6.4.5.5 -- String literals and escape sequences,
/// §12.8.3 -- Interpolated string expressions, §6.4.5.5 -- Unicode escape sequences (\u, \U, \x).
/// Test data for string operations, concatenation, unicode/hex escapes,
/// string equality, and method overload resolution.
/// Shared across compiler backends.
/// </summary>
public static class StringData
{
    /// <summary>
    /// ECMA-334 §6.4.5.5 -- String operations and concatenation.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> OperationCases() =>
    [
        new(@"""Hello"" + "" "" + ""World""", "Hello World") { TestName = "Concatenation" },
        new(@"""HELLO"".ToLower()", "hello") { TestName = "ToLower" },
        new(@"""hello"".ToUpper()", "HELLO") { TestName = "ToUpper" },
        new(@"""hello"".Length", 5) { TestName = "Length" },
        new(@"""  hello  "".Trim()", "hello") { TestName = "Trim" },
        new(@"""hello world"".Contains(""world"")", true) { TestName = "Contains_True" },
        new(@"""hello world"".Contains(""foo"")", false) { TestName = "Contains_False" },
        new(@"""hello world"".StartsWith(""hello"")", true) { TestName = "StartsWith_True" },
        new(@"""hello world"".StartsWith(""world"")", false) { TestName = "StartsWith_False" },
        new(@"""hello world"".EndsWith(""world"")", true) { TestName = "EndsWith_True" },
        new(@"""hello world"".EndsWith(""hello"")", false) { TestName = "EndsWith_False" },
        new(@"""hello world"".Replace(""world"", ""there"")", "hello there") { TestName = "Replace" },
        new(@"""hello world"".Substring(0, 5)", "hello") { TestName = "Substring" },
        new(@"""hello world"".IndexOf(""o"")", 4) { TestName = "IndexOf" },
        new(@""""".Length", 0) { TestName = "EmptyString_Length" },
        new(@"""a"" + ""b"" + ""c""", "abc") { TestName = "MultiConcatenation" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Unicode 4-digit escape sequences in strings.
    /// Signature: (string expr, string expected)
    /// </summary>
    public static IEnumerable<TestCaseData> Unicode4StringCases() =>
    [
        new("\"\\u0041\"", "A") { TestName = "Unicode4_A" },
        new("\"\\u0048\\u0065\\u006C\\u006C\\u006F\"", "Hello") { TestName = "Unicode4_Hello" },
        new("\"\\u03B1\\u03B2\\u03B3\"", "\u03B1\u03B2\u03B3") { TestName = "Unicode4_Greek" },
        new("\"\\u4E2D\\u6587\"", "\u4E2D\u6587") { TestName = "Unicode4_Chinese" },
        new("\"\\u00A9\"", "\u00A9") { TestName = "Unicode4_Copyright" },
        new("\"\\u20AC\"", "\u20AC") { TestName = "Unicode4_Euro" },
        new("\"A\\u0042C\"", "ABC") { TestName = "Unicode4_Mixed" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Unicode 4-digit escape sequences in chars.
    /// Signature: (string expr, char expected)
    /// </summary>
    public static IEnumerable<TestCaseData> Unicode4CharCases() =>
    [
        new("'\\u0041'", 'A') { TestName = "Unicode4_Char_A" },
        new("'\\u03B1'", '\u03B1') { TestName = "Unicode4_Char_Alpha" },
        new("'\\u4E2D'", '\u4E2D') { TestName = "Unicode4_Char_Chinese" },
        new("'\\u0000'", '\0') { TestName = "Unicode4_Char_Null" },
        new("'\\uFFFF'", '\uFFFF') { TestName = "Unicode4_Char_Max" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Unicode 8-digit escape sequences in strings.
    /// Signature: (string expr, string expected)
    /// </summary>
    public static IEnumerable<TestCaseData> Unicode8StringCases() =>
    [
        new("\"\\U00000041\"", "A") { TestName = "Unicode8_A" },
        new("\"\\U000003B1\"", "\u03B1") { TestName = "Unicode8_Alpha" },
        new("\"\\U00004E2D\"", "\u4E2D") { TestName = "Unicode8_Chinese" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Unicode 8-digit escape sequences in chars.
    /// Signature: (string expr, char expected)
    /// </summary>
    public static IEnumerable<TestCaseData> Unicode8CharCases() =>
    [
        new("'\\U00000041'", 'A') { TestName = "Unicode8_Char_A" },
        new("'\\U000003B1'", '\u03B1') { TestName = "Unicode8_Char_Alpha" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Hex escape sequences in strings.
    /// Signature: (string expr, string expected)
    /// </summary>
    public static IEnumerable<TestCaseData> HexEscapeStringCases() =>
    [
        new("\"\\x41\"", "A") { TestName = "HexEscape_2Digit_A" },
        new("\"\\x48\\x69\"", "Hi") { TestName = "HexEscape_2Digit_Hi" },
        new("\"\\x0\"", "\0") { TestName = "HexEscape_1Digit_Null" },
        new("\"\\x9\"", "\t") { TestName = "HexEscape_1Digit_Tab" },
        new("\"\\x41B\"", "\x41B") { TestName = "HexEscape_3Digit" },
        new("\"\\x0041\"", "A") { TestName = "HexEscape_4Digit_A" },
        new("\"A\\x42C\"", "A\x42C") { TestName = "HexEscape_Mixed" },
    ];

    /// <summary>
    /// ECMA-334 §6.4.5.5 -- Hex escape sequences in chars.
    /// Signature: (string expr, char expected)
    /// </summary>
    public static IEnumerable<TestCaseData> HexEscapeCharCases() =>
    [
        new("'\\x41'", 'A') { TestName = "HexEscape_Char_A" },
        new("'\\x0'", '\0') { TestName = "HexEscape_Char_Null" },
        new("'\\x9'", '\t') { TestName = "HexEscape_Char_Tab" },
        new("'\\xFF'", '\xFF') { TestName = "HexEscape_Char_FF" },
    ];

    /// <summary>
    /// ECMA-334 §12.12.7 -- String equality comparison.
    /// Signature: (string expr, bool expected)
    /// </summary>
    public static IEnumerable<TestCaseData> StringEqualityCases() =>
    [
        new("\"\" == \"\"", true) { TestName = "StringEquality_EmptyStrings" },
        new("\"a\" == \"a\"", true) { TestName = "StringEquality_Same" },
        new("\"a\" == \"b\"", false) { TestName = "StringEquality_Different" },
    ];

    /// <summary>
    /// ECMA-334 §12.6.4 -- Method overload resolution (numeric types).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MethodOverloadNumericCases() =>
    [
        new("Math.Max(1, 2)", 2) { TestName = "Overload_IntInt" },
        new("Math.Max(1L, 2L)", 2L) { TestName = "Overload_LongLong" },
        new("Math.Max(1.0, 2.0)", 2.0) { TestName = "Overload_DoubleDouble" },
        new("Math.Abs(-5)", 5) { TestName = "Overload_AbsInt" },
        new("Math.Abs(-5L)", 5L) { TestName = "Overload_AbsLong" },
        new("Math.Abs(-5.0)", 5.0) { TestName = "Overload_AbsDouble" },
    ];

    /// <summary>
    /// ECMA-334 §12.6.4 -- Method overload resolution (string methods).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MethodOverloadStringCases() =>
    [
        new("\"hello\".Substring(1)", "ello") { TestName = "Overload_Substring_OneArg" },
        new("\"hello\".Substring(1, 2)", "el") { TestName = "Overload_Substring_TwoArgs" },
        new("\"hello\".IndexOf('l')", 2) { TestName = "Overload_IndexOf_Char" },
        new("\"hello\".IndexOf(\"ll\")", 2) { TestName = "Overload_IndexOf_String" },
    ];
}
