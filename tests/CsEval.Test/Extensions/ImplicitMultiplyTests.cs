namespace CsEval.Test.Extensions;

// Implicit multiplication (Julia/MATLAB)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ImplicitMultiplyTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Basic implicit multiply

    [Test]
    public void Eval_2x_Returns10()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("2x");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_3x_Returns15()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("3x");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Eval_3Point5y_Returns8Point75()
    {
        var engine = CreateEngine();
        engine.SetVariable("y", 2.5);
        var result = engine.Evaluate("3.5y");
        Assert.That(result, Is.EqualTo(8.75));
    }

    [Test]
    public void Eval_2xPlus1_Returns11()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("2x + 1");
        Assert.That(result, Is.EqualTo(11));
    }

    #endregion

    #region Implicit multiply with parens

    [Test]
    public void Eval_2TimesParenXPlus1_Returns8()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 3);
        var result = engine.Evaluate("2(x + 1)");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Eval_5TimesParenX_Returns10()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 2);
        var result = engine.Evaluate("5(x)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_2Times3InParens_Returns6()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("2(3)");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Identifier stays whole

    [Test]
    public void Eval_2xy_TreatsXyAsOneIdentifier()
    {
        var engine = CreateEngine();
        engine.SetVariable("xy", 10);
        var result = engine.Evaluate("2xy");
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Eval_4abc_TreatsAbcAsOneIdentifier()
    {
        var engine = CreateEngine();
        engine.SetVariable("abc", 3);
        var result = engine.Evaluate("4abc");
        Assert.That(result, Is.EqualTo(12));
    }

    #endregion

    #region Precedence

    [Test]
    public void Eval_2xPow2_IsTwoTimesXSquared()
    {
        // 2x ** 2 = 2 * (x ** 2) = 2 * 9 = 18, not (2*x) ** 2 = 36
        var engine = CreateEngine();
        engine.SetVariable("x", 3);
        var result = engine.Evaluate("2x ** 2");
        Assert.That(result, Is.EqualTo(18.0));
    }

    [Test]
    public void Eval_2xPlus3y_Returns19()
    {
        // 2x + 3y = (2*x) + (3*y) = 10 + 9 = 19
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        engine.SetVariable("y", 3);
        var result = engine.Evaluate("2x + 3y");
        Assert.That(result, Is.EqualTo(19));
    }

    [Test]
    public void Eval_ImplicitMultiplySamePrecedenceAsExplicit()
    {
        // 2x / 2 = (2 * x) / 2 = 10 / 2 = 5, not 2 * (x / 2)
        // Actually implicit multiply fires once (2x), then / 2 is normal precedence
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("2x / 2");
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Scientific notation preserved

    [Test]
    public void Eval_2e10_IsScientificNotation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("2e10");
        Assert.That(result, Is.EqualTo(20000000000.0));
    }

    [Test]
    public void Eval_1E5_IsScientificNotation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("1E5");
        Assert.That(result, Is.EqualTo(100000.0));
    }

    [Test]
    public void Eval_3Point14eNeg2_IsScientificNotation()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("3.14e-2");
        Assert.That((double)result!, Is.EqualTo(0.0314).Within(1e-10));
    }

    #endregion

    #region Non-implicit-multiply cases

    [Test]
    public void Eval_LambdaCallNotImplicitMultiply()
    {
        // x(5) where x is a lambda should be function call, not multiply
        var engine = CreateEngine();
        engine.SetVariable("x", (Func<int, int>)(n => n * 10));
        var result = engine.Evaluate("x(5)");
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Eval_DelegateInvocationNotImplicitMultiply()
    {
        // (a+b)(c) should be invocation if result is callable, not multiply
        // Here we set up a scenario where identifier-paren stays as function call
        var engine = CreateEngine();
        engine.SetVariable("f", (Func<int, int>)(n => n + 1));
        var result = engine.Evaluate("f(10)");
        Assert.That(result, Is.EqualTo(11));
    }

    #endregion

    #region With bare math names

    [Test]
    public void Eval_2pi_ReturnsApprox6Point283()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("2pi");
        Assert.That((double)result!, Is.EqualTo(2 * Math.PI).Within(1e-10));
    }

    [Test]
    public void Eval_2e_ReturnsTwoTimesEuler()
    {
        // 2e where e is followed by non-digit: lexer emits "2" and "e" separately
        // In Extended mode, e resolves to Math.E, so 2e = 2 * Math.E
        var engine = CreateEngine();
        var result = engine.Evaluate("2e");
        Assert.That((double)result!, Is.EqualTo(2 * Math.E).Within(1e-10));
    }

    #endregion

    #region Standard mode unaffected

    [Test]
    public void Eval_StandardMode_2xDoesNotImplicitMultiply()
    {
        // In Standard mode, 2x should NOT produce implicit multiply -- it should error
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        engine.SetVariable("x", 5);
        Assert.That(() => engine.Evaluate("2x"), Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_StandardMode_2ParenExprDoesNotImplicitMultiply()
    {
        // In Standard mode, 2(3) should error, not implicit multiply
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        Assert.That(() => engine.Evaluate("2(3)"), Throws.InstanceOf<CsEvalException>());
    }

    #endregion

    #region Edge cases

    [Test]
    public void Eval_0xFF_HexLiteralStillWorks()
    {
        // Hex literals must not be affected by implicit multiply
        var engine = CreateEngine();
        var result = engine.Evaluate("0xFF");
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void Eval_ImplicitMultiplyDoesNotChain()
    {
        // After 2x, the result (2*x) should NOT trigger further implicit multiply
        // with the next identifier. 2x + y should be (2*x) + y, never 2*x*y.
        var engine = CreateEngine();
        engine.SetVariable("x", 3);
        engine.SetVariable("y", 100);
        var result = engine.Evaluate("2x + y");
        Assert.That(result, Is.EqualTo(106)); // (2*3) + 100 = 106
    }

    [Test]
    public void Eval_ZeroTimesVariable()
    {
        // 0y should be 0 * y = 0 (not hex prefix issue since 'y' is not 'x')
        var engine = CreateEngine();
        engine.SetVariable("y", 42);
        var result = engine.Evaluate("0y");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_NegativeImplicitMultiply_RequiresParens()
    {
        // -(2x) = -(2*x) = -10. Unary minus wraps the implicit multiply result.
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("-(2x)");
        Assert.That(result, Is.EqualTo(-10));
    }

    [Test]
    public void Eval_ImplicitMultiplyInComplexExpression()
    {
        // 3x + 2y - 4z
        var engine = CreateEngine();
        engine.SetVariable("x", 2);
        engine.SetVariable("y", 3);
        engine.SetVariable("z", 1);
        var result = engine.Evaluate("3x + 2y - 4z");
        Assert.That(result, Is.EqualTo(8)); // 6 + 6 - 4 = 8
    }

    #endregion
}
