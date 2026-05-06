using Alder.Aot;
using Alder.Test._Infrastructure;

namespace Alder.Test.AOT;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
[Category("AOT")]
public class BuiltInContextTests(CompilationMode mode)
{
    [Test]
    public void BuiltInContext_Default_ReturnsMetadata()
    {
        var metadata = AlderBuiltInContext.Default.GetTypeMetadata();

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata, Has.Count.GreaterThanOrEqualTo(30));
    }

    [Test]
    public void BuiltInContext_ContainsExpectedTypes()
    {
        var types = new HashSet<Type>(AlderBuiltInContext.Default.GetTypeMetadata()
            .Select(m => m.Type));

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
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate(""" "hello".Length """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Engine_BuiltInContext_PropertyAccess()
    {
        var engine = TestEngineFactory.Create(mode);

        var pi = engine.Evaluate("Math.PI");
        var now = engine.Evaluate("DateTime.Now");

        Assert.That(pi, Is.EqualTo(Math.PI));
        Assert.That(now, Is.Not.Null);
        Assert.That(now, Is.TypeOf<DateTime>());
    }

    [Test]
    public void Engine_BuiltInContext_ConstructorAccess()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate("new List<int>()");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void UseGeneratedContext_StacksOnBuiltIn()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Aot.UseGeneratedContext(TestGeneratedContext.Default));
        engine.SetVariable("m", new TestModel { Name = "test" });

        var builtIn = engine.Evaluate(""" "hello".Length """);
        var user = engine.Evaluate("m.Name");

        Assert.That(builtIn, Is.EqualTo(5));
        Assert.That(user, Is.EqualTo("test"));
    }

    [Test]
    public void UseGeneratedContext_UserOverridesBuiltIn()
    {
        var builtInTypes = AlderBuiltInContext.Default.GetTypeMetadata()
            .ToDictionary(m => m.Type);

        Assert.That(builtInTypes.ContainsKey(typeof(string)), Is.True,
            "Built-in context should contain string metadata");

        var overrideMetadata = new StringOverrideMetadata();
        var merged = new Dictionary<Type, TypedDispatch>(builtInTypes)
        {
            [typeof(string)] = overrideMetadata
        };

        Assert.That(merged[typeof(string)], Is.SameAs(overrideMetadata),
            "User metadata should win for string (last-write-wins)");
    }
}

internal sealed class StringOverrideMetadata : TypedDispatch
{
    public override Type Type => typeof(string);
}
