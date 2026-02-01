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
    // Parity tests for floating-point division (returns Infinity)
    [TestCase("1.0 / 0.0", TestName = "Division_DoubleByZero_PositiveInfinity")]
    [TestCase("-1.0 / 0.0", TestName = "Division_NegativeDoubleByZero_NegativeInfinity")]
    [TestCase("1.0f / 0.0f", TestName = "Division_FloatByZero_PositiveInfinity")]
    [TestCase("10 / 0.0", TestName = "Division_IntByZeroDouble_Infinity")]
    [TestCase("10.0 / 0", TestName = "Division_DoubleByZeroInt_Infinity")]
    public async Task MatchesCSharp(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(csharpResult), $"Value mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

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

    // Integer division by zero throws (can't parity test - Roslyn also throws)
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
}
