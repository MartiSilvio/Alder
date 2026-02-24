namespace CsEval.Test.Security;

[TestFixture]
public class FreezeGuardTests
{
    private CsEvalEngine CreateFrozenEngine()
    {
        var engine = new CsEvalEngine();
        engine.Evaluate("1 + 1"); // Trigger freeze
        return engine;
    }

    [Test]
    public void RegisterFunction_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterFunction("test", args => args[0]));
        Assert.That(ex!.Message, Does.Contain("RegisterFunction"));
        Assert.That(ex.Message, Does.Contain("before the first Evaluate"));
    }

    [Test]
    public void RegisterFromAssembly_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterFromAssembly(typeof(object).Assembly));
        Assert.That(ex!.Message, Does.Contain("RegisterFromAssembly"));
    }

    [Test]
    public void RegisterFromType_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterFromType(typeof(string)));
        Assert.That(ex!.Message, Does.Contain("RegisterFromType"));
    }

    [Test]
    public void RegisterModule_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterModule("Test", typeof(string)));
        Assert.That(ex!.Message, Does.Contain("RegisterModule"));
    }

    [Test]
    public void RegisterExtensionMethods_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterExtensionMethods(typeof(Enumerable)));
        Assert.That(ex!.Message, Does.Contain("RegisterExtensionMethods"));
    }

    [Test]
    public void AddAssembly_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.AddAssembly(typeof(object).Assembly));
        Assert.That(ex!.Message, Does.Contain("AddAssembly"));
    }

    [Test]
    public void AddUsing_AfterEvaluate_Throws()
    {
        var engine = CreateFrozenEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            engine.AddUsing("System.IO"));
        Assert.That(ex!.Message, Does.Contain("AddUsing"));
    }

    [Test]
    public void SetVariable_AfterEvaluate_Succeeds()
    {
        // SetVariable is intentionally allowed after freeze
        var engine = CreateFrozenEngine();

        Assert.DoesNotThrow(() => engine.SetVariable("x", 42));
        var result = engine.Evaluate("x");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SetVariables_AfterEvaluate_Succeeds()
    {
        // SetVariables is intentionally allowed after freeze
        var engine = CreateFrozenEngine();

        Assert.DoesNotThrow(() => engine.SetVariables(
            new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 }));
        var result = engine.Evaluate("a + b");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void RegisterFunction_BeforeEvaluate_Succeeds()
    {
        var engine = new CsEvalEngine();

        Assert.DoesNotThrow(() => engine.RegisterFunction("add",
            args => (int)args[0]! + (int)args[1]!));

        var result = engine.Evaluate("add(3, 4)");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void RegisterModule_BeforeEvaluate_Succeeds()
    {
        var engine = new CsEvalEngine();

        Assert.DoesNotThrow(() => engine.RegisterModule("Test", typeof(TestModule)));

        var result = engine.Evaluate("Test.Double(5)");
        Assert.That(result, Is.EqualTo(10));
    }

    public class TestModule
    {
        public static int Double(int x) => x * 2;
    }
}
