using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class StringTests : EvaluatorTestBase
{
    [Test]
    public void Eval_StringConcatenation()
    {
        Assert.That(Eval("\"Hello\" + \" \" + \"World\""), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var context = new EvalContext();
        context.Define("name", "World");

        Assert.That(Eval("$\"Hello {name}!\"", context), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_StringMethod_ToLower()
    {
        var context = new EvalContext();
        context.Define("s", "HELLO");

        var result = Eval("s.ToLower()", context);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_StringMethod_ToUpper()
    {
        var context = new EvalContext();
        context.Define("s", "hello");

        var result = Eval("s.ToUpper()", context);
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Eval_StringProperty_Length()
    {
        var context = new EvalContext();
        context.Define("s", "hello");

        var result = Eval("s.Length", context);
        Assert.That(result, Is.EqualTo(5));
    }
}
