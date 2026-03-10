namespace CsEval.Test.Runtime;

/// <summary>
/// All tests engine-only: CsEval-specific [...] collection expression syntax (Roslyn rejects CS9176
/// without target type), anonymous objects as mutable IDictionary (not value-comparable).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CollectionTests(CompilationMode mode)
{
    #region Engine-only: CsEval [] collection expression syntax (Roslyn rejects CS9176)

    // Engine-only: CsEval [] syntax with CRLF line endings (edge case, no parity equivalent)
    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate("[\r\n    \"one\"\r\n]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("one"));
    }

    #endregion

    #region Engine-only: anonymous objects as mutable IDictionary (not value-comparable)

    // Engine-only: anonymous object returns IDictionary, not compiler-generated type
    [Test]
    public void Eval_AnonymousObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    #endregion
}
