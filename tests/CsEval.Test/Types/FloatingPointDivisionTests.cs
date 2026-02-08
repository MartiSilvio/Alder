namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §12.10.3 -- Division operator, §12.10.4 -- Remainder operator.
/// Tests floating-point division and modulo by zero semantics.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN per IEEE 754.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class FloatingPointDivisionTests(CompilationMode mode)
{
    #region ECMA-334 §12.10.3 -- Floating-Point Division by Zero

    // NaN tests need special handling (NaN != NaN)
    [TestCase("0.0 / 0.0", TestName = "Division_ZeroByZero_NaN")]
    [TestCase("5.0 % 0.0", TestName = "Modulo_DoubleByZero_NaN")]
    [TestCase("5.0f % 0.0f", TestName = "Modulo_FloatByZero_NaN")]
    public async Task NaN_MatchesCSharp(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(double.IsNaN(Convert.ToDouble(result)), $"Expected NaN for: {expr}");
        Assert.That(double.IsNaN(Convert.ToDouble(csharpResult)), $"C# should return NaN for: {expr}");
    }

    // ECMA-334 §12.10.3: Integer division by zero throws DivideByZeroException
    [Test]
    public void Division_IntByZero_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 / 0"));
    }

    [Test]
    public void Division_LongByZero_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10L / 0L"));
    }

    [Test]
    public void Modulo_IntByZero_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 % 0"));
    }

    #endregion

    #region ECMA-334 §12.10.3 -- Decimal Division by Zero

    // ECMA-334 §12.10.3: Decimal is NOT an IEEE 754 type.
    // Decimal division by zero throws DivideByZeroException, unlike float/double which return Infinity.

    [Test]
    public void Decimal_DivisionByZero_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("1m / 0m"));
    }

    [Test]
    public void Decimal_ModuloByZero_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("1m % 0m"));
    }

    #endregion

    #region ECMA-334 §12.10.3 -- NaN and Infinity Semantics

    // Infinity - Infinity = NaN
    [Test]
    public async Task Infinity_MinusInfinity_IsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("double.PositiveInfinity - double.PositiveInfinity");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("double.PositiveInfinity - double.PositiveInfinity");

        Assert.That(double.IsNaN((double)result!), "CsEval should return NaN");
        Assert.That(double.IsNaN((double)csharpResult!), "C# should return NaN");
    }

    // Infinity * 0 = NaN
    [Test]
    public async Task Infinity_TimesZero_IsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("double.PositiveInfinity * 0");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("double.PositiveInfinity * 0");

        Assert.That(double.IsNaN((double)result!), "CsEval should return NaN");
        Assert.That(double.IsNaN((double)csharpResult!), "C# should return NaN");
    }

    #endregion

    #region ECMA-334 §12.12.3 -- float.NaN Comparison Semantics

    // float.NaN arithmetic: 0.0f / 0.0f produces NaN
    [Test]
    public async Task FloatNaN_ZeroDivZero()
    {
        const string expr = "0.0f / 0.0f";
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("0.0 / 0.0");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("0.0 / 0.0");

        Assert.That(double.IsNaN((double)result!));
        Assert.That(double.IsNaN((double)csharpResult!));
    }

    [Test]
    public async Task IEEE754_NaN_Equality_Variable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nan", double.NaN);

        Assert.That(engine.Evaluate("nan == nan"), Is.False);
        Assert.That(engine.Evaluate("nan != nan"), Is.True);

        var eqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN == double.NaN");
        var neqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN != double.NaN");
        Assert.That(eqResult, Is.False);
        Assert.That(neqResult, Is.True);
    }

    [TestCase("1.0 / 0.0", double.PositiveInfinity, TestName = "IEEE754_Double_PositiveInfinity")]
    [TestCase("-1.0 / 0.0", double.NegativeInfinity, TestName = "IEEE754_Double_NegativeInfinity")]
    public async Task IEEE754_Infinity_Division(string expr, double expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(csharpResult, Is.EqualTo(expected));
    }

    #endregion
}
