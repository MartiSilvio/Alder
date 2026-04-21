using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// All tests engine-only: Alder-specific [...] collection expression syntax (Roslyn rejects CS9176
/// without target type), structural projections via new { ... }.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CollectionTests(CompilationMode mode)
{

    // Engine-only: Alder [] syntax with CRLF line endings (edge case, no parity equivalent)
    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var result = engine.Evaluate("[\r\n    \"one\"\r\n]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("one"));
    }



    // Engine-only: structural projections are runtime-generated CLR objects.
    [Test]
    public void Eval_AnonymousObject()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var result = engine.Evaluate("""new { Name = "John", Age = 30 } """);
        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("John"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Age"), Is.EqualTo(30));
    }

    [Test]
    public void Eval_Comprehension_WithFilter_Works()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);

        var result = engine.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");

        Assert.That(result, Is.EqualTo(new[] { 4, 16, 36, 64, 100 }));
    }

}
