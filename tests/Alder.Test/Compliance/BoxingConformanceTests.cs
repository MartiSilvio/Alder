using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class BoxingConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, o => o.LanguageMode = lang);

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §8.3.13 Boxing/unboxing
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Boxing_IntToObject_UnboxToInt()
    {
        var result = Eval(@"
            object o = 42;
            return (int)o;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Unboxing_WrongType_Throws()
    {
        // §10.3.7: unboxing to wrong type should fail
        // Alder wraps this as AlderException, which is acceptable
        var ex = Assert.Throws<AlderException>(() => Eval(@"
            object o = 42;
            return (long)o;
        "));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030));
    }

    [Test]
    public void Boxing_NullableInt_HasValue_BoxesToInt()
    {
        // §8.3.13: boxing nullable with HasValue=true boxes the underlying value
        // GetType() is sandboxed, so test via unboxing to int (not int?)
        var result = Eval(@"
            int? x = 42;
            object o = x;
            return (int)o;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Boxing_NullableInt_Null_BoxesToNull()
    {
        var result = Eval(@"
            int? x = null;
            object o = x;
            return o == null;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §10.2.11 Implicit constant expression conversions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ImplicitConstExprConversion_IntToByteInRange()
    {
        // §10.2.11: constant int in range [0..255] → byte assignment
        var result = Eval(@"
            byte b = 200;
            return b;
        ");
        Assert.That(result, Is.EqualTo((byte)200));
    }

    [Test]
    public void ImplicitConstExprConversion_IntToSbyte()
    {
        var result = Eval(@"
            sbyte sb = -100;
            return sb;
        ");
        Assert.That(result, Is.EqualTo((sbyte)-100));
    }

    [Test]
    public void ImplicitConstExprConversion_IntToUshort()
    {
        var result = Eval(@"
            ushort us = 60000;
            return us;
        ");
        Assert.That(result, Is.EqualTo((ushort)60000));
    }
}
