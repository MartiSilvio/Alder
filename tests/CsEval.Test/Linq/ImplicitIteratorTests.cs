namespace CsEval.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ImplicitIteratorTests(CompilationMode mode)
{
    [Test]
    public void Where_ImplicitItPredicate_Works()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Where(it > 2).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Select_DiscardIdentifier_IsNotImplicitPlaceholder()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        // _ is C#'s discard, not a placeholder — should fail as unresolved identifier
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("numbers.Select(_ * 10).ToArray()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void Where_ExplicitItLambda_Works()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Where(it => it > 2).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Select_ExplicitDiscardLambda_Works()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Select(_ => _ * 10).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30, 40 }));
    }

    [Test]
    public void AggregateBuiltins_SumCountAvgMinMax_Work()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        Assert.That(engine.Evaluate("sum(numbers)"), Is.EqualTo(10));
        Assert.That(engine.Evaluate("count(numbers.Where(it > 2))"), Is.EqualTo(2));
        Assert.That(engine.Evaluate("avg(numbers)"), Is.EqualTo(2.5d));
        Assert.That(engine.Evaluate("min(numbers)"), Is.EqualTo(1));
        Assert.That(engine.Evaluate("max(numbers)"), Is.EqualTo(4));
    }
}
