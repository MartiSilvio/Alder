namespace CsEval.Test;

/// <summary>
/// Pre-implementation verification: validates that LINQ methods required for
/// query expression desugaring work correctly in all 3 compilation modes.
/// ECMA-334 §12.20 - Query expressions desugar to these method calls.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class QueryPreVerificationTests(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        LanguageMode = LanguageMode.Standard
    };

    [Test]
    public void WhereSelectChain()
    {
        var engine = new CsEvalEngine(Options);
        var result = engine.Evaluate("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 3).Select(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 8, 10 }));
    }

    [Test]
    public void SelectManySingleArg()
    {
        var engine = new CsEvalEngine(Options);
        var result = engine.Evaluate("new[] { 1, 2, 3 }.SelectMany(x => new[] { x, x * 10 }).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 10, 2, 20, 3, 30 }));
    }

    [Test]
    public void SelectManyWithResultSelector()
    {
        var engine = new CsEvalEngine(Options);
        var result = engine.Evaluate(
            "new[] { 1, 2 }.SelectMany(x => new[] { \"a\", \"b\" }, (x, y) => x + y).ToList()");
        Assert.That(result, Is.EqualTo(new List<string> { "1a", "1b", "2a", "2b" }));
    }

    [Test]
    public void AnonymousObjectMemberAccess()
    {
        var engine = new CsEvalEngine(Options);
        var result = engine.Evaluate("new { x = 1, y = 2 }.x");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void NestedAnonymousObjects()
    {
        var engine = new CsEvalEngine(Options);
        var result = engine.Evaluate("new { inner = new { val = 42 } }.inner.val");
        Assert.That(result, Is.EqualTo(42));
    }
}
