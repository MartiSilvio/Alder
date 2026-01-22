using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class NullHandlingTests : EvaluatorTestBase
{
    [Test]
    public void Eval_NullCoalesce()
    {
        var context = new EvalContext();
        context.Define("x", null);
        context.Define("y", "default");

        Assert.That(Eval("x ?? y", context), Is.EqualTo("default"));
        Assert.That(Eval("y ?? \"other\"", context), Is.EqualTo("default"));
    }

    [Test]
    public void Eval_NullSafeAccess()
    {
        var context = new EvalContext();
        context.Define("obj", null);

        Assert.That(Eval("obj?.Name", context), Is.Null);
    }

    [Test]
    public void Eval_NullCoalesceAssign_AssignsWhenNull()
    {
        var result = Eval(@"{
            var x = null;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_KeepsValueWhenNotNull()
    {
        var result = Eval(@"{
            var x = 10;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsAssignedValue()
    {
        var result = Eval(@"{
            var x = null;
            return x ??= 42;
        }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsExistingValue()
    {
        var result = Eval(@"{
            var x = ""hello"";
            return x ??= ""world"";
        }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_NullCoalesceAssign_WithExpression()
    {
        var result = Eval(@"{
            var x = null;
            x ??= 5 + 5;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_InIfStatement()
    {
        var result = Eval(@"{
            var x = null;
            if (true) {
                x ??= 100;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(100L));
    }
}
