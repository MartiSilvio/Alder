namespace CsEval.Test.Operators;

// Engine-only: All tests use SetVariable with Exception objects (reference types not value-comparable)
// or test error cases (InvalidCastException, invalid unboxing)

/// <summary>
/// ECMA-334 §10.3 — Explicit conversions (cast expressions),
/// §10.3.7 — Unboxing conversions, §10.3.2 — Explicit numeric conversions.
/// Tests explicit casts, nullable casts, unboxing rules, and conversion edge cases.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CastTests(CompilationMode mode)
{
    #region Cast with Non-Keyword Class Types

    [Test]
    public void Cast_ClassType_Compatible()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)new ArgumentException("test"));
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Cast_ClassType_ExactMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)new Exception("test"));
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.InstanceOf<Exception>());
        Assert.That(((Exception)result!).Message, Is.EqualTo("test"));
    }

    [Test]
    public void Cast_ClassType_Incompatible_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)"hello");
        Assert.Throws<InvalidCastException>(() => engine.Evaluate("(Exception)obj"));
    }

    [Test]
    public void Cast_ClassType_NullValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region ECMA-334 §10.3.7 — Unboxing Edge Cases

    [Test]
    public async Task Unboxing_ExactTypeMatch_Required()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        Assert.That(engine.Evaluate("(int)boxed"), Is.EqualTo(42));
        Assert.Catch<InvalidCastException>(() => engine.Evaluate("(long)boxed"));

        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int)(object)42");
        Assert.That(csharpResult, Is.EqualTo(42));
    }

    [Test]
    public void Unboxing_NullableFromBoxed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        var result = engine.Evaluate("(int?)boxed");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion
}
