using CsEval.Parsing;

namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5.5 — String literals and escape sequences,
/// §12.8.3 — Interpolated string expressions, §6.4.5.5 — Unicode escape sequences (\u, \U, \x).
/// Tests string operations, concatenation, interpolation, unicode/hex escapes,
/// string equality, and method overload resolution.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode)
{
    #region ECMA-334 §6.4.5.5 — String Operations and Concatenation

    [TestCase(@"""Hello"" + "" "" + ""World""", "Hello World", TestName = "Concatenation")]
    [TestCase(@"""HELLO"".ToLower()", "hello", TestName = "ToLower")]
    [TestCase(@"""hello"".ToUpper()", "HELLO", TestName = "ToUpper")]
    [TestCase(@"""hello"".Length", 5, TestName = "Length")]
    [TestCase(@"""  hello  "".Trim()", "hello", TestName = "Trim")]
    [TestCase(@"""hello world"".Contains(""world"")", true, TestName = "Contains_True")]
    [TestCase(@"""hello world"".Contains(""foo"")", false, TestName = "Contains_False")]
    [TestCase(@"""hello world"".StartsWith(""hello"")", true, TestName = "StartsWith_True")]
    [TestCase(@"""hello world"".StartsWith(""world"")", false, TestName = "StartsWith_False")]
    [TestCase(@"""hello world"".EndsWith(""world"")", true, TestName = "EndsWith_True")]
    [TestCase(@"""hello world"".EndsWith(""hello"")", false, TestName = "EndsWith_False")]
    [TestCase(@"""hello world"".Replace(""world"", ""there"")", "hello there", TestName = "Replace")]
    [TestCase(@"""hello world"".Substring(0, 5)", "hello", TestName = "Substring")]
    [TestCase(@"""hello world"".IndexOf(""o"")", 4, TestName = "IndexOf")]
    [TestCase(@""""".Length", 0, TestName = "EmptyString_Length")]
    [TestCase(@"""a"" + ""b"" + ""c""", "abc", TestName = "MultiConcatenation")]
    public async Task Eval_StringOperations(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.8.3 — Interpolated String Expressions

    [Test]
    public void Eval_InterpolatedString_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "World");
        Assert.That(engine.Evaluate("$\"Hello {name}!\""), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_InterpolatedString_WithExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        engine.SetVariable("y", 3);
        Assert.That(engine.Evaluate("$\"Sum: {x + y}\""), Is.EqualTo("Sum: 8"));
    }

    [Test]
    public void Eval_InterpolatedString_Multiple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", "John");
        engine.SetVariable("last", "Doe");
        Assert.That(engine.Evaluate("$\"{first} {last}\""), Is.EqualTo("John Doe"));
    }

    #endregion

    #region ECMA-334 §6.4.5.5 — Unicode Escape Sequences (\u and \U)

    [TestCase("\"\\u0041\"", "A", TestName = "Unicode4_A")]
    [TestCase("\"\\u0048\\u0065\\u006C\\u006C\\u006F\"", "Hello", TestName = "Unicode4_Hello")]
    [TestCase("\"\\u03B1\\u03B2\\u03B3\"", "αβγ", TestName = "Unicode4_Greek")]
    [TestCase("\"\\u4E2D\\u6587\"", "中文", TestName = "Unicode4_Chinese")]
    [TestCase("\"\\u00A9\"", "©", TestName = "Unicode4_Copyright")]
    [TestCase("\"\\u20AC\"", "€", TestName = "Unicode4_Euro")]
    [TestCase("\"A\\u0042C\"", "ABC", TestName = "Unicode4_Mixed")]
    public async Task Eval_UnicodeEscape4Digit_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("'\\u0041'", 'A', TestName = "Unicode4_Char_A")]
    [TestCase("'\\u03B1'", 'α', TestName = "Unicode4_Char_Alpha")]
    [TestCase("'\\u4E2D'", '中', TestName = "Unicode4_Char_Chinese")]
    [TestCase("'\\u0000'", '\0', TestName = "Unicode4_Char_Null")]
    [TestCase("'\\uFFFF'", '\uFFFF', TestName = "Unicode4_Char_Max")]
    public async Task Eval_UnicodeEscape4Digit_Char(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("\"\\U00000041\"", "A", TestName = "Unicode8_A")]
    [TestCase("\"\\U000003B1\"", "α", TestName = "Unicode8_Alpha")]
    [TestCase("\"\\U00004E2D\"", "中", TestName = "Unicode8_Chinese")]
    public async Task Eval_UnicodeEscape8Digit_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("'\\U00000041'", 'A', TestName = "Unicode8_Char_A")]
    [TestCase("'\\U000003B1'", 'α', TestName = "Unicode8_Char_Alpha")]
    public async Task Eval_UnicodeEscape8Digit_Char(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public async Task Eval_UnicodeEscape_InInterpolatedString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate("$\"\\u0041 = {x}\"");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("$\"\\u0041 = {42}\"");

        Assert.That(result, Is.EqualTo("A = 42"));
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [Test]
    public void Eval_UnicodeEscape_InvalidHex_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"\\u00GG\""));
    }

    [Test]
    public void Eval_UnicodeEscape_TooFewDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"\\u00\""));
    }

    [Test]
    public void Eval_UnicodeEscape8_TooFewDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"\\U0000\""));
    }

    #endregion

    #region ECMA-334 §6.4.5.5 — Hex Escape Sequences (\x)

    [TestCase("\"\\x41\"", "A", TestName = "HexEscape_2Digit_A")]
    [TestCase("\"\\x48\\x69\"", "Hi", TestName = "HexEscape_2Digit_Hi")]
    [TestCase("\"\\x0\"", "\0", TestName = "HexEscape_1Digit_Null")]
    [TestCase("\"\\x9\"", "\t", TestName = "HexEscape_1Digit_Tab")]
    [TestCase("\"\\x41B\"", "\x41B", TestName = "HexEscape_3Digit")]
    [TestCase("\"\\x0041\"", "A", TestName = "HexEscape_4Digit_A")]
    [TestCase("\"A\\x42C\"", "A\x42C", TestName = "HexEscape_Mixed")]
    public async Task Eval_HexEscape_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("'\\x41'", 'A', TestName = "HexEscape_Char_A")]
    [TestCase("'\\x0'", '\0', TestName = "HexEscape_Char_Null")]
    [TestCase("'\\x9'", '\t', TestName = "HexEscape_Char_Tab")]
    [TestCase("'\\xFF'", '\xFF', TestName = "HexEscape_Char_FF")]
    public async Task Eval_HexEscape_Char(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void Eval_HexEscape_NoDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"\\xG\""));
    }

    #endregion

    #region ECMA-334 §12.12.7 — String Comparison and Null Handling

    [TestCase("\"\" == \"\"", true, TestName = "StringEquality_EmptyStrings")]
    [TestCase("\"a\" == \"a\"", true, TestName = "StringEquality_Same")]
    [TestCase("\"a\" == \"b\"", false, TestName = "StringEquality_Different")]
    public async Task StringEquality(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void StringEquality_WithNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", null);

        Assert.That(engine.Evaluate("s == null"), Is.True);
        Assert.That(engine.Evaluate("s != null"), Is.False);
        Assert.That(engine.Evaluate("\"hello\" == null"), Is.False);
    }

    #endregion

    #region ECMA-334 §12.6.4 — Method Overload Resolution

    [TestCase("Math.Max(1, 2)", 2, TestName = "Overload_IntInt")]
    [TestCase("Math.Max(1L, 2L)", 2L, TestName = "Overload_LongLong")]
    [TestCase("Math.Max(1.0, 2.0)", 2.0, TestName = "Overload_DoubleDouble")]
    [TestCase("Math.Abs(-5)", 5, TestName = "Overload_AbsInt")]
    [TestCase("Math.Abs(-5L)", 5L, TestName = "Overload_AbsLong")]
    [TestCase("Math.Abs(-5.0)", 5.0, TestName = "Overload_AbsDouble")]
    public async Task MethodOverload_NumericTypes(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("\"hello\".Substring(1)", "ello", TestName = "Overload_Substring_OneArg")]
    [TestCase("\"hello\".Substring(1, 2)", "el", TestName = "Overload_Substring_TwoArgs")]
    [TestCase("\"hello\".IndexOf('l')", 2, TestName = "Overload_IndexOf_Char")]
    [TestCase("\"hello\".IndexOf(\"ll\")", 2, TestName = "Overload_IndexOf_String")]
    public async Task MethodOverload_StringMethods(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
