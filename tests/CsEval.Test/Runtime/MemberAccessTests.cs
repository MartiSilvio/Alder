using System.Dynamic;

namespace CsEval.Test.Runtime;

/// <summary>
/// All tests engine-only: ExpandoObject dynamic member access, SetVariable with
/// non-serializable types (ExpandoObject, List{int}), CsEvalException error assertion.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class MemberAccessTests(CompilationMode mode)
{
    #region Engine-only: SetVariable with long type

    // Engine-only: SetVariable with long type for variable context access
    [Test]
    public void Eval_Variable_FromContext()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10L);

        Assert.That(engine.Evaluate("x"), Is.EqualTo(10));
        Assert.That(engine.Evaluate("x + 5"), Is.EqualTo(15));
    }

    #endregion

    #region Engine-only: ExpandoObject dynamic member access (non-serializable)

    // Engine-only: SetVariable with ExpandoObject for dynamic member access
    [Test]
    public void Eval_MemberAccess_OnExpandoObject()
    {
        var engine = TestEngineFactory.Create(mode);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        obj["Age"] = 30;
        engine.SetVariable("user", obj);

        Assert.That(engine.Evaluate("user.Name"), Is.EqualTo("John"));
        Assert.That(engine.Evaluate("user.Age"), Is.EqualTo(30));
    }

    // Engine-only: SetVariable with nested ExpandoObject for chained access
    [Test]
    public void Eval_ChainedMemberAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        IDictionary<string, object?> user = new ExpandoObject();
        IDictionary<string, object?> address = new ExpandoObject();
        address["City"] = "New York";
        user["Address"] = address;
        engine.SetVariable("user", user);

        Assert.That(engine.Evaluate("user.Address.City"), Is.EqualTo("New York"));
    }

    // Engine-only: SetVariable with ExpandoObject + CsEvalException for case mismatch
    [Test]
    public void Eval_CaseSensitive_MemberAccess()
    {
        var engine = TestEngineFactory.Create(mode);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        engine.SetVariable("user", obj);

        Assert.That(engine.Evaluate("user.Name"), Is.EqualTo("John"));
        Assert.Throws<CsEvalException>(() => engine.Evaluate("user.name"));
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable collection types

    // Engine-only: SetVariable with List<int> for index access
    [Test]
    public void Eval_IndexAccess_OnList()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });

        Assert.That(engine.Evaluate("arr[0]"), Is.EqualTo(1));
        Assert.That(engine.Evaluate("arr[2]"), Is.EqualTo(3));
    }

    // Engine-only: SetVariable with ExpandoObject for dictionary index access
    [Test]
    public void Eval_IndexAccess_OnDictionary()
    {
        var engine = TestEngineFactory.Create(mode);
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["key"] = "value";
        engine.SetVariable("dict", dict);

        Assert.That(engine.Evaluate("dict[\"key\"]"), Is.EqualTo("value"));
    }

    #endregion
}
