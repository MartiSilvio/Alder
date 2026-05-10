using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class StaticMethodSecurityPolicyTests(CompilationMode mode)
{
    [Test]
    public void ExplicitPolicy_BlocksStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true, AllowAssignment = true, AllowPropertySet = true, AllowIndexSet = true });

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""int.Parse("42") """));
        Assert.That(ex!.Message, Does.Contain("security policy"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void ReadOnlyPolicy_BlocksStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true });

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""int.Parse("42") """));
        Assert.That(ex!.Message, Does.Contain("security policy"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Trusted_AllowsStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Trusted() with
        {
            TrustedTypes = [typeof(Path)]
        });

        var result = engine.Evaluate("""System.IO.Path.GetExtension("file.txt") """);

        Assert.That(result, Is.EqualTo(".txt"));
    }

    [Test]
    public void ExplicitPolicy_LambdasUnaffected()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true, AllowAssignment = true, AllowPropertySet = true, AllowIndexSet = true });

        var result = engine.Evaluate("{ var fn = (x) => x * 2; return fn(5); }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ExplicitPolicy_StaticPropertyAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true, AllowAssignment = true, AllowPropertySet = true, AllowIndexSet = true });

        Assert.That(engine.Evaluate<int>("int.MaxValue"), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ExplicitPolicy_StaticFieldAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true, AllowAssignment = true, AllowPropertySet = true, AllowIndexSet = true });

        // string.Empty is a static field read, allowed when static field reads are enabled
        Assert.That(engine.Evaluate<string>("string.Empty"), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Trusted_StaticPropertyAndFieldAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Trusted());

        Assert.That(engine.Evaluate("int.MaxValue"), Is.EqualTo(int.MaxValue));
        Assert.That(engine.Evaluate("double.NaN"), Is.EqualTo(double.NaN));
    }
}
