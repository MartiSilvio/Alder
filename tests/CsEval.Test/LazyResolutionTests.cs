using System.Reflection;
using CsEval.Attributes;
using CsEval.Evaluation;
using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class LazyResolutionTests
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
        var engine = new CsEvalEngine();
        engine.RegisterModule("Tracking", typeof(TrackingModule));

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(0));
    }

    [Test]
    public void RegisterModule_InstantiatesOnlyWhenAccessed()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Tracking", typeof(TrackingModule));

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(0));

        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterModule_UsesServiceProviderWhenAvailable()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Tracking", typeof(TrackingModule));

        var providedInstance = new TrackingModule();
        var sp = new SimpleServiceProvider();
        sp.Register(providedInstance);

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));

        engine.Evaluate("Tracking.DoSomething()", sp);

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterModule_ResolvesOnEachCall()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Tracking", typeof(TrackingModule));

        engine.Evaluate("Tracking.DoSomething()");
        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(2));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(2));
    }

    [Test]
    public void RegisterModule_WithInstance_DoesNotCreateNew()
    {
        var engine = new CsEvalEngine();
        var instance = new TrackingModule();
        engine.RegisterModule("Tracking", instance);

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));

        engine.Evaluate("Tracking.DoSomething()");
        engine.Evaluate("Tracking.DoSomething()");

        Assert.That(TrackingModule.InstanceCount, Is.EqualTo(1));
        Assert.That(TrackingModule.MethodCallCount, Is.EqualTo(2));
    }
}

[TestFixture]
public class MemberFilteringTests
{
    [Test]
    public void RegisterModule_WithExplicitMembers_OnlyExposesSpecifiedMethods()
    {
        var engine = new CsEvalEngine();
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sum"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!
        };
        engine.RegisterModule("Calc", typeof(CalculatorModule), members);

        Assert.That(engine.Evaluate("Calc.Sum(2, 3)"), Is.EqualTo(5));

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("Calc.Subtract(5, 2)"));
        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public void RegisterModule_WithExplicitMembers_SupportsAliases()
    {
        var engine = new CsEvalEngine();
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["plus"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!,
            ["minus"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Subtract))!
        };
        engine.RegisterModule("Calc", typeof(CalculatorModule), members);

        Assert.That(engine.Evaluate("Calc.plus(2, 3)"), Is.EqualTo(5));
        Assert.That(engine.Evaluate("Calc.minus(5, 2)"), Is.EqualTo(3));

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("Calc.Sum(2, 3)"));
        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public void RegisterModule_WithoutExplicitMembers_ExposesAllPublicMembers()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Calc", typeof(CalculatorModule));

        Assert.That(engine.Evaluate("Calc.Sum(2, 3)"), Is.EqualTo(5));
        Assert.That(engine.Evaluate("Calc.Subtract(5, 2)"), Is.EqualTo(3));
        Assert.That(engine.Evaluate("Calc.Multiply(4, 3)"), Is.EqualTo(12));
    }

    [Test]
    public void RegisterModule_WithExplicitMembers_CanExposeProperties()
    {
        var engine = new CsEvalEngine();
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pi"] = typeof(ConstantsModule).GetProperty(nameof(ConstantsModule.Pi))!
        };
        engine.RegisterModule("Constants", typeof(ConstantsModule), members);

        Assert.That(engine.Evaluate("Constants.Pi"), Is.EqualTo(3.14159));

        var ex = Assert.Throws<EvalException>(() => engine.Evaluate("Constants.E"));
        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public void RegisterModule_CaseInsensitive_WorksWithExplicitMembers()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });
        var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["sum"] = typeof(CalculatorModule).GetMethod(nameof(CalculatorModule.Sum))!
        };
        engine.RegisterModule("calc", typeof(CalculatorModule), members);

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
