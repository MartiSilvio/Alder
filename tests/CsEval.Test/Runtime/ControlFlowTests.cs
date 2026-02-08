namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.18 — Conditional operator (?:), §13.6 — Selection statements (if/else).
/// Tests ternary expressions, if/else statements, block expressions with return,
/// conditional type promotion (§12.4.7.3), and nested ternary evaluation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ControlFlowTests(CompilationMode mode)
{
    #region ECMA-334 §12.18 — Ternary and If/Else with External Variables

    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10L);
        Assert.That(engine.Evaluate("x > 5 ? \"big\" : \"small\""), Is.EqualTo("big"));
    }

    [Test]
    public void Eval_Ternary_WithArrays()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ? [] : [1, 2, 3]");
        Assert.That(result, Is.InstanceOf<Array>());
        Assert.That(result, Has.Length.EqualTo(0));
    }

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
