using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class StaticMethodSandboxTests(CompilationMode mode)
{
    [Test]
    public void Safe_BlocksStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""int.Parse("42") """));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Strict_BlocksStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""int.Parse("42") """));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Trusted_AllowsStaticMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted() with
        {
            TrustedTypes = [typeof(Path)]
        });

        var result = engine.Evaluate("""System.IO.Path.GetExtension("file.txt") """);

        Assert.That(result, Is.EqualTo(".txt"));
    }

    [Test]
    public void Safe_LambdasUnaffected()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var result = engine.Evaluate("{ var fn = (x) => x * 2; return fn(5); }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Safe_StaticPropertyAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        Assert.That(engine.Evaluate<int>("int.MaxValue"), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void Safe_StaticFieldAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        // string.Empty is a static field read, allowed in Safe mode
        Assert.That(engine.Evaluate<string>("string.Empty"), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Trusted_StaticPropertyAndFieldAccessAllowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Trusted());

        Assert.That(engine.Evaluate("int.MaxValue"), Is.EqualTo(int.MaxValue));
        Assert.That(engine.Evaluate("double.NaN"), Is.EqualTo(double.NaN));
    }
}
