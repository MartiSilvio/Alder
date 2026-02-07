using CsEval.TestData.Data;

namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5 -- Literals (integer, real, boolean, character, string, null).
/// Tests integer literals (§6.4.5.2), real literals (§6.4.5.3), boolean literals (§6.4.5.6),
/// character literals (§6.4.5.5), escape sequences (§6.4.5.5), hex/binary literals,
/// digit separators, exponent notation, and leading decimal literals (§6.4.5.4).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiteralTests(CompilationMode mode)
{
    #region ECMA-334 §6.4.5.2, §6.4.5.3, §6.4.5.6 -- Basic Literals

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.BasicCases))]
    public async Task Eval_Literal(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public async Task Eval_Null_ReturnsNull()
        => await TestHelpers.RunCSharpParityTestAsync("null", null, mode);

    #endregion

    #region ECMA-334 §6.4.5.5 -- Character Escape Sequences

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.EscapeSequenceCases))]
    public async Task Eval_EscapeSequence_MatchesCSharp(string expr, string expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.2 -- Hex and Binary Integer Literals

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.HexBinaryArithmeticCases))]
    public async Task Eval_HexBinaryArithmetic_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.5 -- Character Literals

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.CharLiteralCases))]
    public async Task Eval_CharLiteral_MatchesCSharp(string expr, char expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.12 -- Char Comparison Operations

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.CharComparisonCases))]
    public async Task Eval_CharOperations_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.2, §6.4.5.3 -- Digit Separators (C# 7.0+)

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.DigitSeparatorDecimalCases))]
    public async Task Eval_DigitSeparator_Decimal(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.DigitSeparatorHexCases))]
    public async Task Eval_DigitSeparator_Hex(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.DigitSeparatorBinaryCases))]
    public async Task Eval_DigitSeparator_Binary(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.3 -- Exponent Notation

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.ExponentDoubleCases))]
    public async Task Eval_Exponent_Double(string expr, double expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.ExponentFloatCases))]
    public async Task Eval_Exponent_Float(string expr, float expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.ExponentDecimalCases))]
    public async Task Eval_Exponent_Decimal(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.4 -- Leading Decimal Literals

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.LeadingDecimalCases))]
    public async Task Eval_LeadingDecimal(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5.2, §6.4.5.3 -- Combined Digit Separators and Exponents

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.DigitSeparatorWithExponentCases))]
    public async Task Eval_DigitSeparator_WithExponent(string expr, double expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §6.4.5 -- Invalid Literal Error Cases

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.InvalidLiteralLexerCases))]
    public void Eval_Literal_ShouldThrowLexerException(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalLexerException>(() => engine.Evaluate(expr));
    }

    [TestCaseSource(typeof(LiteralData), nameof(LiteralData.InvalidLiteralParserCases))]
    public void Eval_Literal_ShouldThrowParserException(string expr)
    {
        // 0b123 lexes as 0b1 (valid binary) + 23 (unexpected token)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalParserException>(() => engine.Evaluate(expr));
    }

    #endregion
}
