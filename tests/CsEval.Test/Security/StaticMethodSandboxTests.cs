namespace CsEval.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StaticMethodSandboxTests(CompilationMode mode)
{
    [Test]
    public void Safe_BlocksStaticMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("System.IO.File.Exists(\"/tmp\")"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Strict_BlocksStaticMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Strict()
        });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("System.IO.File.Exists(\"/tmp\")"));
        Assert.That(ex!.Message, Does.Contain("sandbox"));
    }

    [Test]
    public void Trusted_AllowsStaticMethodCalls()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Trusted()
        });

        var result = engine.Evaluate("System.IO.Path.GetExtension(\"file.txt\")");

        Assert.That(result, Is.EqualTo(".txt"));
    }

    [Test]
    public void Safe_ModuleMethodsUnaffected()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("Math.Abs(-42)");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Safe_LambdasUnaffected()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("{ var fn = (x) => x * 2; return fn(5); }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Safe_StaticPropertyAccessUnaffected()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("int.MaxValue");

        Assert.That(result, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void Safe_StaticFieldAccessUnaffected()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = mode,
            Sandbox = SandboxOptions.Safe()
        });

        var result = engine.Evaluate("double.NaN");

        Assert.That(result, Is.EqualTo(double.NaN));
    }
}
