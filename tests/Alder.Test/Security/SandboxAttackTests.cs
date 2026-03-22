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

    // ── Process/IO escape attempts ──────────────────────────────────

    [Test]
    public void Attack_ConstructProcess_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new System.Diagnostics.Process()"""));
    }

    [Test]
    public void Attack_ProcessStart_Blocked()
    {
        var engine = Strict();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Diagnostics.Process.Start("calc")"""));
    }

    [Test]
    public void Attack_FileReadAllText_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
    }

    [Test]
    public void Attack_FileWriteAllText_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.File.WriteAllText("/tmp/pwned", "data")"""));
    }

    [Test]
    public void Attack_EnvironmentGetVariable_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Environment.GetEnvironmentVariable("PATH")"""));
    }

    // ── Reflection escape attempts ──────────────────────────────────

    [Test]
    public void Attack_TypeofGetMethods_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(string).GetMethods()"""));
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
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.GetType()"));
    }

    [Test]
    public void Attack_TypeofAssembly_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""typeof(string).Assembly"""));
    }

    // ── Method call bypass attempts ─────────────────────────────────

    [Test]
    public void Attack_MethodCallInSafeMode_Blocked()
    {
        var engine = SafeWithVariables();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.ToUpper()"));
    }

    [Test]
    public void Attack_MethodCallViaToString_Blocked()
    {
        var engine = SafeWithVariables();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("num.ToString()"));
    }

    [Test]
    public void Attack_StaticMethodCall_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("int.Parse(\"42\")"));
    }

    // ── Construction bypass attempts ────────────────────────────────

    [Test]
    public void Attack_NewObject_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("new object()"));
    }

    [Test]
    public void Attack_NewList_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("new System.Collections.Generic.List<int>()"));
    }

    [Test]
    public void Attack_NewStringBuilder_Blocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("new System.Text.StringBuilder()"));
    }

    // ── Assignment bypass attempts ──────────────────────────────────

    [Test]
    public void Attack_AssignmentInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("x = 2"));
    }

    [Test]
    public void Attack_CompoundAssignmentInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("x += 1"));
    }

    [Test]
    public void Attack_IncrementInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("x++"));
    }

    [Test]
    public void Attack_NullCoalesceAssignInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("x", (int?)null);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("x ??= 5"));
    }

    [Test]
    public void Attack_VarDeclarationInStrictMode_Blocked()
    {
        var engine = Strict();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("var x = 1"));
    }

    // ── Property write bypass attempts ──────────────────────────────

    [Test]
    public void Attack_PropertySetInStrictMode_Blocked()
    {
        var engine = Strict();
        var obj = new TestMutableObject { Value = 10 };
        engine.SetVariable("obj", obj);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("obj.Value = 99"));
    }

    [Test]
    public void Attack_IndexSetInStrictMode_Blocked()
    {
        var engine = Strict();
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("items[0] = 99"));
    }

    // ── Delegate bypass attempts ────────────────────────────────────

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

    // ── Type allowlist enforcement ──────────────────────────────────

    [Test]
    public void Attack_AllowedTypes_BlocksUnlisted()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions
        {
            AllowMethodCalls = true,
            AllowPropertyRead = true,
            AllowStaticPropertyRead = true,
            AllowStaticFieldRead = true,
            AllowConstruction = true,
            AllowedTypes = new HashSet<Type> { typeof(string), typeof(int) }
        });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });

        Assert.Throws<AlderException>(() =>
            engine.Evaluate("items.Count"));
    }

    [Test]
    public void Attack_AllowedTypes_PermitsListed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = new SandboxOptions
        {
            AllowMethodCalls = true,
            AllowPropertyRead = true,
            AllowStaticPropertyRead = true,
            AllowStaticFieldRead = true,
            AllowedTypes = new HashSet<Type> { typeof(string) }
        });
        engine.SetVariable("text", "hello");

        var result = engine.Evaluate("text.Length");
        Assert.That(result, Is.EqualTo(5));
    }

    // ── Resource exhaustion attacks ──────────────────────────────────

    [Test]
    public void Attack_HugeArrayAllocation_Blocked()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("new int[100000000]"));
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
        Assert.Throws<AlderException>(() => engine.Evaluate(expr));
    }

    // ── Disposed engine attacks ─────────────────────────────────────

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

    // ── Namespace blocking ──────────────────────────────────────────

    [Test]
    public void Attack_SystemIO_NamespaceBlocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.IO.Path.GetTempPath()"""));
    }

    [Test]
    public void Attack_SystemNet_NamespaceBlocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new System.Net.WebClient()"""));
    }

    [Test]
    public void Attack_SystemReflection_NamespaceBlocked()
    {
        var engine = Safe();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""System.Reflection.Assembly.GetCallingAssembly()"""));
    }

    // ── Compiled mode parity ────────────────────────────────────────

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

    // ── Safe mode positive tests ────────────────────────────────────

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
