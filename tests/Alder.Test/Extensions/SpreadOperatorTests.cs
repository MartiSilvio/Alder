using System.Dynamic;
using Alder.Test._Infrastructure;

namespace Alder.Test.Extensions;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class SpreadOperatorTests(CompilationMode mode)
{
    // Array Spread
    [Test]
    public void Eval_ArraySpread_SingleArray()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("[..arr]");
        Assert.That(result, Is.TypeOf<int[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(1));
        Assert.That(list[1], Is.EqualTo(2));
        Assert.That(list[2], Is.EqualTo(3));
    }

    [Test]
    public void Eval_ArraySpread_WithOtherElements()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("arr", new List<int> { 2, 3 });

        var result = engine.Evaluate("[1, ..arr, 4]");
        Assert.That(result, Is.TypeOf<int[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(4));
        Assert.That(list[0], Is.EqualTo(1));
        Assert.That(list[1], Is.EqualTo(2));
        Assert.That(list[2], Is.EqualTo(3));
        Assert.That(list[3], Is.EqualTo(4));
    }

    [Test]
    public void Eval_ArraySpread_MultipleArrays()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("arr1", new List<int> { 1, 2 });
        engine.SetVariable("arr2", new List<int> { 3, 4 });

        var result = engine.Evaluate("[..arr1, ..arr2]");
        Assert.That(result, Is.TypeOf<int[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(4));
        Assert.That(list[0], Is.EqualTo(1));
        Assert.That(list[1], Is.EqualTo(2));
        Assert.That(list[2], Is.EqualTo(3));
        Assert.That(list[3], Is.EqualTo(4));
    }

    [Test]
    public void Eval_ArraySpread_WithNativeArray()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("arr", new[] { "a", "b", "c" });

        var result = engine.Evaluate("[..arr]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo("a"));
    }

    // Object Spread

    [Test]
    public void Eval_ObjectSpread_SingleObject()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        engine.SetVariable("obj", obj);

        Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj }"));
    }

    [Test]
    public void Eval_ObjectSpread_WithOtherProperties()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        engine.SetVariable("obj", obj);

        Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj, B = 2 }"));
    }

    [Test]
    public void Eval_ObjectSpread_OverridesEarlierProperties()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        engine.SetVariable("obj", obj);

        Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj, B = 99 }"));
    }

    [Test]
    public void Eval_ObjectSpread_MultipleObjects()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 2L;
        engine.SetVariable("obj1", obj1);
        engine.SetVariable("obj2", obj2);

        Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj1, ..obj2 }"));
    }

    [Test]
    public void Eval_ObjectSpread_FromTypedObject()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("person", new TestPerson { Name = "John", Age = 30 });

        Assert.Throws<AlderException>(() => engine.Evaluate("""new { ..person, City = "NYC" } """));
    }

    [Test]
    public void Eval_ObjectSpread_LaterSpreadOverridesEarlier()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        obj1["B"] = 2L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 99L;
        obj2["C"] = 3L;
        engine.SetVariable("obj1", obj1);
        engine.SetVariable("obj2", obj2);

        Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj1, ..obj2 }"));
    }
}
