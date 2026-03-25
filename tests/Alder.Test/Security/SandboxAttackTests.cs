using System.Text.RegularExpressions;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class SandboxAttackTests(CompilationMode mode)
{
    private AlderEngine Strict() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

    private AlderEngine Safe() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

    private AlderEngine SafeWithVariables()
    {
        var engine = Safe();
        engine.SetVariable("text", "hello");
        engine.SetVariable("num", 42);
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        return engine;
    }
    [Test]
    public void Attack_ConstructProcess_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new System.Diagnostics.Process()"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_ProcessStart_Blocked()
    {
        var engine = Strict();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Diagnostics.Process.Start("calc")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_FileReadAllText_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FileWriteAllText_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.WriteAllText("/tmp/pwned", "data")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_EnvironmentGetVariable_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Environment.GetEnvironmentVariable("PATH")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }
    [Test]
    public void Attack_TypeofGetMethods_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(string).GetMethods()"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_TypeofGetMethod_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Catch(() =>
            engine.Evaluate("""typeof(string).GetMethod("ToUpper")"""));
    }

    [Test]
    public void Attack_GetTypeOnVariable_Blocked()
    {
        var engine = SafeWithVariables();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.GetType()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_TypeofAssembly_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(string).Assembly"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }
    [Test]
    public void Attack_MethodCallInSafeMode_Blocked()
    {
        var engine = SafeWithVariables();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.ToUpper()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_MethodCallViaToString_Blocked()
    {
        var engine = SafeWithVariables();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("num.ToString()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_StaticMethodCall_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("int.Parse(\"42\")"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }
    [Test]
    public void Attack_NewObject_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new object()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_NewList_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new System.Collections.Generic.List<int>()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_NewStringBuilder_Blocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new System.Text.StringBuilder()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }
    [Test]
    public void Attack_AssignmentInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("x = 2"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_CompoundAssignmentInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("x += 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_IncrementInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("x++"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_NullCoalesceAssignInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", (int?)null);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("x ??= 5"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_VarDeclarationInStrictMode_Blocked()
    {
        var engine = Strict();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("var x = 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1003));
    }
    [Test]
    public void Attack_PropertySetInStrictMode_Blocked()
    {
        var engine = Strict();
        var obj = new TestMutableObject { Value = 10 };
        engine.SetVariable("obj", obj);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("obj.Value = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0105));
    }

    [Test]
    public void Attack_IndexSetInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("items[0] = 99"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0102));
    }
    [Test]
    public void Attack_DelegateInvoke_InSafeMode_Allowed()
    {
        var engine = Safe();
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));
        Assert.That(engine.Evaluate("fn(5)"), Is.EqualTo(6));
    }

    [Test]
    public void Attack_DelegateInvoke_InTrustedMode_Allowed()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));
        var result = engine.Evaluate("fn(5)");
        Assert.That(result, Is.EqualTo(6));
    }
    [Test]
    public void Attack_TrustedTypes_OverridesDenyList()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Types.AddAssembly(typeof(Console).Assembly);
            o.Types.AddNamespace("System");
            o.Sandbox = SandboxOptions.Trusted() with
            {
                TrustedTypes = [typeof(Console)]
            };
        });

        Assert.DoesNotThrow(() => engine.Evaluate("Console.Out"));
    }

    [Test]
    public void Attack_TrustedTypes_PermitsListed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions
        {
            AllowMethodCalls = true,
            AllowPropertyRead = true,
            AllowStaticPropertyRead = true,
            AllowStaticFieldRead = true,
            TrustedTypes = [typeof(string)]
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");
        Assert.That(result, Is.EqualTo(5));
    }
    [Test]
    public void Attack_HugeArrayAllocation_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new int[100000000]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0203));
    }

    [Test]
    public void Attack_ReDoS_TimesOut()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);

        Assert.Throws<RegexMatchTimeoutException>(() =>
            engine.Evaluate("""
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaab" =~ "(a+)+$"
            """));
    }

    [Test]
    public void Attack_InfiniteLoop_StatementLimited()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Constraints = new ExecutionConstraints { MaxStatements = 1000 });

        Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ while (true) { } }"));
    }

    [Test]
    public void Attack_DeepNesting_DepthLimited()
    {
        var depth = 600;
        var expr = string.Join("", Enumerable.Repeat("(", depth)) +
                   "1" +
                   string.Join("", Enumerable.Repeat(")", depth));

        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CS8078));
    }
    [Test]
    public void Attack_UseAfterDispose_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            engine.Evaluate("1 + 1"));
    }

    [Test]
    public void Attack_ChildAfterParentDispose_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var child = engine.CreateChild();
        engine.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            child.Evaluate("1 + 1"));
    }
    [Test]
    public void Attack_SystemIO_NamespaceBlocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.Path.GetTempPath()"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_SystemNet_NamespaceBlocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new System.Net.WebClient()"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_SystemReflection_NamespaceBlocked()
    {
        var engine = Safe();
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Reflection.Assembly.GetCallingAssembly()"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }
    [Test]
    public void CompiledAndInterpreted_SameSecurityBehavior_Construction()
    {
        var interpreted = TestEngineFactory.Create(CompilationMode.Interpreted, o => o.Sandbox = SandboxOptions.Safe());
        var compiled = TestEngineFactory.Create(CompilationMode.Compiled, o => o.Sandbox = SandboxOptions.Safe());

        var intEx = Assert.Throws<AlderException>(() => interpreted.Evaluate("new object()"));
        var compEx = Assert.Throws<AlderException>(() => compiled.Evaluate("new object()"));

        Assert.That(intEx!.ErrorCode, Is.EqualTo(compEx!.ErrorCode));
    }

    [Test]
    public void CompiledAndInterpreted_SameSecurityBehavior_MethodCall()
    {
        var interpreted = TestEngineFactory.Create(CompilationMode.Interpreted, o => o.Sandbox = SandboxOptions.Safe());
        var compiled = TestEngineFactory.Create(CompilationMode.Compiled, o => o.Sandbox = SandboxOptions.Safe());

        interpreted.SetVariable("text", "hello");
        compiled.SetVariable("text", "hello");

        var intEx = Assert.Throws<AlderException>(() => interpreted.Evaluate("text.ToUpper()"));
        var compEx = Assert.Throws<AlderException>(() => compiled.Evaluate("text.ToUpper()"));

        Assert.That(intEx!.ErrorCode, Is.EqualTo(compEx!.ErrorCode));
    }
    [Test]
    public void Safe_AllowsArithmetic()
    {
        var engine = Safe();
        Assert.That(engine.Evaluate("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsStringConcatenation()
    {
        var engine = Safe();
        engine.SetVariable("name", "World");
        Assert.That(engine.Evaluate("""
            "Hello " + name
        """), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Safe_AllowsPropertyRead()
    {
        var engine = SafeWithVariables();
        Assert.That(engine.Evaluate("text.Length"), Is.EqualTo(5));
    }

    [Test]
    public void Safe_AllowsVariableAssignment()
    {
        var engine = Safe();
        engine.SetVariable("x", 1);
        Assert.That(engine.Evaluate("x = 2"), Is.EqualTo(2));
    }

    [Test]
    public void Safe_AllowsConditionalExpression()
    {
        var engine = Safe();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x > 3 ? \"big\" : \"small\""), Is.EqualTo("big"));
    }

    [Test]
    public void Safe_AllowsNullCoalesce()
    {
        var engine = Safe();
        engine.SetVariable("x", (string?)null);
        Assert.That(engine.Evaluate("""x ?? "default" """), Is.EqualTo("default"));
    }

    public class TestMutableObject
    {
        public int Value { get; set; }
    }
}
