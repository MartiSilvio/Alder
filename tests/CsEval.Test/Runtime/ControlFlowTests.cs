namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.18 — Conditional operator (?:), §13.6 — Selection statements (if/else).
/// All tests engine-only: SetVariable with long, CsEval [] collection expression syntax,
/// TestPerson + anonymous object merge (non-serializable types).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ControlFlowTests(CompilationMode mode)
{
    #region Engine-only: SetVariable and CsEval-specific syntax

    // Engine-only: SetVariable with long type
    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10L);
        Assert.That(engine.Evaluate("x > 5 ? \"big\" : \"small\""), Is.EqualTo("big"));
    }

    // Engine-only: SetVariable with TestPerson (non-serializable) + anonymous object merge
    [Test]
    public void Eval_IfStatement_NullCheck_Pattern()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended });
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

    [Test]
    public void Eval_IfExpression_ReturnsBranchValue()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended });
        engine.SetVariable("x", -5);

        var result = engine.Evaluate("if (x > 0) x else -x");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion
}
