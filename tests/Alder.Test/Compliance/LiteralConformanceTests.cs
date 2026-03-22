using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LiteralConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, o => o.LanguageMode = lang);

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.3 Integer literals — hex, binary, digit separators
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IntegerLiteral_Hex()
    {
        var result = Eval("0xFF");
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void IntegerLiteral_Binary()
    {
        var result = Eval("0b1010");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntegerLiteral_DigitSeparator()
    {
        var result = Eval("1_000_000");
        Assert.That(result, Is.EqualTo(1000000));
    }

    [Test]
    public void IntegerLiteral_HexDigitSeparator()
    {
        var result = Eval("0xFF_FF");
        Assert.That(result, Is.EqualTo(65535));
    }

    [Test]
    public void IntegerLiteral_BinaryDigitSeparator()
    {
        var result = Eval("0b1111_0000");
        Assert.That(result, Is.EqualTo(240));
    }

    [Test]
    public void LongLiteral_Suffix()
    {
        var result = Eval("42L");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void UintLiteral_Suffix()
    {
        var result = Eval("42U");
        Assert.That(result, Is.TypeOf<uint>());
    }

    [Test]
    public void UlongLiteral_Suffix()
    {
        var result = Eval("42UL");
        Assert.That(result, Is.TypeOf<ulong>());
    }

    [Test]
    public void FloatLiteral_Suffix()
    {
        var result = Eval("3.14f");
        Assert.That(result, Is.TypeOf<float>());
    }

    [Test]
    public void DecimalLiteral_Suffix()
    {
        var result = Eval("3.14m");
        Assert.That(result, Is.TypeOf<decimal>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.5 Character literals — escape sequences
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CharLiteral_UnicodeEscape()
    {
        var result = Eval(@"'\u0041'"); // 'A'
        Assert.That(result, Is.EqualTo('A'));
    }

    [Test]
    public void CharLiteral_HexEscape()
    {
        var result = Eval(@"'\x41'"); // 'A'
        Assert.That(result, Is.EqualTo('A'));
    }

    [Test]
    public void CharLiteral_Backslash()
    {
        var result = Eval(@"'\\'");
        Assert.That(result, Is.EqualTo('\\'));
    }

    [Test]
    public void CharLiteral_Null()
    {
        var result = Eval(@"'\0'");
        Assert.That(result, Is.EqualTo('\0'));
    }

    [Test]
    public void CharLiteral_Tab()
    {
        var result = Eval(@"'\t'");
        Assert.That(result, Is.EqualTo('\t'));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.6 String literals — escape sequences
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void StringLiteral_UnicodeEscape()
    {
        var result = Eval(@"""\u0048\u0065\u006C\u006C\u006F""");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void VerbatimString_DoubleQuoteEscape()
    {
        var result = Eval(@"@""He said """"hello""""""");
        Assert.That(result, Is.EqualTo("He said \"hello\""));
    }

    [Test]
    public void VerbatimString_ContainsNewlines()
    {
        var result = Eval("@\"line1\nline2\"");
        Assert.That(result!.ToString(), Does.Contain("line1"));
        Assert.That(result!.ToString(), Does.Contain("line2"));
    }
}
