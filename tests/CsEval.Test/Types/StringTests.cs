using CsEval.Parsing;

namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode)
{
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

    // Interpolation Tests

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

    // Unicode Escape Tests

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

    // Hex Escape Tests (\xH to \xHHHH)

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
}
