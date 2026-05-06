using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class ExplicitConversionConformanceTests(CompilationMode mode)
{

    #region §10.3.2 Explicit numeric conversions — truncation rules

    [Test]
    public void ExplicitConversion_IntToSbyte_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked((sbyte)200)"));
    }

    [Test]
    public void ExplicitConversion_LongToInt_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate(@"
            long x = (long)int.MaxValue + 1;
            return checked((int)x);
        "));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedNaN_Throws()
    {
        // §10.3.2: in checked context, NaN → integral throws OverflowException
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked((int)double.NaN)"));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedInfinity_Throws()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked((int)double.PositiveInfinity)"));
    }

    #endregion

    #region §12.10 Arithmetic — overflow semantics

    [Test]
    public void LongAdd_Overflow_Checked_Throws()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked(long.MaxValue + 1L)"));
    }

    [Test]
    public void DecimalOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("decimal.MaxValue + 1m"));
    }

    #endregion

    #region Type coercion in assignment

    [Test]
    public void Assignment_ImplicitIntToDouble()
    {
        var result = TestEngineFactory.Create(mode).Evaluate(@"
            double d = 42;
            return d;
        ");
        Assert.That(result, Is.EqualTo(42.0));
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Assignment_ImplicitIntToLong()
    {
        var result = TestEngineFactory.Create(mode).Evaluate(@"
            long l = 42;
            return l;
        ");
        Assert.That(result, Is.EqualTo(42L));
        Assert.That(result, Is.TypeOf<long>());
    }

    #endregion
}
