namespace CsEval.Test.Extensions;

// Spaceship operator (Ruby/Perl/PHP)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SpaceshipOperatorTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Integer comparison

    [Test]
    public void Eval_Spaceship_LessThan_ReturnsNegativeOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 <=> 2"), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_Equal_ReturnsZero()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("2 <=> 2"), Is.EqualTo(0));
    }

    [Test]
    public void Eval_Spaceship_GreaterThan_ReturnsOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("3 <=> 2"), Is.EqualTo(1));
    }

    #endregion

    #region Double comparison

    [Test]
    public void Eval_Spaceship_DoubleLessThan_ReturnsNegativeOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1.5 <=> 2.5"), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_DoubleEqual_ReturnsZero()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("2.5 <=> 2.5"), Is.EqualTo(0));
    }

    [Test]
    public void Eval_Spaceship_DoubleGreaterThan_ReturnsOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("3.5 <=> 2.5"), Is.EqualTo(1));
    }

    #endregion

    #region String comparison

    [Test]
    public void Eval_Spaceship_StringLessThan_ReturnsNegativeOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"apple\" <=> \"banana\""), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_StringEqual_ReturnsZero()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"banana\" <=> \"banana\""), Is.EqualTo(0));
    }

    [Test]
    public void Eval_Spaceship_StringGreaterThan_ReturnsOne()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"cherry\" <=> \"banana\""), Is.EqualTo(1));
    }

    #endregion

    #region Null handling

    [Test]
    public void Eval_Spaceship_BothNull_ReturnsZero()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("null <=> null"), Is.EqualTo(0));
    }

    [Test]
    public void Eval_Spaceship_LeftNull_ReturnsNegativeOne()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", (object?)null);
        Assert.That(engine.Evaluate("x <=> 5"), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_RightNull_ReturnsOne()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", (object?)null);
        Assert.That(engine.Evaluate("5 <=> x"), Is.EqualTo(1));
    }

    #endregion

    #region With variables

    [Test]
    public void Eval_Spaceship_WithVariables_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 10);
        engine.SetVariable("b", 20);
        Assert.That(engine.Evaluate("a <=> b"), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_WithVariables_EqualValues_ReturnsZero()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 42);
        engine.SetVariable("b", 42);
        Assert.That(engine.Evaluate("a <=> b"), Is.EqualTo(0));
    }

    #endregion

    #region Mixed numeric types

    [Test]
    public void Eval_Spaceship_IntVsDouble_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1 <=> 1.5"), Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_LargeNumbers_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("1000000 <=> 999999"), Is.EqualTo(1));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_Spaceship_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("1 <=> 2"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    #endregion
}
