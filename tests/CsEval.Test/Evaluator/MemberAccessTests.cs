using System.Dynamic;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class MemberAccessTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Variable_FromContext()
    {
        var context = new EvalContext();
        context.Define("x", 10L);

        Assert.That(Eval("x", context), Is.EqualTo(10));
        Assert.That(Eval("x + 5", context), Is.EqualTo(15));
    }

    [Test]
    public void Eval_MemberAccess_OnExpandoObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        obj["Age"] = 30;
        context.Define("user", obj);

        Assert.That(Eval("user.Name", context), Is.EqualTo("John"));
        Assert.That(Eval("user.Age", context), Is.EqualTo(30));
    }

    [Test]
    public void Eval_ChainedMemberAccess()
    {
        var context = new EvalContext();
        IDictionary<string, object?> user = new ExpandoObject();
        IDictionary<string, object?> address = new ExpandoObject();
        address["City"] = "New York";
        user["Address"] = address;
        context.Define("user", user);

        Assert.That(Eval("user.Address.City", context), Is.EqualTo("New York"));
    }

    [Test]
    public void Eval_CaseSensitive_MemberAccess()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        context.Define("user", obj);

        Assert.That(Eval("user.Name", context), Is.EqualTo("John"));
        Assert.Throws<EvalException>(() => Eval("user.name", context));
    }

    [Test]
    public void Eval_IndexAccess_OnList()
    {
        var context = new EvalContext();
        context.Define("arr", new List<int> { 1, 2, 3 });

        Assert.That(Eval("arr[0]", context), Is.EqualTo(1));
        Assert.That(Eval("arr[2]", context), Is.EqualTo(3));
    }

    [Test]
    public void Eval_IndexAccess_OnDictionary()
    {
        var context = new EvalContext();
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["key"] = "value";
        context.Define("dict", dict);

        Assert.That(Eval("dict[\"key\"]", context), Is.EqualTo("value"));
    }
}
