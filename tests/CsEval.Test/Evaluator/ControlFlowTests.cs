namespace CsEval.Test.Evaluator;

[TestFixture]
public class ControlFlowTests : EvaluatorTestBase
{
    #region Ternary

    [Test]
    public void Eval_Ternary_True()
    {
        Assert.That(Eval("true ? 1 : 2"), Is.EqualTo(1));
    }

    [Test]
    public void Eval_Ternary_False()
    {
        Assert.That(Eval("false ? 1 : 2"), Is.EqualTo(2));
    }

    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var context = new EvalContext();
        context.Define("x", 10L);
        Assert.That(Eval("x > 5 ? \"big\" : \"small\"", context), Is.EqualTo("big"));
    }

    [Test]
    public void Eval_Ternary_WithArrays()
    {
        var result = Eval("true ? [] : [1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    #endregion

    #region Blocks

    [Test]
    public void Eval_Block_WithReturn()
    {
        var result = Eval("{ var x = 5; return x * 2; }");
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region If Statements

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionTrue()
    {
        var result = Eval("{ var x = null; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionFalse()
    {
        var result = Eval("{ var x = 10; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesThenBranch()
    {
        var result = Eval("{ var x = true; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesElseBranch()
    {
        var result = Eval("{ var x = false; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Eval_IfStatement_WithBlock()
    {
        var result = Eval(@"{
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
        var result = Eval(@"{
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
        var result = Eval(@"{
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
        var result = Eval(@"{
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
        var result = Eval(@"{
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
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval(@"{
            var p = person;
            if (p == null) return null;
            return p + new { Extra = ""test"" };
        }", context) as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Extra"], Is.EqualTo("test"));
    }

    #endregion
}
