using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class NumericPromotionConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §10.2.3 Implicit numeric conversions — binary numeric promotion
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BinaryPromotion_UintPlusSbyte_ProducesLong()
    {
        // §12.4.7.3: uint + sbyte → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (sbyte)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusShort_ProducesLong()
    {
        // §12.4.7.3: uint + short → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (short)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusInt_ProducesLong()
    {
        // §12.4.7.3: uint + int → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", 5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusUint_ProducesUint()
    {
        // §12.4.7.3: uint + uint → stays uint
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (uint)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo((uint)15));
        Assert.That(result, Is.TypeOf<uint>());
    }

    [Test]
    public void BinaryPromotion_BytePlusByte_ProducesInt()
    {
        // §12.4.7.3: byte + byte → both promoted to int
        var engine = Engine();
        engine.SetVariable("a", (byte)100);
        engine.SetVariable("b", (byte)50);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(150));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_ShortPlusShort_ProducesInt()
    {
        // §12.4.7.3: short + short → both promoted to int
        var engine = Engine();
        engine.SetVariable("a", (short)100);
        engine.SetVariable("b", (short)50);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(150));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_CharPlusChar_ProducesInt()
    {
        // char is promoted to int per §12.4.7.3
        var result = Eval("'A' + 'B'");
        Assert.That(result, Is.EqualTo(131)); // 65 + 66
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_LongPlusUlong_ShouldError()
    {
        // §12.4.7.3: ulong + long → binding-time error
        var engine = Engine();
        engine.SetVariable("a", (ulong)10);
        engine.SetVariable("b", (long)5);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return a + b;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void BinaryPromotion_DecimalPlusDouble_ShouldError()
    {
        // §12.4.7.3: decimal + double → binding-time error
        var engine = Engine();
        engine.SetVariable("a", 10m);
        engine.SetVariable("b", 5.0);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return a + b;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void BinaryPromotion_DecimalPlusFloat_ShouldError()
    {
        // §12.4.7.3: decimal + float → binding-time error
        var engine = Engine();
        engine.SetVariable("a", 10m);
        engine.SetVariable("b", 5.0f);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("return a + b;"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.7.2 Unary numeric promotions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void UnaryPromotion_BitwiseComplementByte_ProducesInt()
    {
        // §12.4.7.2: ~byte → promoted to int, result is int
        var engine = Engine();
        engine.SetVariable("x", (byte)0xFF);
        var result = engine.Evaluate("return ~x;");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(~(int)(byte)0xFF));
    }

    [Test]
    public void UnaryPromotion_UnaryPlusByte_ProducesInt()
    {
        // §12.4.7.2: +byte → promoted to int
        var engine = Engine();
        engine.SetVariable("x", (byte)42);
        var result = engine.Evaluate("return +x;");
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void UnaryPromotion_NegateSbyte_ProducesInt()
    {
        // §12.4.7.2: -sbyte → promoted to int
        var engine = Engine();
        engine.SetVariable("x", (sbyte)5);
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-5));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void UnaryPromotion_NegateChar_ProducesInt()
    {
        // §12.4.7.2: -char → promoted to int
        var engine = Engine();
        engine.SetVariable("x", 'A');
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-65));
        Assert.That(result, Is.TypeOf<int>());
    }
}
