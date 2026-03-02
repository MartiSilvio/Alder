namespace CsEval.Test.Extensions;

// Range literals (Ruby/Kotlin/Haskell)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class RangeLiteralTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Basic inclusive ranges

    [Test]
    public void Eval_Range1To5_Count_Returns5()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..5).Count()");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Eval_Range1To10_ToList_Returns1Through10()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }

    [Test]
    public void Eval_Range1To1_Count_Returns1()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..1).Count()");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Eval_Range0To0_Count_Returns1()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(0..0).Count()");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region Exclusive ranges

    [Test]
    public void Eval_ExclusiveRange1To5_Count_Returns4()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..<5).Count()");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Eval_ExclusiveRange1To10_ToList_Returns1Through9()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..<10).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
    }

    [Test]
    public void Eval_ExclusiveRange1To1_Count_Returns0()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..<1).Count()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region Empty ranges

    [Test]
    public void Eval_ReversedRange10To1_Count_Returns0()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(10..1).Count()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_ExclusiveRange5To5_Count_Returns0()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(5..<5).Count()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region Negative ranges

    [Test]
    public void Eval_NegativeRange_Neg3To3_Count_Returns7()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(-3..3).Count()");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Eval_NegativeRange_Neg3To3_ToList_ReturnsCorrectSequence()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(-3..3).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { -3, -2, -1, 0, 1, 2, 3 }));
    }

    #endregion

    #region Foreach with range

    [Test]
    public void Eval_ForeachOverRange_AccumulatesSum()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var sum = 0; foreach (var i in 1..10) { sum = sum + i; } sum");
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region LINQ method syntax

    [Test]
    public void Eval_RangeWithWhere_CountsEvenNumbers()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..100).Where(x => x % 2 == 0).Count()");
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Eval_RangeWithSelect_SquaresElements()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).Select(x => x * x).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 4, 9, 16, 25, 36, 49, 64, 81, 100 }));
    }

    [Test]
    public void Eval_RangeSum_Returns55()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).Sum()");
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region Spread regression

    [Test]
    public void Eval_SpreadStillWorks_NotBrokenByRange()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var arr = new[] { 1, 2, 3 }; var result = [0, ..arr, 4]; result.Length");
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_RangeThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("(1..10).Count()"), Throws.InstanceOf<CsEvalException>());
    }

    #endregion
}
