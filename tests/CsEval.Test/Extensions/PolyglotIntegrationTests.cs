namespace CsEval.Test.Extensions;

// Polyglot extended features -- cross-feature integration
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class PolyglotIntegrationTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Bare math + implicit multiply

    [Test]
    public void Eval_2pi_ReturnsApprox6Point283()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("2pi");
        Assert.That((double)result!, Is.EqualTo(2 * Math.PI).Within(1e-10));
    }

    [Test]
    public void Eval_2sinX_Returns2TimesSin1()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 1.0);
        var result = engine.Evaluate("2sin(x)");
        Assert.That((double)result!, Is.EqualTo(2 * Math.Sin(1.0)).Within(1e-10));
    }

    [Test]
    public void Eval_2sqrt9_Returns6()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("2sqrt(9.0)");
        Assert.That((double)result!, Is.EqualTo(6.0).Within(1e-10));
    }

    [Test]
    public void Eval_3e_Returns3TimesEuler()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("3e");
        Assert.That((double)result!, Is.EqualTo(3 * Math.E).Within(1e-10));
    }

    #endregion

    #region Range + LINQ method syntax

    [Test]
    public void Eval_Range1To100_WhereEven_Count_Returns50()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..100).Where(x => x % 2 == 0).Count()");
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Eval_Range1To10_SelectSquared_Sum_Returns385()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).Select(x => x ** 2).Sum()");
        Assert.That(result, Is.EqualTo(385));
    }

    [Test]
    public void Eval_Range1To10_Aggregate_Returns55()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).Aggregate(0, (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region Range + foreach + chained comparison

    [Test]
    public void Eval_ForeachRange_ChainedComparison_CountsValues11To19()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var count = 0;
            foreach (var x in 1..100) {
                if (10 < x < 20) { count = count + 1; }
            }
            count
            """);
        Assert.That(result, Is.EqualTo(9));
    }

    #endregion

    #region Pipeline + bare math

    [Test]
    public void Eval_Pipeline_SinLambda_ReturnsSinOfHalf()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("0.5 |> (x => sin(x))");
        Assert.That((double)result!, Is.EqualTo(Math.Sin(0.5)).Within(1e-10));
    }

    [Test]
    public void Eval_Pipeline_SinThenAbs_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("pi / 4 |> (x => sin(x)) |> (x => abs(x))");
        Assert.That((double)result!, Is.EqualTo(Math.Abs(Math.Sin(Math.PI / 4))).Within(1e-10));
    }

    #endregion

    #region Pipeline + range + LINQ

    [Test]
    public void Eval_PipelineRangeToLINQ_FiltersGreaterThan5()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10) |> (r => r.Where(x => x > 5).ToList())");
        var list = (List<int>)result!;
        Assert.That(list, Has.Count.EqualTo(5));
        Assert.That(list, Is.EqualTo(new List<int> { 6, 7, 8, 9, 10 }));
    }

    #endregion

    #region Implicit multiply + chained comparison

    [Test]
    public void Eval_ChainedComparison_ImplicitMultiply_InRange_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 3);
        var result = engine.Evaluate("0 < 2x < 20");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Eval_ChainedComparison_ImplicitMultiply_OutOfRange_ReturnsFalse()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 15);
        var result = engine.Evaluate("0 < 2x < 20");
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region Regex + pipeline

    [Test]
    public void Eval_Pipeline_RegexMatch_ReturnsTrue()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"hello123\" |> (s => s =~ \"\\\\d+\")");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Spaceship + variables

    [Test]
    public void Eval_Spaceship_LessThan_ReturnsNeg1()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1 <=> 2)");
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void Eval_Spaceship_GreaterThan_Returns1()
    {
        var engine = CreateEngine();
        engine.SetVariable("a", 5);
        engine.SetVariable("b", 3);
        var result = engine.Evaluate("a <=> b");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region Unless + bare math

    [Test]
    public void Eval_Unless_WithPi_ExecutesBodyWhenConditionFalse()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var x = pi;
            unless (x < 3) { x = 0.0; }
            x
            """);
        Assert.That(result, Is.EqualTo(0.0));
    }

    #endregion

    #region Until + range

    [Test]
    public void Eval_Until_SumRangeItems_Returns55()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var sum = 0;
            var items = (1..10).ToList();
            var i = 0;
            until (i >= items.Count) {
                sum = sum + (int)items[i];
                i = i + 1;
            }
            sum
            """);
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region Slice step + range

    [Test]
    public void Eval_RangeToList_SliceStep3_ReturnsEveryThird()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..20).ToList()[::3]");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 4, 7, 10, 13, 16, 19 }));
    }

    [Test]
    public void Eval_RangeToList_SliceReverse_ReturnsReversed()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("(1..10).ToList()[::-1]");
        Assert.That(result, Is.EqualTo(new List<int> { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }));
    }

    #endregion

    #region Showcase expression (Gaussian readability)

    [Test]
    public void Eval_GaussianShowcase_CorrectMathResult()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("""
            var x = 0.5;
            var result = 2sin(pi * x) + 3cos(2pi * x);
            result
            """);
        var expected = 2 * Math.Sin(Math.PI * 0.5) + 3 * Math.Cos(2 * Math.PI * 0.5);
        Assert.That((double)result!, Is.EqualTo(expected).Within(1e-10));
    }

    #endregion

    #region Standard mode isolation

    [Test]
    public void Eval_StandardMode_BareMath_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("sin(1.0)"),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_StandardMode_ImplicitMultiply_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        engine.SetVariable("x", 5);
        Assert.That(() => engine.Evaluate("2x"),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_StandardMode_Range_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("(1..10).Count()"),
            Throws.InstanceOf<CsEvalException>());
    }

    [Test]
    public void Eval_StandardMode_Pipeline_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("5 |> (x => x * 2)"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    [Test]
    public void Eval_StandardMode_ChainedComparison_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        engine.SetVariable("x", 5);
        Assert.That(() => engine.Evaluate("0 < x < 10"), Throws.Exception);
    }

    [Test]
    public void Eval_StandardMode_Spaceship_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("1 <=> 2"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    [Test]
    public void Eval_StandardMode_Regex_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("\"hello\" =~ \"hel\""),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    [Test]
    public void Eval_StandardMode_Slice_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.Catch<CsEvalException>(() => engine.Evaluate("new[] { 1, 2, 3 }[::2]"));
    }

    [Test]
    public void Eval_StandardMode_Unless_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("unless (true) { }"), Throws.Exception);
    }

    [Test]
    public void Eval_StandardMode_Until_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("until (true) { }"), Throws.Exception);
    }

    [Test]
    public void Eval_StandardMode_Power_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.That(() => engine.Evaluate("2 ** 3"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    [Test]
    public void Eval_StandardMode_Spread_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        Assert.Catch<CsEvalException>(() =>
            engine.Evaluate("var a = new[] { 1 }; var b = [..a, 2]; b.Length"));
    }

    [Test]
    public void Eval_StandardMode_ComplexExpression_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
            { CompilationMode = mode, LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate("var x = 10; var y = x > 5 ? x * 2 : x / 2; y");
        Assert.That(result, Is.EqualTo(20));
    }

    #endregion
}
