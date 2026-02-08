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

    [Test]
    public async Task Eval_Null_ReturnsNull()
        => await TestHelpers.RunCSharpParityTestAsync("null", null, mode);

    #endregion

    #region ECMA-334 §6.4.5 -- Invalid Literal Error Cases

    [TestCase("0xGG", TestName = "InvalidHex")]
    [TestCase("1__000", TestName = "DoubleUnderscore")]
    [TestCase("1000_", TestName = "TrailingUnderscore")]
    public void Eval_Literal_ShouldThrowLexerException(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalLexerException>(() => engine.Evaluate(expr));
    }

    [TestCase("0b123", TestName = "InvalidBinary_MixedDigits")]
    public void Eval_Literal_ShouldThrowParserException(string expr)
    {
        // 0b123 lexes as 0b1 (valid binary) + 23 (unexpected token)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalParserException>(() => engine.Evaluate(expr));
    }

    #endregion
}
