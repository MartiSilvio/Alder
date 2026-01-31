namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ControlFlowTests(CompilationMode mode) 
{
    #region Ternary

    [Test]
    public void Eval_Ternary_True()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("true ? 1 : 2"), Is.EqualTo(1));
    }

    [Test]
    public void Eval_Ternary_False()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("false ? 1 : 2"), Is.EqualTo(2));
    }

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
        var result = engine.Evaluate("true ? [] : [1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    #endregion

    #region Blocks

    [Test]
    public void Eval_Block_WithReturn()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 5; return x * 2; }");
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region If Statements

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = null; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 10; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesThenBranch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = true; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesElseBranch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = false; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Eval_IfStatement_WithBlock()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"{
            var x = 5;
            if (x > 3) {
                var y = x * 2;
                return y;
            }
            return 0;
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_IfStatement_WithElseBlock()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"{
            var x = 1;
            if (x > 3) {
                return 100;
            } else {
                return 200;
            }
        }");
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Eval_IfStatement_NestedIf()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"{
            var x = 10;
            var y = 20;
            if (x > 5) {
                if (y > 15) return 1;
                else return 2;
            }
            return 3;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Eval_IfStatement_ElseIf()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"{
            var x = 2;
            if (x == 1) return ""one"";
            else if (x == 2) return ""two"";
            else return ""other"";
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Eval_IfStatement_NoReturn_ContinuesExecution()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"{
            var x = 5;
            if (x < 3) {
                return 0;
            }
            return x * 2;
        }");
        Assert.That(result, Is.EqualTo(10));
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
