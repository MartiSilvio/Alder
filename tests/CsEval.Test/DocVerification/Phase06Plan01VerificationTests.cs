namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase06Plan01VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode, new CsEvalOptions { LanguageMode = LanguageMode.Extended });

    // ═══════════════════════════════════════════════════════════════
    // EXT-01: Index — enabling Extended mode / Standard mode rejection
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ExtendedMode_Enabled_CanEvaluateExtendedExpression()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("2 ** 10"), Is.EqualTo(1024.0));
    }

    [Test]
    public void StandardMode_RejectsExtendedOperator()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.That(() => engine.Evaluate("2 ** 10"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Power Operator
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Power_Basic()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("2 ** 10"), Is.EqualTo(1024.0));
    }

    [Test]
    public void Power_UnaryMinusBindsLooser()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("-2 ** 2"), Is.EqualTo(-4.0));
    }

    [Test]
    public void Power_RightAssociative()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("2 ** 3 ** 2"), Is.EqualTo(512.0));
    }

    [Test]
    public void Power_CompoundAssignment()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("{ var x = 3.0; x **= 2; return x; }"), Is.EqualTo(9.0));
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Range Operators
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void InclusiveRange_Iteration()
    {
        var engine = Engine();
        var result = engine.Evaluate("{ var sum = 0; foreach (var i in 1..=5) sum += i; return sum; }");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ExplicitExclusiveRange_Iteration()
    {
        var engine = Engine();
        var result = engine.Evaluate("{ var sum = 0; foreach (var i in 1..<5) sum += i; return sum; }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void StandardRange_IterableInExtendedMode()
    {
        var engine = Engine();
        var result = engine.Evaluate("{ var sum = 0; foreach (var i in 1..5) sum += i; return sum; }");
        Assert.That(result, Is.EqualTo(10));
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Pipeline Operator
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Pipeline_BareMathFunction()
    {
        var engine = Engine();
        var result = (double)engine.Evaluate("3.14 |> sin")!;
        Assert.That(result, Is.EqualTo(Math.Sin(3.14)).Within(1e-10));
    }

    [Test]
    public void Pipeline_Sqrt()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("16 |> sqrt"), Is.EqualTo(4.0));
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Spaceship Operator
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Spaceship_GreaterThan()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("5 <=> 3"), Is.EqualTo(1));
    }

    [Test]
    public void Spaceship_LessThan()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("3 <=> 5"), Is.EqualTo(-1));
    }

    [Test]
    public void Spaceship_Equal()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("3 <=> 3"), Is.EqualTo(0));
    }

    [Test]
    public void Spaceship_NullBehavior()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("null <=> 5"), Is.EqualTo(-1));
        Assert.That(engine.Evaluate("5 <=> null"), Is.EqualTo(1));
        Assert.That(engine.Evaluate("null <=> null"), Is.EqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Regex Operators
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RegexMatch_True()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"^h\""), Is.True);
    }

    [Test]
    public void RegexNotMatch_True()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" !~ \"^x\""), Is.True);
    }

    [Test]
    public void RegexMatch_False()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" =~ \"^x\""), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Strict Equality
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StrictEquals_SameTypeAndValue()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
    }

    [Test]
    public void StrictEquals_DifferentTypes()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("1 === 1.0"), Is.False);
    }

    [Test]
    public void StrictEquals_NullNull()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("null === null"), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Chained Comparisons
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ChainedComparison_AllTrue()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("1 < 2 < 3"), Is.True);
    }

    [Test]
    public void ChainedComparison_LastFails()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("1 < 2 > 3"), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Containment Operators
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void In_Array()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("2 in new[] { 1, 2, 3 }"), Is.True);
    }

    [Test]
    public void In_String_Containment()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"lo\" in \"hello\""), Is.True);
    }

    [Test]
    public void NotIn_Array()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("5 not in new[] { 1, 2, 3 }"), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Like Operator
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Like_Prefix()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" like \"h%\""), Is.True);
    }

    [Test]
    public void Like_SingleCharWildcard()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" like \"h_llo\""), Is.True);
    }

    [Test]
    public void NotLike()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("\"hello\" not like \"x%\""), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Between
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Between_InRange()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("5 between 1 and 10"), Is.True);
    }

    [Test]
    public void Between_OutOfRange()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("15 between 1 and 10"), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-02: Word Operators
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Word_And()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("true and false"), Is.False);
    }

    [Test]
    public void Word_Or()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("true or false"), Is.True);
    }

    [Test]
    public void Word_Not()
    {
        var engine = Engine();
        Assert.That(engine.Evaluate("not true"), Is.False);
    }

    [Test]
    public void Word_Operators_Rejected_InStandardMode()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.That(() => engine.Evaluate("true and false"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }
}
