namespace CsEval.Test.Extensions;

// Unless/until control flow (Ruby/Perl)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class UnlessUntilTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Unless -- condition false, body executes

    [Test]
    public void Eval_Unless_ConditionFalse_ExecutesBody()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 5; unless (x > 10) { x = 0; } x");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_Unless_ConditionTrue_SkipsBody()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 15; unless (x > 10) { x = 0; } x");
        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region Unless with else

    [Test]
    public void Eval_Unless_ConditionFalse_ExecutesBody_NotElse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 5; unless (x > 10) { x = 0; } else { x = 99; } x");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_Unless_ConditionTrue_ExecutesElse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 15; unless (x > 10) { x = 0; } else { x = 99; } x");
        Assert.That(result, Is.EqualTo(99));
    }

    #endregion

    #region Until -- loops while condition is false

    [Test]
    public void Eval_Until_LoopsWhileConditionFalse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 0; until (x >= 5) { x = x + 1; } x");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Eval_Until_ConditionAlreadyTrue_NeverExecutes()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 10; until (x >= 5) { x = x + 1; } x");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_Until_Sum1To10_Returns55()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var sum = 0; var i = 1; until (i > 10) { sum = sum + i; i = i + 1; } sum");
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region Nested unless/until

    [Test]
    public void Eval_UnlessContainingUntil_Nested()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var x = 0; unless (false) { until (x >= 3) { x = x + 1; } } x");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_Unless_ThrowsError()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        Assert.That(() => engine.Evaluate("unless (true) { }"), Throws.Exception);
    }

    [Test]
    public void Eval_StandardMode_Until_ThrowsError()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        Assert.That(() => engine.Evaluate("until (true) { }"), Throws.Exception);
    }

    #endregion
}
