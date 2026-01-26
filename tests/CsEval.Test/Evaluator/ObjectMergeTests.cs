using System.Dynamic;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ObjectMergeTests(CompilationMode mode) : TestBase
{
    #region Dictionary Merge

    [Test]
    public void Eval_DictionaryMerge_WithPlusOperator()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { A = 1 } + new { B = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
    }

    [Test]
    public void Eval_DictionaryMerge_RightOverwritesLeft()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { A = 1, B = 2 } + new { B = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(3));
    }

    [Test]
    public void Eval_DictionaryMerge_WithVariables()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> left = new ExpandoObject();
        left["Name"] = "John";
        IDictionary<string, object?> right = new ExpandoObject();
        right["Age"] = 30;
        engine.SetVariable("left", left);
        engine.SetVariable("right", right);

        var result = engine.Evaluate("left + right") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_DictionaryMerge_ChainedOperations()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { A = 1 } + new { B = 2 } + new { C = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
        Assert.That(result["C"], Is.EqualTo(3));
    }

    [Test]
    public void Eval_DictionaryMerge_CaseSensitive_KeepsBothKeys()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("new { a = 1 } + new { A = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result["a"], Is.EqualTo(1));
        Assert.That(result["A"], Is.EqualTo(2));
    }

    #endregion

    #region Typed Object Merge

    [Test]
    public void Eval_TypedObjectMerge_WithPlusOperator()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate("person + new { City = \"NYC\" }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_TypedObjectMerge_RightOverwritesLeft()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate("person + new { Age = 40 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(40));
    }

    [Test]
    public void Eval_TypedObjectMerge_WithBlock()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate(@"{
            var p = person;
            return p + new { City = ""NYC"", Country = ""USA"" };
        }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_InSelect()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("people", new List<TestPerson>
        {
            new TestPerson { Name = "John", Age = 30 },
            new TestPerson { Name = "Jane", Age = 25 }
        });

        var result = engine.Evaluate("people.Select(p => p + new { Status = \"Active\" })") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));

        var first = result![0] as IDictionary<string, object?>;
        Assert.That(first, Is.Not.Null);
        Assert.That(first!["Name"], Is.EqualTo("John"));
        Assert.That(first["Status"], Is.EqualTo("Active"));

        var second = result[1] as IDictionary<string, object?>;
        Assert.That(second, Is.Not.Null);
        Assert.That(second!["Name"], Is.EqualTo("Jane"));
        Assert.That(second["Status"], Is.EqualTo("Active"));
    }

    [Test]
    public void Eval_TypedObjectMerge_ChainedWithDictionary()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate("person + new { City = \"NYC\" } + new { Country = \"USA\" }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_WithNestedObject()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });
        engine.SetVariable("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = engine.Evaluate("person + new { Address = address }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        var addr = result["Address"] as TestAddress;
        Assert.That(addr, Is.Not.Null);
        Assert.That(addr!.City, Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_DictionaryPlusTypedObject()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["Extra"] = "Value";
        engine.SetVariable("dict", dict);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        var result = engine.Evaluate("dict + person") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Extra"], Is.EqualTo("Value"));
        Assert.That(result["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_TwoTypedObjects()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });
        engine.SetVariable("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = engine.Evaluate("person + address") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    #endregion
}