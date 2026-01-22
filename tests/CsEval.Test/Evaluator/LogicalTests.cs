using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class LogicalTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Logical_And()
    {
        Assert.That(Eval("true && true"), Is.True);
        Assert.That(Eval("true && false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Or()
    {
        Assert.That(Eval("true || false"), Is.True);
        Assert.That(Eval("false || false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Not()
    {
        Assert.That(Eval("!true"), Is.False);
        Assert.That(Eval("!false"), Is.True);
    }
}
