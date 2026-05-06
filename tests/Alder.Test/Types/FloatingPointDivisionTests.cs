using Alder.Test._Infrastructure;

namespace Alder.Test.Types;

/// <summary>
/// ECMA-334 §12.10.3 -- Division operator, §12.10.4 -- Remainder operator.
/// Tests floating-point division and modulo by zero semantics.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN per IEEE 754.
///
/// Engine-only tests retained here cover: NaN special cases (NaN != NaN breaks parity equality),
/// DivideByZeroException assertions, float tolerance, and SetVariable-based tests.
/// Standard floating-point expressions are in TestData/FloatingPointDivision/*.csx.
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class FloatingPointDivisionTests(CompilationMode mode)
{
    #region ECMA-334 §12.10.3 -- Floating-Point Division by Zero (NaN)

    // Engine-only: NaN special cases -- NaN != NaN breaks parity equality checks
    [TestCase("0.0 / 0.0", TestName = "Division_ZeroByZero_NaN")]
    [TestCase("5.0 % 0.0", TestName = "Modulo_DoubleByZero_NaN")]
    [TestCase("5.0f % 0.0f", TestName = "Modulo_FloatByZero_NaN")]
    public async Task NaN_MatchesCSharp(string expr)
    {
        // Engine-only: NaN special case -- requires double.IsNaN check instead of Is.EqualTo
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(double.IsNaN(Convert.ToDouble(result)), $"Expected NaN for: {expr}");
        Assert.That(double.IsNaN(Convert.ToDouble(csharpResult)), $"C# should return NaN for: {expr}");
    }

    #endregion

    #region ECMA-334 §12.10.3 -- Integer Division by Zero

    // Engine-only: DivideByZeroException assertion tests
    [Test]
    public void Division_IntByZero_Throws()
    {
        // Engine-only: DivideByZeroException test
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 / 0"));
    }

    [Test]
    public void Division_LongByZero_Throws()
    {
        // Engine-only: DivideByZeroException test
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10L / 0L"));
    }

    [Test]
    public void Modulo_IntByZero_Throws()
    {
        // Engine-only: DivideByZeroException test
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 % 0"));
    }

    #endregion

    #region ECMA-334 §12.10.3 -- Decimal Division by Zero

    // Engine-only: DivideByZeroException assertion tests
    [Test]
    public void Decimal_DivisionByZero_Throws()
    {
        // Engine-only: DivideByZeroException test
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("1m / 0m"));
    }

    [Test]
    public void Decimal_ModuloByZero_Throws()
    {
        // Engine-only: DivideByZeroException test
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("1m % 0m"));
    }

    #endregion

    #region ECMA-334 §12.10.3 -- NaN and Infinity Semantics

    // Engine-only: NaN special cases -- requires double.IsNaN check
    [Test]
    public async Task Infinity_MinusInfinity_IsNaN()
    {
        // Engine-only: NaN special case
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("double.PositiveInfinity - double.PositiveInfinity");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("double.PositiveInfinity - double.PositiveInfinity");

        Assert.That(double.IsNaN((double)result!), "Alder should return NaN");
        Assert.That(double.IsNaN((double)csharpResult!), "C# should return NaN");
    }

    [Test]
    public async Task Infinity_TimesZero_IsNaN()
    {
        // Engine-only: NaN special case
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("double.PositiveInfinity * 0");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("double.PositiveInfinity * 0");

        Assert.That(double.IsNaN((double)result!), "Alder should return NaN");
        Assert.That(double.IsNaN((double)csharpResult!), "C# should return NaN");
    }

    #endregion

    #region ECMA-334 §12.12.3 -- float.NaN Comparison Semantics

    [Test]
    public async Task FloatNaN_ZeroDivZero()
    {
        // Engine-only: NaN special case -- float.IsNaN check required
        const string expr = "0.0f / 0.0f";
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(float.IsNaN(Convert.ToSingle(result)), $"Expected NaN for: {expr}");
        Assert.That(float.IsNaN(Convert.ToSingle(csharpResult)), $"C# should return NaN for: {expr}");
    }

    #endregion

    #region ECMA-334 §8.3.7 -- IEEE 754 NaN/Infinity with Variables

    [Test]
    public async Task IEEE754_NaN_ZeroDivZero_Variable()
    {
        // Engine-only: NaN special case
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("0.0 / 0.0");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("0.0 / 0.0");

        Assert.That(double.IsNaN((double)result!));
        Assert.That(double.IsNaN((double)csharpResult!));
    }

    [Test]
    public async Task IEEE754_NaN_Equality_Variable()
    {
        // Engine-only: SetVariable + NaN equality semantics
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("nan", double.NaN);

        Assert.That(engine.Evaluate("nan == nan"), Is.False);
        Assert.That(engine.Evaluate("nan != nan"), Is.True);

        var eqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN == double.NaN");
        var neqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN != double.NaN");
        Assert.That(eqResult, Is.False);
        Assert.That(neqResult, Is.True);
    }

    #endregion
}
