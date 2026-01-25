namespace CsEval.Test.Evaluator;

/// <summary>
/// Tests for floating-point division and modulo by zero.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN.
/// </summary>
[TestFixture]
public class FloatingPointDivisionTests : EvaluatorTestBase
{
    #region Division By Zero

    [Test]
    public void Divide_IntegerByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Eval("10 / 0"));
    }

    [Test]
    public void Divide_LongByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Eval("10L / 0L"));
    }

    [Test]
    public void Divide_DoubleByZero_ReturnsPositiveInfinity()
    {
        var result = Eval("1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Divide_NegativeDoubleByZero_ReturnsNegativeInfinity()
    {
        var result = Eval("-1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.NegativeInfinity));
    }

    [Test]
    public void Divide_FloatByZero_ReturnsPositiveInfinity()
    {
        var result = Eval("1.0f / 0.0f");
        Assert.That(result, Is.EqualTo(float.PositiveInfinity));
    }

    [Test]
    public void Divide_ZeroDoubleByZero_ReturnsNaN()
    {
        var result = Eval("0.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.NaN));
    }

    #endregion

    #region Modulo By Zero

    [Test]
    public void Modulo_IntegerByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Eval("10 % 0"));
    }

    [Test]
    public void Modulo_DoubleByZero_ReturnsNaN()
    {
        var result = Eval("5.0 % 0.0");
        Assert.That(result, Is.EqualTo(double.NaN));
    }

    [Test]
    public void Modulo_FloatByZero_ReturnsNaN()
    {
        var result = Eval("5.0f % 0.0f");
        Assert.That(result, Is.EqualTo(float.NaN));
    }

    #endregion

    #region IL Compiled Path

    [Test]
    public void ILCompiled_Divide_DoubleByZero_ReturnsPositiveInfinity()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("1.0 / 0.0");
        
        Assert.That(expr.TryCompile(), Is.True, "Simple division should be IL-compilable");
        
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void ILCompiled_Divide_IntegerByZero_ThrowsDivideByZeroException()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("10 / 0");
        
        Assert.That(expr.TryCompile(), Is.True);
        
        Assert.Throws<DivideByZeroException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void ILCompiled_Modulo_DoubleByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("5.0 % 0.0");
        
        Assert.That(expr.TryCompile(), Is.True);
        
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.NaN));
    }

    #endregion

    #region Mixed Types - Promotion Edge Cases

    [Test]
    public void Divide_IntByZeroDouble_ReturnsInfinity()
    {
        // int / double → promoted to double, should return Infinity
        var result = Eval("10 / 0.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Divide_DoubleByZeroInt_ReturnsInfinity()
    {
        // double / int → should still handle correctly (0 is int, but left is double)
        var result = Eval("10.0 / 0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    #endregion
}
