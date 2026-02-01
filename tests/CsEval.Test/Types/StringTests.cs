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
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

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
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("'\\u0041'", 'A', TestName = "Unicode4_Char_A")]
    [TestCase("'\\u03B1'", 'α', TestName = "Unicode4_Char_Alpha")]
    [TestCase("'\\u4E2D'", '中', TestName = "Unicode4_Char_Chinese")]
    [TestCase("'\\u0000'", '\0', TestName = "Unicode4_Char_Null")]
    [TestCase("'\\uFFFF'", '\uFFFF', TestName = "Unicode4_Char_Max")]
    public async Task Eval_UnicodeEscape4Digit_Char(string expr, char expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("\"\\U00000041\"", "A", TestName = "Unicode8_A")]
    [TestCase("\"\\U000003B1\"", "α", TestName = "Unicode8_Alpha")]
    [TestCase("\"\\U00004E2D\"", "中", TestName = "Unicode8_Chinese")]
    public async Task Eval_UnicodeEscape8Digit_String(string expr, string expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("'\\U00000041'", 'A', TestName = "Unicode8_Char_A")]
    [TestCase("'\\U000003B1'", 'α', TestName = "Unicode8_Char_Alpha")]
    public async Task Eval_UnicodeEscape8Digit_Char(string expr, char expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

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
        Assert.Throws<LexerException>(() => engine.Evaluate("\"\\u00GG\""));
    }

    [Test]
    public void Eval_UnicodeEscape_TooFewDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<LexerException>(() => engine.Evaluate("\"\\u00\""));
    }

    [Test]
    public void Eval_UnicodeEscape8_TooFewDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<LexerException>(() => engine.Evaluate("\"\\U0000\""));
    }
}
