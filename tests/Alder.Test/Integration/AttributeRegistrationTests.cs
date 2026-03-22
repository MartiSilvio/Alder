using Alder.Attributes;
using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class AttributeRegistrationTests(CompilationMode mode)
{
    [Test]
    public void GlobalFunction()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromType<GlobalFunctions>());

        var result = engine.Evaluate("triple(4)");
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void GlobalFunction_WithInstance()
    {
        var instance = new StatefulFunctions { Multiplier = 5 };
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromType(instance));

        var result = engine.Evaluate("multiply(3)");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Module()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromType<CustomMathModule>());

        Assert.That(engine.Evaluate("CustomMath.Square(4)"), Is.EqualTo(16));
        Assert.That(engine.Evaluate("CustomMath.Cube(3)"), Is.EqualTo(27));
    }

    [Test]
    public void Module_WithInstance()
    {
        var instance = new GreeterModule("Hi");
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromType(instance));

        var result = engine.Evaluate("""Greeter.SayHello("World") """);
        Assert.That(result, Is.EqualTo("Hi, World!"));
    }

    [Test]
    public void StaticMethods()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromType<StaticHelpers>());

        Assert.That(engine.Evaluate("isEven(4)"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("isEven(5)"), Is.EqualTo(false));
    }

    [Test]
    public void FromAssembly()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.RegisterFromAssembly(typeof(AssemblyTestModule).Assembly));

        Assert.That(engine.Evaluate("AssemblyTest.Double(5)"), Is.EqualTo(10));
    }

    [Test]
    public void WithServiceProvider()
    {
        var sp = new SimpleServiceProvider();
        sp.Register(new GreeterModule("Hola"));

        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.ServiceProvider = sp;
            o.Modules.RegisterFromType<GreeterModule>();
        });

        var result = engine.Evaluate("""Greeter.SayHello("World") """);
        Assert.That(result, Is.EqualTo("Hola, World!"));
    }
}

#region Test Fixtures

public class GlobalFunctions
{
    [AlderFunction("triple")]
    public long Triple(long value) => value * 3;
}

public class StatefulFunctions
{
    public long Multiplier { get; set; } = 2;

    [AlderFunction("multiply")]
    public long Multiply(long value) => value * Multiplier;
}

public class StaticHelpers
{
    [AlderFunction("isEven")]
    public static bool IsEven(long value) => value % 2 == 0;
}

[AlderModule("CustomMath")]
public class CustomMathModule
{
    public long Square(long value) => value * value;
    public long Cube(long value) => value * value * value;
}

[AlderModule("Greeter")]
public class GreeterModule(string greeting)
{
    public string SayHello(string name) => $"{greeting}, {name}!";
}

[AlderModule("AssemblyTest")]
public class AssemblyTestModule
{
    public long Double(long value) => value * 2;
}

public class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

    public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
}

#endregion
