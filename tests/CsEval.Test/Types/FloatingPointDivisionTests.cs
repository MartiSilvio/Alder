namespace CsEval.Test.Types;

/// <summary>
/// Tests for floating-point division and modulo by zero.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class FloatingPointDivisionTests(CompilationMode mode) 
{
    #region Division By Zero

    [Test]
    public void Divide_IntegerByZero_ThrowsDivideByZeroException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 / 0"));
    }

    [Test]
    public void Divide_LongByZero_ThrowsDivideByZeroException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10L / 0L"));
    }

    [Test]
    public void Divide_DoubleByZero_ReturnsPositiveInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Divide_NegativeDoubleByZero_ReturnsNegativeInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("-1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.NegativeInfinity));
    }

    [Test]
    public void Divide_FloatByZero_ReturnsPositiveInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("1.0f / 0.0f");
        Assert.That(result, Is.EqualTo(float.PositiveInfinity));
    }

    [Test]
    public void Divide_ZeroDoubleByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("0.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.NaN));
    }

    #endregion

    #region Modulo By Zero

    [Test]
    public void Modulo_IntegerByZero_ThrowsDivideByZeroException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate("10 % 0"));
    }

    [Test]
    public void Modulo_DoubleByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("5.0 % 0.0");
        Assert.That(result, Is.EqualTo(double.NaN));
    }

    [Test]
    public void Modulo_FloatByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("5.0f % 0.0f");
        Assert.That(result, Is.EqualTo(float.NaN));
    }

    #endregion

    #region Mixed Types - Promotion Edge Cases

    [Test]
    public void Divide_IntByZeroDouble_ReturnsInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // int / double → promoted to double, should return Infinity
        var result = engine.Evaluate("10 / 0.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Divide_DoubleByZeroInt_ReturnsInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // double / int → should still handle correctly (0 is int, but left is double)
        var result = engine.Evaluate("10.0 / 0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    #endregion
}
