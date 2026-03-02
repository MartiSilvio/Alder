namespace CsEval.Test.Extensions;

// Bare math names (Julia/MATLAB/Python)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class BareMathTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Constants

    [Test]
    public void Eval_Pi_ReturnsMathPI()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("pi");
        Assert.That(result, Is.EqualTo(Math.PI));
    }

    [Test]
    public void Eval_E_ReturnsMathE()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("e");
        Assert.That(result, Is.EqualTo(Math.E));
    }

    [Test]
    public void Eval_Tau_ReturnsMathTau()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("tau");
        Assert.That(result, Is.EqualTo(Math.Tau));
    }

    [Test]
    public void Eval_Infinity_ReturnsPositiveInfinity()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("infinity");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Eval_Nan_ReturnsNaN()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("nan");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(double.IsNaN((double)result!), Is.True);
    }

    #endregion

    #region Single-arg trig functions

    [Test]
    public void Eval_Sin0_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sin(0.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Eval_Cos0_ReturnsOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("cos(0.0)");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Eval_SinPiOver2_ReturnsOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sin(pi / 2)");
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Eval_Tan0_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("tan(0.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Eval_Asin1_ReturnsPiOver2()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("asin(1.0)");
        Assert.That((double)result!, Is.EqualTo(Math.PI / 2).Within(1e-10));
    }

    [Test]
    public void Eval_Atan0_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("atan(0.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    #endregion

    #region Other single-arg functions

    [Test]
    public void Eval_AbsNegativeInt_ReturnsPositive()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("abs(-5)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Eval_AbsNegativeDouble_ReturnsPositive()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("abs(-3.14)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_Sqrt9_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sqrt(9.0)");
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void Eval_Log1_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("log(1.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Eval_Ln1_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("ln(1.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Eval_Exp0_ReturnsOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("exp(0.0)");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Eval_Floor37_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("floor(3.7)");
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void Eval_Ceil32_ReturnsFour()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("ceil(3.2)");
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_Round35_MatchesMathRound()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("round(3.5)");
        Assert.That(result, Is.EqualTo(Math.Round(3.5)));
    }

    [Test]
    public void Eval_SignNegative_ReturnsMinusOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sign(-5.0)");
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void Eval_SignPositive_ReturnsOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sign(5.0)");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Eval_Truncate39_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("truncate(3.9)");
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void Eval_Cbrt27_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("cbrt(27.0)");
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void Eval_Log2Of8_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("log2(8.0)");
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void Eval_Log10Of1000_ReturnsThree()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("log10(1000.0)");
        Assert.That((double)result!, Is.EqualTo(3.0).Within(1e-10));
    }

    #endregion

    #region Multi-arg functions

    [Test]
    public void Eval_Round2Digits_Returns314()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("round(3.14159, 2)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_LogBase10_ReturnsTwo()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("log(100.0, 10.0)");
        Assert.That((double)result!, Is.EqualTo(2.0).Within(1e-10));
    }

    [Test]
    public void Eval_Atan2_ReturnsPiOver4()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("atan2(1.0, 1.0)");
        Assert.That((double)result!, Is.EqualTo(Math.PI / 4).Within(1e-10));
    }

    [Test]
    public void Eval_Min_ReturnsSmallerValue()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("min(3, 5)");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Eval_Max_ReturnsLargerValue()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("max(3, 5)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Eval_Pow2To10_Returns1024()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("pow(2.0, 10.0)");
        Assert.That(result, Is.EqualTo(1024.0));
    }

    [Test]
    public void Eval_Clamp_ClampsToMax()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("clamp(15, 0, 10)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_Clamp_ClampsToMin()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("clamp(-5, 0, 10)");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_Clamp_ValueInRange()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("clamp(5, 0, 10)");
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Shadowing

    [Test]
    public void Eval_UserVariableShadowsConstant()
    {
        var engine = CreateEngine();
        engine.SetVariable("pi", 3);
        var result = engine.Evaluate("pi");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Eval_UserVariableShadowsFunction()
    {
        // When a user-defined variable named "sin" exists, it takes precedence
        // over the bare math function. Here we set sin to a FunctionRef-compatible
        // host function so it can be invoked as sin(5.0).
        var engine = CreateEngine();
        engine.RegisterFunction("sin", args => 42);
        var result = engine.Evaluate("sin(5.0)");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_WithoutShadow_ConstantReturns()
    {
        // A fresh engine without user-defined pi should return Math.PI
        var engine = CreateEngine();
        var result = engine.Evaluate("pi");
        Assert.That(result, Is.EqualTo(Math.PI));
    }

    [Test]
    public void Eval_ShadowedVarDoesNotAffectNewScope()
    {
        // Shadowing via SetVariable only applies to that engine
        var engine = CreateEngine();
        engine.SetVariable("pi", 3);

        var shadowed = engine.Evaluate("pi");
        Assert.That(shadowed, Is.EqualTo(3));

        // A new engine still resolves to Math.PI
        var engine2 = CreateEngine();
        var original = engine2.Evaluate("pi");
        Assert.That(original, Is.EqualTo(Math.PI));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_SinThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("sin(1.0)"));
    }

    [Test]
    public void Eval_StandardMode_PiThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("pi"));
    }

    #endregion

    #region Composite expressions

    [Test]
    public void Eval_SinPiOver4TimesSqrt2_ReturnsApprox1()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sin(pi / 4) * sqrt(2.0)");
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Eval_NestedMathExpression()
    {
        var engine = CreateEngine();
        // log(e) should be 1.0 (natural log of e)
        var result = engine.Evaluate("log(e)");
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Eval_MathExpressionWithVariable()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 0.0);
        var result = engine.Evaluate("sin(x) + cos(x)");
        // sin(0) + cos(0) = 0 + 1 = 1
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Eval_PythagoreanIdentity()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 1.23);
        var result = engine.Evaluate("sin(x) * sin(x) + cos(x) * cos(x)");
        // sin^2(x) + cos^2(x) = 1 for all x
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-10));
    }

    #endregion

    #region Hyperbolic functions

    [Test]
    public void Eval_Sinh0_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("sinh(0.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Eval_Cosh0_ReturnsOne()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("cosh(0.0)");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void Eval_Tanh0_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("tanh(0.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    #endregion

    #region Acos function

    [Test]
    public void Eval_Acos1_ReturnsZero()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("acos(1.0)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    #endregion
}
