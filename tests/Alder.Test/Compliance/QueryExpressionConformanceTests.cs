using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class QueryExpressionConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    [Test]
    public void Query_OrderBy()
    {
        var result = Eval(@"
            var nums = new[] { 3, 1, 4, 1, 5 };
            var sorted = (from n in nums orderby n select n).ToList();
            return sorted[0] * 10000 + sorted[1] * 1000 + sorted[2] * 100 + sorted[3] * 10 + sorted[4];
        ");
        Assert.That(result, Is.EqualTo(11345));
    }

    [Test]
    public void Query_GroupBy()
    {
        var result = Eval(@"
            var nums = new[] { 1, 2, 3, 4, 5, 6 };
            var groups = (from n in nums group n by n % 2).ToList();
            return groups.Count;
        ");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Query_Let()
    {
        var result = Eval(@"
            var words = new[] { ""hello"", ""world"" };
            var result = (from w in words let upper = w.ToUpper() select upper).ToList();
            return result[0];
        ");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Query_MultipleFrom()
    {
        var result = Eval(@"
            var a = new[] { 1, 2 };
            var b = new[] { 10, 20 };
            var result = (from x in a from y in b select x + y).ToList();
            return result.Count;
        ");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void WhereSelectChain()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 3).Select(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 8, 10 }));
    }

    [Test]
    public void SelectManySingleArg()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate("new[] { 1, 2, 3 }.SelectMany(x => new[] { x, x * 10 }).ToList()");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 10, 2, 20, 3, 30 }));
    }

    [Test]
    public void SelectManyWithResultSelector()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate(
            "new[] { 1, 2 }.SelectMany(x => new[] { \"a\", \"b\" }, (x, y) => x + y).ToList()");
        Assert.That(result, Is.EqualTo(new List<string> { "1a", "1b", "2a", "2b" }));
    }

    [Test]
    public void AnonymousObjectMemberAccess()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate("new { x = 1, y = 2 }.x");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void NestedAnonymousObjects()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Standard });
        var result = engine.Evaluate("new { inner = new { val = 42 } }.inner.val");
        Assert.That(result, Is.EqualTo(42));
    }
}
