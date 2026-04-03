using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ExplicitConversionConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, o => o.LanguageMode = lang);

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    #region §10.3.2 Explicit numeric conversions — truncation rules

    [Test]
    public void ExplicitConversion_DoubleToFloat_LosesPrecision()
    {
        var result = Eval(@"
            double d = 1.23456789012345;
            float f = (float)d;
            return (double)f != d;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void ExplicitConversion_IntToSbyte_Truncates()
    {
        var result = Eval("unchecked((sbyte)200)");
        Assert.That(result, Is.EqualTo(unchecked((sbyte)200)));
    }

    [Test]
    public void ExplicitConversion_IntToSbyte_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => Eval("checked((sbyte)200)"));
    }

    [Test]
    public void ExplicitConversion_IntToUshort_Truncates()
    {
        var result = Eval("unchecked((ushort)70000)");
        Assert.That(result, Is.EqualTo(unchecked((ushort)70000)));
    }

    [Test]
    public void ExplicitConversion_LongToInt_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => Eval(@"
            long x = (long)int.MaxValue + 1;
            return checked((int)x);
        "));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedNaN_Throws()
    {
        // §10.3.2: in checked context, NaN → integral throws OverflowException
        Assert.Throws<OverflowException>(() => Eval("checked((int)double.NaN)"));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedInfinity_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked((int)double.PositiveInfinity)"));
    }

    #endregion

    #region §12.10 Arithmetic — overflow semantics

    [Test]
    public void IntMultiply_Overflow_Unchecked_Wraps()
    {
        var result = Eval("unchecked(int.MaxValue * 2)");
        Assert.That(result, Is.EqualTo(unchecked(int.MaxValue * 2)));
    }

    [Test]
    public void LongAdd_Overflow_Checked_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked(long.MaxValue + 1L)"));
    }

    [Test]
    public void DecimalOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("decimal.MaxValue + 1m"));
    }

    #endregion

    #region Type coercion in assignment

    [Test]
    public void Assignment_ImplicitIntToDouble()
    {
        var result = Eval(@"
            double d = 42;
            return d;
        ");
        Assert.That(result, Is.EqualTo(42.0));
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Assignment_ImplicitIntToLong()
    {
        var result = Eval(@"
            long l = 42;
            return l;
        ");
        Assert.That(result, Is.EqualTo(42L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void Assignment_ImplicitByteToInt()
    {
        var result = Eval(@"
            byte b = 200;
            int i = b;
            return i;
        ");
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Assignment_ImplicitCharToInt()
    {
        var result = Eval(@"
            char c = 'A';
            int i = c;
            return i;
        ");
        Assert.That(result, Is.EqualTo(65));
    }

    #endregion
}
