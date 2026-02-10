namespace CsEval.Test.Runtime;

/// <summary>
/// All tests engine-only: CsEval-specific [...] collection expression syntax (Roslyn rejects CS9176
/// without target type), anonymous objects as mutable IDictionary (not value-comparable).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CollectionTests(CompilationMode mode)
{
    #region Engine-only: CsEval [] collection expression syntax (Roslyn rejects CS9176)

    // Engine-only: CsEval [] syntax for typed array literal
    [Test]
    public void Eval_ArrayLiteral()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(1));
    }

    // Engine-only: CsEval [] syntax for string array type inference
    [Test]
    public void Eval_ArrayLiteral_TypeInference_Strings()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\"a\", \"b\", \"c\"]");
        Assert.That(result, Is.TypeOf<string[]>());
    }

    // Engine-only: CsEval [] syntax for mixed-type fallback
    [Test]
    public void Eval_ArrayLiteral_MixedTypes_FallbackToObjectList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, \"two\", 3.0]");
        Assert.That(result, Is.TypeOf<object?[]>());
    }

    // Engine-only: CsEval [] syntax with nulls and value types
    [Test]
    public void Eval_ArrayLiteral_WithNulls_ValueType_CreatesNullableList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, null, 3]");
        Assert.That(result, Is.TypeOf<int?[]>());
    }

    // Engine-only: CsEval [] syntax with nulls and reference types
    [Test]
    public void Eval_ArrayLiteral_WithNulls_ReferenceType_TypedList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\"a\", null, \"c\"]");
        Assert.That(result, Is.TypeOf<string[]>());
    }

    // Engine-only: CsEval [] syntax for empty array
    [Test]
    public void Eval_ArrayLiteral_Empty_ReturnsObjectList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[]");
        Assert.That(result, Is.TypeOf<object?[]>());
    }

    // Engine-only: CsEval [] syntax with multiline formatting
    [Test]
    public void Eval_ArrayLiteral_Multiline()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"[
    ""one"",
    ""two"",
    ""three""
]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo("one"));
        Assert.That(list[1], Is.EqualTo("two"));
        Assert.That(list[2], Is.EqualTo("three"));
    }

    // Engine-only: CsEval [] syntax with CRLF line endings
    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    #endregion
}
