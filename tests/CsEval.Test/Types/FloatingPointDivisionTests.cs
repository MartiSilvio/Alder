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
    public async Task Divide_DoubleByZero_ReturnsPositiveInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1.0 / 0.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Divide_NegativeDoubleByZero_ReturnsNegativeInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "-1.0 / 0.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.NegativeInfinity));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Divide_FloatByZero_ReturnsPositiveInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1.0f / 0.0f";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(float.PositiveInfinity));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Divide_ZeroDoubleByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "0.0 / 0.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.NaN));
        Assert.That(double.IsNaN((double)(await TestHelpers.EvaluateCSharpAsync(expr))!));
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
    public async Task Modulo_DoubleByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "5.0 % 0.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.NaN));
        Assert.That(double.IsNaN((double)(await TestHelpers.EvaluateCSharpAsync(expr))!));
    }

    [Test]
    public async Task Modulo_FloatByZero_ReturnsNaN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "5.0f % 0.0f";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(float.NaN));
        Assert.That(float.IsNaN((float)(await TestHelpers.EvaluateCSharpAsync(expr))!));
    }

    #endregion

    #region Mixed Types - Promotion Edge Cases

    [Test]
    public async Task Divide_IntByZeroDouble_ReturnsInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "10 / 0.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Divide_DoubleByZeroInt_ReturnsInfinity()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "10.0 / 0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion
}
