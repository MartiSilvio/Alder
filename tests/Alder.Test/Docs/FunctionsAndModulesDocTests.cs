using Alder.Attributes;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class FunctionsAndModulesDocTests(CompilationMode mode)
{
    [Test]
    public void Functions_Register()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Functions.Register("clamp", args =>
            {
                var value = Convert.ToDouble(args![0]);
                var min = Convert.ToDouble(args[1]);
                var max = Convert.ToDouble(args[2]);
                return Math.Min(Math.Max(value, min), max);
            });
        });

        Assert.That(engine.Evaluate<double>("clamp(150, 0, 100)"), Is.EqualTo(100.0));
        Assert.That(engine.Evaluate<double>("clamp(-5, 0, 100)"), Is.EqualTo(0.0));
        Assert.That(engine.Evaluate<double>("clamp(50, 0, 100)"), Is.EqualTo(50.0));
    }

    public class MathUtils
    {
        public double CircleArea(double radius) => Math.PI * radius * radius;
        public double Hypotenuse(double a, double b) => Math.Sqrt(a * a + b * b);
        public double Pi => Math.PI;
    }

    [Test]
    public void Modules_Register()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.Register<MathUtils>("utils");
        });

        Assert.That(engine.Evaluate<double>("utils.CircleArea(5)"), Is.EqualTo(Math.PI * 25).Within(0.001));
        Assert.That(engine.Evaluate<double>("utils.Pi"), Is.EqualTo(Math.PI));
    }

    [AlderModule("secure", ExplicitOnly = true)]
    public class SecureModule
    {
        [AlderFunction]
        public string GetValue() => "exposed";

        public string InternalMethod() => "hidden";
    }

    [Test]
    public void Modules_ExplicitOnly()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.RegisterFromType<SecureModule>();
        });

        Assert.That(engine.Evaluate<string>("secure.GetValue()"), Is.EqualTo("exposed"));
        Assert.Throws<AlderException>(() => engine.Evaluate("secure.InternalMethod()"));
    }

    public class GlobalHelpers
    {
        [AlderFunction("greet")]
        public string Greet(string name) => $"Hello, {name}!";
    }

    [Test]
    public void Functions_GlobalFromType()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Modules.RegisterFromType<GlobalHelpers>();
        });

        Assert.That(engine.Evaluate<string>("""greet("Alice")"""), Is.EqualTo("Hello, Alice!"));
    }
}
