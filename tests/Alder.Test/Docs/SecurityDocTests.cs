using Alder.Diagnostics;
using Alder.Security;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class SecurityDocTests(CompilationMode mode)
{
    [Test]
    public void Presets_Trusted_AllowsEverything()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.That(engine.Evaluate<string>(""" "hello".ToUpper() """), Is.EqualTo("HELLO"));
        Assert.That(engine.Evaluate<int>("new List<int> { 1, 2, 3 }.Count"), Is.EqualTo(3));
    }

    [Test]
    public void Presets_Safe_BlocksMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate(""" "hello".ToUpper() """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Presets_Safe_BlocksConstruction()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new List<int>()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Presets_Strict_BlocksAssignment()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable<int>("x", 5);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("x = 10"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void CustomSandbox_SafeWithMethodCalls()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe() with
            {
                AllowMethodCalls = true,
                AllowConstruction = true
            };
        });

        Assert.That(engine.Evaluate<string>(""" "hello".ToUpper() """), Is.EqualTo("HELLO"));
        Assert.That(engine.Evaluate<int>("new List<int> { 1, 2, 3 }.Count"), Is.EqualTo(3));
    }

    [Test]
    public void TrustedTypes_AllowThroughDenyList()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe() with
            {
                TrustedTypes = new HashSet<Type> { typeof(System.IO.FileAttributes) }
            };
        });

        // FileAttributes is in System.IO (denied namespace) but trusted explicitly
        Assert.DoesNotThrow(() => engine.Evaluate("System.IO.FileAttributes.ReadOnly"));
    }

    [Test]
    public void ReflectionBlocked_EvenInTrusted()
    {
        var engine = TestEngineFactory.Create(mode);

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("typeof(string).GetMethods()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void ExecutionLimits_MaxStatements()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Constraints = new ExecutionConstraints { MaxStatements = 10 };
        });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("var x = 0; for (var i = 0; i < 100; i++) { x += i; } return x;"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    [Test]
    public void ExecutionLimits_MaxLoopIterations()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Constraints = new ExecutionConstraints { MaxLoopIterations = 5 };
        });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("var x = 0; for (var i = 0; i < 100; i++) { x++; } return x;"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void TypeBlocking_FileIO_Blocked()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void TypeBlocking_Environment_Blocked()
    {
        // Use Trusted + custom deny to test type blocking specifically (not method call blocking)
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Trusted() with
            {
                DeniedTypes = new HashSet<Type> { typeof(Environment) }
            };
        });

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""Environment.GetEnvironmentVariable("PATH")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Safe_AllowsStaticPropertyRead()
    {
        // Safe() allows static property/field reads (int.MaxValue, Math.PI, string.Empty)
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

        Assert.That(engine.Evaluate<int>("int.MaxValue"), Is.EqualTo(int.MaxValue));
        Assert.That(engine.Evaluate<double>("Math.PI"), Is.EqualTo(Math.PI));
    }

    [Test]
    public void ExtensionMethods_BypassMethodCallBlock()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable<int[]>("items", new[] { 1, 2, 3 });

        // LINQ extension methods work even with AllowMethodCalls = false
        var result = engine.Evaluate<int>("items.Where(x => x > 1).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void MaxArrayLength_Enforced()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Trusted() with { MaxArrayLength = 100 };
        });

        Assert.That(engine.Evaluate<int>("new int[50].Length"), Is.EqualTo(50));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new int[200]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0202));
    }

    [Test]
    public void MaxArrayLength_DefaultAllowsLargeArrays()
    {
        var engine = TestEngineFactory.Create(mode);

        Assert.That(engine.Evaluate<int>("new int[10000].Length"), Is.EqualTo(10000));
    }

    [Test]
    public void RegexTimeout_Configurable()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = LanguageMode.Extended;
            o.Sandbox = SandboxOptions.Trusted() with
            {
                RegexTimeout = TimeSpan.FromMilliseconds(100)
            };
        });

        // Simple regex should succeed within timeout
        Assert.That(engine.Evaluate<bool>(""" "hello" =~ "h.*o" """), Is.True);
    }
}
