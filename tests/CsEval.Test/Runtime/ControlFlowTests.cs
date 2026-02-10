namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.18 — Conditional operator (?:), §13.6 — Selection statements (if/else).
/// All tests engine-only: SetVariable with long, CsEval [] collection expression syntax,
/// TestPerson + anonymous object merge (non-serializable types).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ControlFlowTests(CompilationMode mode)
{
    #region Engine-only: SetVariable and CsEval-specific syntax

    // Engine-only: SetVariable with long type
    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10L);
        Assert.That(engine.Evaluate("x > 5 ? \"big\" : \"small\""), Is.EqualTo("big"));
    }

    // Engine-only: CsEval [] collection expression syntax (Roslyn rejects CS9176)
    [Test]
    public void Eval_Ternary_WithArrays()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ? [] : [1, 2, 3]");
        Assert.That(result, Is.InstanceOf<Array>());
        Assert.That(result, Has.Length.EqualTo(0));
    }

    // Engine-only: SetVariable with TestPerson (non-serializable) + anonymous object merge
    [Test]
    public void Eval_IfStatement_NullCheck_Pattern()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate(@"{
            var p = person;
            if (p == null) return null;
            return p + new { Extra = ""test"" };
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Extra"], Is.EqualTo("test"));
    }

    #endregion
}
