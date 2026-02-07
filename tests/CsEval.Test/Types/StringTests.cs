using CsEval.Parsing;
using CsEval.TestData.Data;

namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5.5 -- String literals and escape sequences,
/// §12.8.3 -- Interpolated string expressions, §6.4.5.5 -- Unicode escape sequences (\u, \U, \x).
/// Tests string operations, concatenation, interpolation, unicode/hex escapes,
/// string equality, and method overload resolution.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode)
{
    #region ECMA-334 §6.4.5.5 -- String Operations and Concatenation

    [TestCaseSource(typeof(StringData), nameof(StringData.OperationCases))]
    public async Task Eval_StringOperations(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.8.3 -- Interpolated String Expressions

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

    #region ECMA-334 §6.4.5.5 -- Unicode Escape Sequences (\u and \U)

    [TestCaseSource(typeof(StringData), nameof(StringData.Unicode4StringCases))]
    public async Task Eval_UnicodeEscape4Digit_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(StringData), nameof(StringData.Unicode4CharCases))]
    public async Task Eval_UnicodeEscape4Digit_Char(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(StringData), nameof(StringData.Unicode8StringCases))]
    public async Task Eval_UnicodeEscape8Digit_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(StringData), nameof(StringData.Unicode8CharCases))]
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

    #region ECMA-334 §6.4.5.5 -- Hex Escape Sequences (\x)

    [TestCaseSource(typeof(StringData), nameof(StringData.HexEscapeStringCases))]
    public async Task Eval_HexEscape_String(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(StringData), nameof(StringData.HexEscapeCharCases))]
    public async Task Eval_HexEscape_Char(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void Eval_HexEscape_NoDigits_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalLexerException>(() => engine.Evaluate("\"\\xG\""));
    }

    #endregion

    #region ECMA-334 §12.12.7 -- String Comparison and Null Handling

    [TestCaseSource(typeof(StringData), nameof(StringData.StringEqualityCases))]
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

    #region ECMA-334 §12.6.4 -- Method Overload Resolution

    [TestCaseSource(typeof(StringData), nameof(StringData.MethodOverloadNumericCases))]
    public async Task MethodOverload_NumericTypes(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(StringData), nameof(StringData.MethodOverloadStringCases))]
    public async Task MethodOverload_StringMethods(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
