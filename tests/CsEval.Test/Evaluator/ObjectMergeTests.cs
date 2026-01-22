using System.Dynamic;
using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class ObjectMergeTests : EvaluatorTestBase
{
    #region Dictionary Merge

    [Test]
    public void Eval_DictionaryMerge_WithPlusOperator()
    {
        var result = Eval("new { A = 1 } + new { B = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_DictionaryMerge_RightOverwritesLeft()
    {
        var result = Eval("new { A = 1, B = 2 } + new { B = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_DictionaryMerge_WithVariables()
    {
        var context = new EvalContext();
        IDictionary<string, object?> left = new ExpandoObject();
        left["Name"] = "John";
        IDictionary<string, object?> right = new ExpandoObject();
        right["Age"] = 30;
        context.Define("left", left);
        context.Define("right", right);

        var result = Eval("left + right", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_DictionaryMerge_ChainedOperations()
    {
        var result = Eval("new { A = 1 } + new { B = 2 } + new { C = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
        Assert.That(result["C"], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_DictionaryMerge_CaseSensitive_KeepsBothKeys()
    {
        var result = Eval("new { a = 1 } + new { A = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result["a"], Is.EqualTo(1L));
        Assert.That(result["A"], Is.EqualTo(2L));
    }

    #endregion

    #region Typed Object Merge

    [Test]
    public void Eval_TypedObjectMerge_WithPlusOperator()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { City = \"NYC\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_TypedObjectMerge_RightOverwritesLeft()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { Age = 40 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(40L));
    }

    [Test]
    public void Eval_TypedObjectMerge_WithBlock()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval(@"{
            var p = person;
            return p + new { City = ""NYC"", Country = ""USA"" };
        }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_InSelect()
    {
        var context = new EvalContext();
        context.Define("people", new List<object?>
        {
            new TestPerson { Name = "John", Age = 30 },
            new TestPerson { Name = "Jane", Age = 25 }
        });

        var result = Eval("people.Select(p => p + new { Status = \"Active\" })", context) as List<object?>;
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
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { City = \"NYC\" } + new { Country = \"USA\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_WithNestedObject()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });
        context.Define("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = Eval("person + new { Address = address }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        var addr = result["Address"] as TestAddress;
        Assert.That(addr, Is.Not.Null);
        Assert.That(addr!.City, Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_DictionaryPlusTypedObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["Extra"] = "Value";
        context.Define("dict", dict);
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("dict + person", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Extra"], Is.EqualTo("Value"));
        Assert.That(result["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_TwoTypedObjects()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });
        context.Define("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = Eval("person + address", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    #endregion
}
