namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class NullHandlingTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_NullCoalesce()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", null);
        engine.SetVariable("y", "default");

        Assert.That(engine.Evaluate("x ?? y"), Is.EqualTo("default"));
        Assert.That(engine.Evaluate("y ?? \"other\""), Is.EqualTo("default"));
    }

    [Test]
    public void Eval_NullSafeAccess()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("obj", null);

        Assert.That(engine.Evaluate("obj?.Name"), Is.Null);
    }

    [Test]
    public void Eval_NullCoalesceAssign_AssignsWhenNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = null;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_NullCoalesceAssign_KeepsValueWhenNotNull()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = 10;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsAssignedValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = null;
            return x ??= 42;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsExistingValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = ""hello"";
            return x ??= ""world"";
        }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_NullCoalesceAssign_WithExpression()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = null;
            x ??= 5 + 5;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_NullCoalesceAssign_InIfStatement()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"{
            var x = null;
            if (true) {
                x ??= 100;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(100));
    }
}
