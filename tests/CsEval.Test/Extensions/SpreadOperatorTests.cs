using System.Dynamic;
using NUnit.Framework;

namespace CsEval.Test.Extensions;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SpreadOperatorTests(CompilationMode mode) 
{
    #region Array Spread

    [Test]
    public void Eval_ArraySpread_SingleArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("[...arr]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1));
        Assert.That(result[1], Is.EqualTo(2));
        Assert.That(result[2], Is.EqualTo(3));
    }

    [Test]
    public void Eval_ArraySpread_WithOtherElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new List<int> { 2, 3 });

        var result = engine.Evaluate("[1, ...arr, 4]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1));
        Assert.That(result[1], Is.EqualTo(2));
        Assert.That(result[2], Is.EqualTo(3));
        Assert.That(result[3], Is.EqualTo(4));
    }

    [Test]
    public void Eval_ArraySpread_MultipleArrays()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr1", new List<int> { 1, 2 });
        engine.SetVariable("arr2", new List<int> { 3, 4 });

        var result = engine.Evaluate("[...arr1, ...arr2]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1));
        Assert.That(result[1], Is.EqualTo(2));
        Assert.That(result[2], Is.EqualTo(3));
        Assert.That(result[3], Is.EqualTo(4));
    }

    [Test]
    public void Eval_ArraySpread_WithNativeArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new[] { "a", "b", "c" });

        var result = engine.Evaluate("[...arr]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo("a"));
    }

    #endregion

    #region Object Spread

    [Test]
    public void Eval_ObjectSpread_SingleObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        engine.SetVariable("obj", obj);

        var result = engine.Evaluate("new { ...obj }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
    }

    [Test]
    public void Eval_ObjectSpread_WithOtherProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        engine.SetVariable("obj", obj);

        var result = engine.Evaluate("new { ...obj, B = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
    }

    [Test]
    public void Eval_ObjectSpread_OverridesEarlierProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        engine.SetVariable("obj", obj);

        var result = engine.Evaluate("new { ...obj, B = 99 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(99));
    }

    [Test]
    public void Eval_ObjectSpread_MultipleObjects()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 2L;
        engine.SetVariable("obj1", obj1);
        engine.SetVariable("obj2", obj2);

        var result = engine.Evaluate("new { ...obj1, ...obj2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
    }

    [Test]
    public void Eval_ObjectSpread_FromTypedObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate("new { ...person, City = \"NYC\" }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_ObjectSpread_LaterSpreadOverridesEarlier()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        obj1["B"] = 2L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 99L;
        obj2["C"] = 3L;
        engine.SetVariable("obj1", obj1);
        engine.SetVariable("obj2", obj2);

        var result = engine.Evaluate("new { ...obj1, ...obj2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(99));
        Assert.That(result["C"], Is.EqualTo(3));
    }

    #endregion
}
