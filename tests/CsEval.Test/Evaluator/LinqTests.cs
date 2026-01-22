using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class LinqTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Lambda_Where()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where((x) => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Eval_Lambda_Where_WithoutParens()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where(x => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Eval_Lambda_Select()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L });

        var result = Eval("numbers.Select((x) => x * 2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2L, 4L, 6L }));
    }

    [Test]
    public void Eval_Lambda_Select_WithoutParens()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L });

        var result = Eval("numbers.Select(x => x * 2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2L, 4L, 6L }));
    }

    [Test]
    public void Eval_Lambda_Select_WithMemberAccess()
    {
        var context = new EvalContext();
        context.Define("items", new List<object?> {
            new { Name = "Alice" },
            new { Name = "Bob" }
        });

        var result = Eval("items.Select(x => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Alice", "Bob" }));
    }

    [Test]
    public void Eval_Lambda_Aggregate()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L, 4L });

        var result = Eval("numbers.Aggregate((acc, x) => acc + x, 0)", context);
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_ComplexExpression_WithLinq()
    {
        var context = new EvalContext();
        context.Define("items", new List<object?>
        {
            CreateItem("Apple", 1.5),
            CreateItem("Banana", 0.75),
            CreateItem("Orange", 2.0)
        });

        var result = Eval("items.Where((x) => x.Price > 1).Select((x) => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Apple", "Orange" }));
    }

    [Test]
    public void Eval_MathProxy_Abs()
    {
        var result = Eval("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void Eval_MathProxy_Sqrt()
    {
        var result = Eval("Math.Sqrt(16)");
        Assert.That(result, Is.EqualTo(4.0));
    }
}
