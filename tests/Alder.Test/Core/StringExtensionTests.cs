namespace Alder.Test.Core;

[TestFixture]
public class StringExtensionTests
{
    [TearDown]
    public void TearDown()
    {
        AlderEval.Reset();
    }

    [Test]
    public void Eval_SimpleArithmetic()
    {
        Assert.That("1 + 2".Evaluate(), Is.EqualTo(3));
    }

    [Test]
    public void Eval_Generic_ReturnsTypedResult()
    {
        Assert.That("10 * 5".Evaluate<int>(), Is.EqualTo(50));
    }

    [Test]
    public void Eval_Generic_String()
    {
        Assert.That("""  "hello".ToUpper()  """.Evaluate<string>(), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Eval_Generic_Bool()
    {
        Assert.That("1 == 1".Evaluate<bool>(), Is.True);
    }

    [Test]
    public void Eval_Generic_Double()
    {
        Assert.That("1.5 + 2.5".Evaluate<double>(), Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_WithDictionaryVariables()
    {
        var vars = new Dictionary<string, object?> { ["x"] = 7, ["y"] = 3 };
        Assert.That("x + y".Evaluate<int>(vars), Is.EqualTo(10));
    }

    [Test]
    public void Eval_NonGeneric_WithDictionaryVariables()
    {
        var vars = new Dictionary<string, object?> { ["a"] = 100 };
        Assert.That("a + 1".Evaluate(vars), Is.EqualTo(101));
    }

    [Test]
    public void Eval_WithAnonymousObject()
    {
        Assert.That("x * y".Evaluate<int>(new { x = 4, y = 5 }), Is.EqualTo(20));
    }

    [Test]
    public void Eval_NonGeneric_WithAnonymousObject()
    {
        Assert.That("a - b".Evaluate(new { a = 10, b = 3 }), Is.EqualTo(7));
    }

    [Test]
    public void Eval_UsesGlobalConfig()
    {
        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended);
        Assert.That("2 ** 10".Evaluate<int>(), Is.EqualTo(1024));
    }

    [Test]
    public void Eval_InvalidExpression_Throws()
    {
        Assert.Throws<AlderException>(() => "!!!".Evaluate());
    }

    [Test]
    public void Eval_UndefinedVariable_Throws()
    {
        Assert.Throws<AlderException>(() => "nonexistent + 1".Evaluate());
    }
}
