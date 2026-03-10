namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5 -- Literals (integer, real, boolean, character, string, null).
///
/// Engine-only tests retained here cover lexer/parser error cases that verify
/// specific exception types (CsEvalLexerException, CsEvalParserException).
/// Standard literal expressions are in TestData/Literals/*/*.csx.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LiteralTests(CompilationMode mode)
{
    #region ECMA-334 §6.4.5 -- Invalid Literal Error Cases

    // Engine-only: lexer error tests -- verify specific CsEvalLexerException
    [TestCase("0xGG", TestName = "InvalidHex")]
    [TestCase("1000_", TestName = "TrailingUnderscore")]
    public void Eval_Literal_ShouldThrowLexerException(string expr)
    {
        // Engine-only: lexer error test
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalLexerException>(() => engine.Evaluate(expr));
    }

    // Engine-only: parser error test -- verify specific CsEvalParserException
    [TestCase("0b123", TestName = "InvalidBinary_MixedDigits")]
    public void Eval_Literal_ShouldThrowParserException(string expr)
    {
        // Engine-only: parser error test
        // 0b123 lexes as 0b1 (valid binary) + 23 (unexpected token)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEval.Parsing.CsEvalParserException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void Eval_DecimalIntegerBeyondLongMax_ParsesAsULong()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("9223372036854775808");
        Assert.That(result, Is.EqualTo(9223372036854775808UL));
        Assert.That(result, Is.TypeOf<ulong>());
    }

    #endregion
}
