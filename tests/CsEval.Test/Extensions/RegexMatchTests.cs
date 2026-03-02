namespace CsEval.Test.Extensions;

// Regex match operator (Ruby/Perl)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class RegexMatchTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Basic matching

    [Test]
    public void Eval_EqualTilde_SubstringMatch_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"hel\""), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualTilde_AnchoredMatch_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"^hel\""), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualTilde_NoMatch_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"xyz\""), Is.EqualTo(false));
    }

    [Test]
    public void Eval_EqualTilde_FullAnchoredPattern_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"^hel.*o$\""), Is.EqualTo(true));
    }

    #endregion

    #region Negated matching

    [Test]
    public void Eval_BangTilde_NoMatch_ReturnsTrue()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" !~ \"xyz\""), Is.EqualTo(true));
    }

    [Test]
    public void Eval_BangTilde_HasMatch_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"hello\" !~ \"hel\""), Is.EqualTo(false));
    }

    #endregion

    #region Regex patterns

    [Test]
    public void Eval_EqualTilde_DigitPattern_MatchesDigits()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"abc123\" =~ \"\\\\d+\""), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualTilde_DigitPattern_NoDigits_ReturnsFalse()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"abc\" =~ \"\\\\d+\""), Is.EqualTo(false));
    }

    [Test]
    public void Eval_EqualTilde_DatePattern_MatchesDate()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"2024-01-15\" =~ \"\\\\d{4}-\\\\d{2}-\\\\d{2}\""), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualTilde_EmailPattern_MatchesEmail()
    {
        var engine = CreateEngine();
        Assert.That(engine.Evaluate("\"test@email.com\" =~ \"@.*\\\\.\""), Is.EqualTo(true));
    }

    #endregion

    #region With variables

    [Test]
    public void Eval_EqualTilde_WithVariables_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("str", "hello world");
        engine.SetVariable("pattern", "world$");
        Assert.That(engine.Evaluate("str =~ pattern"), Is.EqualTo(true));
    }

    [Test]
    public void Eval_EqualTilde_WithVariables_NoMatch_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("str", "hello world");
        engine.SetVariable("pattern", "^world");
        Assert.That(engine.Evaluate("str =~ pattern"), Is.EqualTo(false));
    }

    #endregion

    #region Error cases

    [Test]
    public void Eval_EqualTilde_LeftNull_Throws()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", (object?)null);
        Assert.That(() => engine.Evaluate("x =~ \"pattern\""), Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_EqualTilde_RightNull_Throws()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", (object?)null);
        Assert.That(() => engine.Evaluate("\"hello\" =~ x"), Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_EqualTilde_RightNotString_Throws()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("\"hello\" =~ 42"), Throws.InstanceOf<CsEvalException>());
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_EqualTilde_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("\"hello\" =~ \"hel\""),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    [Test]
    public void Eval_StandardMode_BangTilde_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("\"hello\" !~ \"hel\""),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    #endregion
}
