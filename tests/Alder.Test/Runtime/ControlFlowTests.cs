using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// ECMA-334 §12.18 — Conditional operator (?:), §13.6 — Selection statements (if/else).
/// All tests engine-only: SetVariable with long, Alder [] collection expression syntax,
/// TestPerson variables (non-serializable types).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ControlFlowTests(CompilationMode mode)
{
    #region Engine-only: SetVariable and Alder-specific syntax

    // Engine-only: SetVariable with long type
    [Test]
    public void Eval_Ternary_ReturnsExpectedBranch()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10L);
        Assert.That(engine.Evaluate("""x > 5 ? "big" : "small" """), Is.EqualTo("big"));
    }

    // Engine-only: SetVariable with TestPerson (non-serializable)
    [Test]
    public void Eval_IfStatement_NullCheck_Pattern()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate(@"{
            var p = person;
            if (p == null) return null;
            return new { PersonName = p.Name, Extra = ""test"" };
        }");

        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "PersonName"), Is.EqualTo("John"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Extra"), Is.EqualTo("test"));
    }

    [Test]
    public void Eval_IfExpression_ReturnsBranchValue()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("x", -5);

        var result = engine.Evaluate("if (x > 0) x else -x");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion
}
