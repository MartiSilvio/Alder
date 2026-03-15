namespace CsEval.Test.Aot;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Category("AOT")]
public class BuiltInContextTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode });

    [Test]
    public void BuiltInContext_Default_ReturnsMetadata()
    {
        var metadata = CsEvalBuiltInContext.Default.GetTypeMetadata();

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata, Has.Count.GreaterThanOrEqualTo(30));
    }

    [Test]
    public void BuiltInContext_ContainsExpectedTypes()
    {
        var types = CsEvalBuiltInContext.Default.GetTypeMetadata()
            .Select(m => m.Type)
            .ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(types, Does.Contain(typeof(string)));
            Assert.That(types, Does.Contain(typeof(int)));
            Assert.That(types, Does.Contain(typeof(double)));
            Assert.That(types, Does.Contain(typeof(Math)));
            Assert.That(types, Does.Contain(typeof(DateTime)));
            Assert.That(types, Does.Contain(typeof(List<int>)));
            Assert.That(types, Does.Contain(typeof(Dictionary<string, object>)));
        });
    }

    [Test]
    public void Engine_AutoLoads_BuiltInContext()
    {
        var engine = CreateEngine();

        var result = engine.Evaluate("\"hello\".Length");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Engine_BuiltInContext_PropertyAccess()
    {
        var engine = CreateEngine();

        var pi = engine.Evaluate("Math.PI");
        var now = engine.Evaluate("DateTime.Now");

        Assert.That(pi, Is.EqualTo(Math.PI));
        Assert.That(now, Is.Not.Null);
        Assert.That(now, Is.TypeOf<DateTime>());
    }

    [Test]
    public void Engine_BuiltInContext_ConstructorAccess()
    {
        var engine = CreateEngine();

        var result = engine.Evaluate("new List<int>()");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void UseGeneratedContext_StacksOnBuiltIn()
    {
        var engine = CreateEngine();
        engine.UseGeneratedContext(TestGeneratedContext.Default);
        engine.SetVariable("m", new TestModel { Name = "test" });

        var builtIn = engine.Evaluate("\"hello\".Length");
        var user = engine.Evaluate("m.Name");

        Assert.That(builtIn, Is.EqualTo(5));
        Assert.That(user, Is.EqualTo("test"));
    }

    [Test]
    public void UseGeneratedContext_UserOverridesBuiltIn()
    {
        var builtInTypes = CsEvalBuiltInContext.Default.GetTypeMetadata()
            .ToDictionary(m => m.Type);

        Assert.That(builtInTypes.ContainsKey(typeof(string)), Is.True,
            "Built-in context should contain string metadata");

        var overrideMetadata = new StringOverrideMetadata();
        var merged = new Dictionary<Type, IAotTypeMetadata>(builtInTypes)
        {
            [typeof(string)] = overrideMetadata
        };

        Assert.That(merged[typeof(string)], Is.SameAs(overrideMetadata),
            "User metadata should win for string (last-write-wins)");
    }
}

internal sealed class StringOverrideMetadata : IAotTypeMetadata
{
    public Type Type => typeof(string);
    public bool TryGetProperty(string name, object instance, out object? value) { value = default; return false; }
    public bool TrySetProperty(string name, object instance, object? value) => false;
    public bool TryGetField(string name, object instance, out object? value) { value = default; return false; }
    public bool TrySetField(string name, object instance, object? value) => false;
    public bool TryGetIndex(object instance, object key, out object? value) { value = default; return false; }
    public bool TrySetIndex(object instance, object key, object? value) => false;
    public bool TryGetStaticProperty(string name, out object? value) { value = default; return false; }
    public bool TryGetStaticField(string name, out object? value) { value = default; return false; }
    public bool TryCreateInstance(object?[] args, out object? instance) { instance = default; return false; }
    public bool TryInvokeMethod(string name, object instance, object?[] args, out object? result) { result = default; return false; }
    public bool TryInvokeStaticMethod(string name, object?[] args, out object? result) { result = default; return false; }
}
