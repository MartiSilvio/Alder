using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class LiteralConformanceTests(CompilationMode mode)
{

    #region §6.4.5.3 Integer literals — type suffixes

    [Test]
    public void LongLiteral_Suffix()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("42L");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void UintLiteral_Suffix()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("42U");
        Assert.That(result, Is.TypeOf<uint>());
    }

    [Test]
    public void UlongLiteral_Suffix()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("42UL");
        Assert.That(result, Is.TypeOf<ulong>());
    }

    [Test]
    public void FloatLiteral_Suffix()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("3.14f");
        Assert.That(result, Is.TypeOf<float>());
    }

    [Test]
    public void DecimalLiteral_Suffix()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("3.14m");
        Assert.That(result, Is.TypeOf<decimal>());
    }

    #endregion

    #region §6.4.5.6 String literals — verbatim

    [Test]
    public void VerbatimString_ContainsNewlines()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("@\"line1\nline2\"");
        Assert.That(result!.ToString(), Does.Contain("line1"));
        Assert.That(result!.ToString(), Does.Contain("line2"));
    }

    #endregion

    #region §6.4.5 — Invalid Literal Error Cases

    [TestCase("0xGG", TestName = "InvalidHex")]
    [TestCase("1000_", TestName = "TrailingUnderscore")]
    public void Eval_Literal_ShouldThrowOnInvalidLexerInput(string expr)
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1013));
    }

    [TestCase("0b123", TestName = "InvalidBinary_MixedDigits")]
    public void Eval_Literal_ShouldThrowOnInvalidParserInput(string expr)
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
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
