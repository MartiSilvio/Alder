namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5 -- Literals (integer, real, boolean, character, string, null).
///
/// Engine-only tests retained here cover lexer/parser error cases.
/// Standard literal expressions are in TestData/Literals/*/*.csx.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LiteralTests(CompilationMode mode)
{
    #region ECMA-334 §6.4.5 -- Invalid Literal Error Cases

    [TestCase("0xGG", TestName = "InvalidHex")]
    [TestCase("1000_", TestName = "TrailingUnderscore")]
    public void Eval_Literal_ShouldThrowOnInvalidLexerInput(string expr)
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<CsEvalException>(() => engine.Evaluate(expr));
    }

    [TestCase("0b123", TestName = "InvalidBinary_MixedDigits")]
    public void Eval_Literal_ShouldThrowOnInvalidParserInput(string expr)
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.Not.Null);
    }

    [Test]
    public void Eval_DecimalIntegerBeyondLongMax_ParsesAsULong()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("9223372036854775808");
        Assert.That(result, Is.EqualTo(9223372036854775808UL));
        Assert.That(result, Is.TypeOf<ulong>());
    }

    #endregion
}
