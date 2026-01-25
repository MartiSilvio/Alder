using System.Dynamic;

namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class MemberAccessTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_Variable_FromContext()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10L);

        Assert.That(engine.Evaluate("x"), Is.EqualTo(10));
        Assert.That(engine.Evaluate("x + 5"), Is.EqualTo(15));
    }

    [Test]
    public void Eval_MemberAccess_OnExpandoObject()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        obj["Age"] = 30;
        engine.SetVariable("user", obj);

        Assert.That(engine.Evaluate("user.Name"), Is.EqualTo("John"));
        Assert.That(engine.Evaluate("user.Age"), Is.EqualTo(30));
    }

    [Test]
    public void Eval_ChainedMemberAccess()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> user = new ExpandoObject();
        IDictionary<string, object?> address = new ExpandoObject();
        address["City"] = "New York";
        user["Address"] = address;
        engine.SetVariable("user", user);

        Assert.That(engine.Evaluate("user.Address.City"), Is.EqualTo("New York"));
    }

    [Test]
    public void Eval_CaseSensitive_MemberAccess()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        engine.SetVariable("user", obj);

        Assert.That(engine.Evaluate("user.Name"), Is.EqualTo("John"));
        Assert.Throws<CsEvalException>(() => engine.Evaluate("user.name"));
    }

    [Test]
    public void Eval_IndexAccess_OnList()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });

        Assert.That(engine.Evaluate("arr[0]"), Is.EqualTo(1));
        Assert.That(engine.Evaluate("arr[2]"), Is.EqualTo(3));
    }

    [Test]
    public void Eval_IndexAccess_OnDictionary()
    {
        var engine = CreateEngine(mode);
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["key"] = "value";
        engine.SetVariable("dict", dict);

        Assert.That(engine.Evaluate("dict[\"key\"]"), Is.EqualTo("value"));
    }
}
