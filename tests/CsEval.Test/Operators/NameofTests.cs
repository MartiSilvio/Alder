namespace CsEval.Test.Operators;

// Engine-only: CsEval resolves nameof() syntactically, Roslyn requires member to exist.
// All tests use SetVariable or reference non-existent members (obj.Property, a.b.c).

/// <summary>
/// Tests for nameof() expression (ECMA-334 §12.8.22).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NameofTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.22 - Nameof Expression

    [TestCase("nameof(x)", "x", TestName = "Nameof_SimpleIdentifier")]
    [TestCase("nameof(foo)", "foo", TestName = "Nameof_Variable")]
    public void Nameof_Basic(string expr, string expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        engine.SetVariable("foo", "bar");
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [Test]
    public void Nameof_MemberAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // nameof(obj.Property) should return "Property"
        Assert.That(engine.Evaluate("nameof(obj.Property)"), Is.EqualTo("Property"));
        Assert.That(engine.Evaluate("nameof(a.b.c)"), Is.EqualTo("c"));
    }

    #endregion
}
