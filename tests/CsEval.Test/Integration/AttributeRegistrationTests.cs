using CsEval.Attributes;
using CsEval.Test._Infrastructure;

namespace CsEval.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class AttributeRegistrationTests(CompilationMode mode)
{
    [Test]
    public void GlobalFunction()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFromType<GlobalFunctions>();

        var result = engine.Evaluate("triple(4)");
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void GlobalFunction_WithInstance()
    {
        var engine = TestEngineFactory.Create(mode);
        var instance = new StatefulFunctions { Multiplier = 5 };
        engine.RegisterFromType(instance);

        var result = engine.Evaluate("multiply(3)");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Module()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFromType<CustomMathModule>();

        Assert.That(engine.Evaluate("CustomMath.Square(4)"), Is.EqualTo(16));
        Assert.That(engine.Evaluate("CustomMath.Cube(3)"), Is.EqualTo(27));
    }

    [Test]
    public void Module_WithInstance()
    {
        var engine = TestEngineFactory.Create(mode);
        var instance = new GreeterModule("Hi");
        engine.RegisterFromType(instance);

        var result = engine.Evaluate("""Greeter.SayHello("World") """);
        Assert.That(result, Is.EqualTo("Hi, World!"));
    }

    [Test]
    public void StaticMethods()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFromType<StaticHelpers>();

        Assert.That(engine.Evaluate("isEven(4)"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("isEven(5)"), Is.EqualTo(false));
    }

    [Test]
    public void FromAssembly()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFromAssembly(typeof(AssemblyTestModule).Assembly);

        Assert.That(engine.Evaluate("AssemblyTest.Double(5)"), Is.EqualTo(10));
    }

    [Test]
    public void WithServiceProvider()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterFromType<GreeterModule>();

        var serviceProvider = new SimpleServiceProvider();
        serviceProvider.Register(new GreeterModule("Hola"));

        var result = engine.Evaluate("""Greeter.SayHello("World") """, serviceProvider: serviceProvider);
        Assert.That(result, Is.EqualTo("Hola, World!"));
    }
}

#region Test Fixtures

public class GlobalFunctions
{
    [CsEvalFunction("triple")]
    public long Triple(long value) => value * 3;
}

public class StatefulFunctions
{
    public long Multiplier { get; set; } = 2;

    [CsEvalFunction("multiply")]
    public long Multiply(long value) => value * Multiplier;
}

public class StaticHelpers
{
    [CsEvalFunction("isEven")]
    public static bool IsEven(long value) => value % 2 == 0;
}

[CsEvalModule("CustomMath")]
public class CustomMathModule
{
    public long Square(long value) => value * value;
    public long Cube(long value) => value * value * value;
}

[CsEvalModule("Greeter")]
public class GreeterModule(string greeting)
{
    public string SayHello(string name) => $"{greeting}, {name}!";
}

[CsEvalModule("AssemblyTest")]
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