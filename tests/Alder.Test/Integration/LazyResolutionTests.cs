using System.Reflection;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LazyResolutionTests(CompilationMode mode)
{
    [SetUp]
    public void SetUp()
    {
        TrackingModule.InstanceCount = 0;
        TrackingModule.MethodCallCount = 0;
    }

    [Test]
    public void RegisterModule_DoesNotInstantiateType()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Tracking", typeof(TrackingModule)));

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(0));
    }

    [Test]
    public void RegisterModule_InstantiatesOnlyWhenAccessed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Tracking", typeof(TrackingModule)));

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(0));

        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterModule_UsesServiceProviderWhenAvailable()
    {
        var providedInstance = new TrackingModule();
        var sp = new SimpleServiceProvider();
        sp.Register(providedInstance);

        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.ServiceProvider = sp;
            o.Modules.Register("Tracking", typeof(TrackingModule));
        });

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));

        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterModule_ResolvesOnEachCall()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Tracking", typeof(TrackingModule)));

        engine.Evaluate("Tracking.DoSomething()");
        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(2));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(2));
    }

    [Test]
    public void RegisterModule_WithInstance_DoesNotCreateNew()
    {
        var instance = new TrackingModule();
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register<TrackingModule>("Tracking", instance: instance));

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));

        engine.Evaluate("Tracking.DoSomething()");
        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(2));
    }
}

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class MemberFilteringTests(CompilationMode mode)
{
    [Test]
    public void RegisterModule_WithExplicitMembers_OnlyExposesSpecifiedMethods()
    {
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sum"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!
        };
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Calc", typeof(CalculatorModule), members));

        Assert.That(engine.Evaluate("Calc.Sum(2, 3)"), Is.EqualTo(5));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Calc.Subtract(5, 2)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void RegisterModule_WithExplicitMembers_SupportsAliases()
    {
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["plus"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!,
            ["minus"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Subtract))!
        };
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Calc", typeof(CalculatorModule), members));

        Assert.That(engine.Evaluate("Calc.plus(2, 3)"), Is.EqualTo(5));
        Assert.That(engine.Evaluate("Calc.minus(5, 2)"), Is.EqualTo(3));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Calc.Sum(2, 3)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void RegisterModule_WithoutExplicitMembers_ExposesAllPublicMembers()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Calc", typeof(CalculatorModule)));

        Assert.That(engine.Evaluate("Calc.Sum(2, 3)"), Is.EqualTo(5));
        Assert.That(engine.Evaluate("Calc.Subtract(5, 2)"), Is.EqualTo(3));
        Assert.That(engine.Evaluate("Calc.Multiply(4, 3)"), Is.EqualTo(12));
    }

    [Test]
    public void RegisterModule_WithExplicitMembers_CanExposeProperties()
    {
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pi"] = typeof(ConstantsModule).GetProperty(nameof(ConstantsModule.Pi))!
        };
        var engine = TestEngineFactory.Create(mode, o => o.Modules.Register("Constants", typeof(ConstantsModule), members));

        Assert.That(engine.Evaluate("Constants.Pi"), Is.EqualTo(3.14159));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Constants.E"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0117));
    }

    [Test]
    public void RegisterModule_CaseInsensitive_WorksWithExplicitMembers()
    {
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["sum"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!
        };
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.IsCaseSensitive = false;
            o.Modules.Register("calc", typeof(CalculatorModule), members);
        });

        Assert.That(engine.Evaluate("CALC.SUM(2, 3)"), Is.EqualTo(5));
        Assert.That(engine.Evaluate("Calc.sum(2, 3)"), Is.EqualTo(5));
    }
}

#region Test Fixtures

public class TrackingModule
{
    public static int InstanceCount;
    public static int MethodCallCount;

    public TrackingModule()
    {
        Interlocked.Increment(ref InstanceCount);
    }

    public void DoSomething()
    {
        Interlocked.Increment(ref MethodCallCount);
    }
}

public class CalculatorModule
{
    public long Sum(long a, long b) => a + b;
    public long Subtract(long a, long b) => a - b;
    public long Multiply(long a, long b) => a * b;
}

public class ConstantsModule
{
    public double Pi => 3.14159;
    public double E => 2.71828;
}

#endregion
