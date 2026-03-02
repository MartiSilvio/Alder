namespace CsEval.Test.Extensions;

// Chained comparisons (Python/Julia)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ChainedComparisonTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Basic two-comparison chains

    [Test]
    public void Eval_ChainedLess_InRange_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("0 < x < 10"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_ChainedLess_AboveRange_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 15);
        Assert.That(engine.Evaluate("0 < x < 10"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_ChainedLess_AtLowerBound_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("0 < x < 10"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_ChainedLess_AtUpperBound_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 10);
        Assert.That(engine.Evaluate("0 < x < 10"), Is.EqualTo(false));
    }

    #endregion

    #region Inclusive bounds

    [Test]
    public void Eval_ChainedLessEqual_AtLowerBound_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("0 <= x <= 10"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_ChainedLessEqual_AtUpperBound_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 10);
        Assert.That(engine.Evaluate("0 <= x <= 10"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_ChainedLessEqual_InRange_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("0 <= x <= 10"), Is.EqualTo(true));
    }

    #endregion

    #region Mixed operators

    [Test]
    public void Eval_MixedChain_InRange_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("0 <= x < 10"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_MixedChain_AtUpperBound_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 10);
        Assert.That(engine.Evaluate("0 <= x < 10"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_MixedChain_AtLowerBound_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("0 <= x < 10"), Is.EqualTo(true));
    }

    #endregion

    #region Three-comparison chain

    [Test]
    public void Eval_ThreeChain_AllAscending_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 3);
        engine.SetVariable("y", 5);
        Assert.That(engine.Evaluate("0 < x < y < 10"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_ThreeChain_MiddleOutOfOrder_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 5);
        engine.SetVariable("y", 3);
        Assert.That(engine.Evaluate("0 < x < y < 10"), Is.EqualTo(false));
    }

    #endregion

    #region Equality in chain

    [Test]
    public void Eval_EqualityChain_AllEqual_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 5);
        engine.SetVariable("b", 5);
        Assert.That(engine.Evaluate("a == b == 5"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualityChain_NotEqual_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 5);
        engine.SetVariable("b", 6);
        Assert.That(engine.Evaluate("a == b == 5"), Is.EqualTo(false));
    }

    #endregion

    #region Single-evaluation guarantee

    [Test]
    public void Eval_ChainedComparison_MiddleOperandEvaluatedOnce()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var counter = 0;
            var f = () => { counter = counter + 1; return counter; };
            var result = 0 < f() < 10;
            result.ToString() + "," + counter.ToString()
            """);
        // f() returns 1 (> 0 and < 10 => true), counter should be 1 (called once)
        Assert.That(result, Is.EqualTo("True,1"));
    }

    #endregion

    #region Short-circuit behavior

    [Test]
    public void Eval_ChainedComparison_ShortCircuitsOnFalse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var counter = 0;
            var g = () => { counter = counter + 1; return 100; };
            var result = 5 < 3 < g();
            result.ToString() + "," + counter.ToString()
            """);
        // 5 < 3 is false, short-circuits. g() never called. counter remains 0.
        Assert.That(result, Is.EqualTo("False,0"));
    }

    #endregion

    #region Floating point

    [Test]
    public void Eval_ChainedComparison_WithDoubles_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 3.14);
        Assert.That(engine.Evaluate("0.0 < x < 10.0"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_ChainedComparison_WithNaN_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", double.NaN);
        Assert.That(engine.Evaluate("0 < x < 10"), Is.EqualTo(false));
    }

    #endregion

    #region Standard mode

    [Test]
    public void Eval_StandardMode_ChainedComparison_ThrowsError()
    {
        // In Standard mode, 0 < x < 10 parses as (0 < x) < 10 => true < 10 => type error
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        engine.SetVariable("x", 5);
        Assert.That(() => engine.Evaluate("0 < x < 10"), Throws.Exception);
    }

    #endregion
}
