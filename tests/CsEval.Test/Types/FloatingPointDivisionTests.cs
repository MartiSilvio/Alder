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
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

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

    #region ECMA-334 Edge Cases - NaN and Infinity Semantics

    // ECMA-334 §12.10.3: NaN comparison semantics
    // NaN is not equal to any value, including itself
    [TestCase("double.NaN == double.NaN", false, TestName = "NaN_EqualsItself_False")]
    [TestCase("double.NaN != double.NaN", true, TestName = "NaN_NotEqualsItself_True")]
    [TestCase("double.NaN < 0", false, TestName = "NaN_LessThanZero_False")]
    [TestCase("double.NaN > 0", false, TestName = "NaN_GreaterThanZero_False")]
    [TestCase("double.NaN <= 0", false, TestName = "NaN_LessOrEqualZero_False")]
    [TestCase("double.NaN >= 0", false, TestName = "NaN_GreaterOrEqualZero_False")]
    [TestCase("0 < double.NaN", false, TestName = "Zero_LessThanNaN_False")]
    [TestCase("0 > double.NaN", false, TestName = "Zero_GreaterThanNaN_False")]
    public async Task NaN_Comparison_MatchesCSharp(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // ECMA-334 §12.10.3: Infinity arithmetic
    [TestCase("double.PositiveInfinity + 1", double.PositiveInfinity, TestName = "Infinity_PlusOne")]
    [TestCase("double.NegativeInfinity - 1", double.NegativeInfinity, TestName = "NegativeInfinity_MinusOne")]
    [TestCase("double.PositiveInfinity * 2", double.PositiveInfinity, TestName = "Infinity_TimesTwo")]
    [TestCase("double.PositiveInfinity / 2", double.PositiveInfinity, TestName = "Infinity_DividedByTwo")]
    [TestCase("1.0 / double.PositiveInfinity", 0.0, TestName = "One_DividedByInfinity_Zero")]
    [TestCase("double.PositiveInfinity == double.PositiveInfinity", true, TestName = "Infinity_EqualsItself")]
    [TestCase("double.PositiveInfinity > double.MaxValue", true, TestName = "Infinity_GreaterThanMaxValue")]
    public async Task Infinity_Arithmetic_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

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
}
