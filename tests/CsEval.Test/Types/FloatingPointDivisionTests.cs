namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §12.10.3 — Division operator, §12.10.4 — Remainder operator.
/// Tests floating-point division and modulo by zero semantics.
/// C# semantics: integers throw DivideByZeroException, floats return Infinity/NaN per IEEE 754.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class FloatingPointDivisionTests(CompilationMode mode)
{
    #region ECMA-334 §12.10.3 — Floating-Point Division by Zero
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

    #region ECMA-334 §12.10.3 — Decimal Division by Zero

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

    #region ECMA-334 §12.10.3 — NaN and Infinity Semantics

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

    #region ECMA-334 §12.12.3 — float.NaN Comparison Semantics

    // ECMA-334 §12.12.3: float.NaN follows same IEEE 754 rules as double.NaN
    [TestCase("float.NaN == float.NaN", false, TestName = "FloatNaN_EqualsItself_False")]
    [TestCase("float.NaN != float.NaN", true, TestName = "FloatNaN_NotEqualsItself_True")]
    [TestCase("float.NaN < 0f", false, TestName = "FloatNaN_LessThanZero_False")]
    [TestCase("float.NaN > 0f", false, TestName = "FloatNaN_GreaterThanZero_False")]
    [TestCase("float.NaN <= 0f", false, TestName = "FloatNaN_LessOrEqualZero_False")]
    [TestCase("float.NaN >= 0f", false, TestName = "FloatNaN_GreaterOrEqualZero_False")]
    [TestCase("0f < float.NaN", false, TestName = "ZeroFloat_LessThanNaN_False")]
    [TestCase("0f > float.NaN", false, TestName = "ZeroFloat_GreaterThanNaN_False")]
    public async Task FloatNaN_Comparison_MatchesCSharp(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Cross-type NaN -- float.NaN promoted to double via binary numeric promotion
    [TestCase("float.NaN == 0.0", false, TestName = "CrossType_FloatNaN_EqualsDoubleZero_False")]
    [TestCase("float.NaN != 0.0", true, TestName = "CrossType_FloatNaN_NotEqualsDoubleZero_True")]
    [TestCase("float.NaN < 0.0", false, TestName = "CrossType_FloatNaN_LessThanDoubleZero_False")]
    public async Task CrossType_NaN_MatchesCSharp(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

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

    #region ECMA-334 §8.3.7 — IEEE 754 NaN/Infinity with Variables

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
