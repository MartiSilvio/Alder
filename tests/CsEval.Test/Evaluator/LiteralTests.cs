using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class LiteralTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Number_ReturnsNumber()
    {
        var result = Eval("42");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_String_ReturnsString()
    {
        var result = Eval("\"hello\"");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_Boolean_ReturnsBoolean()
    {
        Assert.That(Eval("true"), Is.EqualTo(true));
        Assert.That(Eval("false"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_Null_ReturnsNull()
    {
        Assert.That(Eval("null"), Is.Null);
    }
}
