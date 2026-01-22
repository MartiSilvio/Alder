using System.Dynamic;
using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class SpreadOperatorTests : EvaluatorTestBase
{
    #region Array Spread

    [Test]
    public void Eval_ArraySpread_SingleArray()
    {
        var context = new EvalContext();
        context.Define("arr", new List<object?> { 1L, 2L, 3L });

        var result = Eval("[...arr]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_ArraySpread_WithOtherElements()
    {
        var context = new EvalContext();
        context.Define("arr", new List<object?> { 2L, 3L });

        var result = Eval("[1, ...arr, 4]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
        Assert.That(result[3], Is.EqualTo(4L));
    }

    [Test]
    public void Eval_ArraySpread_MultipleArrays()
    {
        var context = new EvalContext();
        context.Define("arr1", new List<object?> { 1L, 2L });
        context.Define("arr2", new List<object?> { 3L, 4L });

        var result = Eval("[...arr1, ...arr2]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
        Assert.That(result[3], Is.EqualTo(4L));
    }

    [Test]
    public void Eval_ArraySpread_WithNativeArray()
    {
        var context = new EvalContext();
        context.Define("arr", new[] { "a", "b", "c" });

        var result = Eval("[...arr]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo("a"));
    }

    #endregion

    #region Object Spread

    [Test]
    public void Eval_ObjectSpread_SingleObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_WithOtherProperties()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj, B = 2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_OverridesEarlierProperties()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj, B = 99 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(99L));
    }

    [Test]
    public void Eval_ObjectSpread_MultipleObjects()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 2L;
        context.Define("obj1", obj1);
        context.Define("obj2", obj2);

        var result = Eval("new { ...obj1, ...obj2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_FromTypedObject()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("new { ...person, City = \"NYC\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_ObjectSpread_LaterSpreadOverridesEarlier()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        obj1["B"] = 2L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 99L;
        obj2["C"] = 3L;
        context.Define("obj1", obj1);
        context.Define("obj2", obj2);

        var result = Eval("new { ...obj1, ...obj2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(99L));
        Assert.That(result["C"], Is.EqualTo(3L));
    }

    #endregion
}
