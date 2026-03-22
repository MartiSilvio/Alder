using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class RedTeamAttackTests(CompilationMode mode)
{
    private AlderEngine Safe() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());

    private AlderEngine Strict() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());

    private AlderEngine Trusted() => TestEngineFactory.Create(mode);
    [Test]
    public void Attack_FQN_Environment_MachineName_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.MachineName"));
    }

    [Test]
    public void Attack_FQN_Environment_UserName_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.UserName"));
    }

    [Test]
    public void Attack_FQN_Environment_CurrentDirectory_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.CurrentDirectory"));
    }

    [Test]
    public void Attack_FQN_Environment_CommandLine_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.CommandLine"));
    }

    [Test]
    public void Attack_FQN_Environment_OSVersion_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.OSVersion"));
    }

    [Test]
    public void Attack_FQN_Environment_ProcessPath_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("System.Environment.ProcessPath"));
    }

    [Test]
    public void Attack_FQN_Environment_Strict()
    {
        Assert.Throws<AlderException>(() => Strict().Evaluate("System.Environment.MachineName"));
    }

    [Test]
    public void Attack_ShortName_Environment_Safe()
    {
        Assert.Throws<AlderException>(() => Safe().Evaluate("Environment.MachineName"));
    }
    [Test]
    public void Attack_FQN_ProcessStart_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""System.Diagnostics.Process.Start("calc")"""));
    }

    [Test]
    public void Attack_FQN_FileRead_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
    }

    [Test]
    public void Attack_FQN_FileWrite_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""System.IO.File.WriteAllText("/tmp/pwned", "data")"""));
    }

    [Test]
    public void Attack_FQN_FileDelete_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""System.IO.File.Delete("important.db")"""));
    }

    [Test]
    public void Attack_FQN_DirectoryGetFiles_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""System.IO.Directory.GetFiles("C:\\")"""));
    }

    [Test]
    public void Attack_FQN_PathGetTempPath_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("System.IO.Path.GetTempPath()"));
    }

    [Test]
    public void Attack_FQN_ReflectionAssembly_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("System.Reflection.Assembly.GetCallingAssembly()"));
    }
    [Test]
    public void Attack_GetType_ReflectionChain_Trusted()
    {
        var engine = Trusted();
        engine.SetVariable("text", "hello");
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.GetType()"));
    }

    [Test]
    public void Attack_TypeofAssembly_Trusted()
    {
        Assert.Throws<AlderException>(() =>
            Trusted().Evaluate("typeof(string).Assembly"));
    }

    [Test]
    public void Attack_TypeofBaseType_Trusted()
    {
        Assert.Throws<AlderException>(() =>
            Trusted().Evaluate("typeof(string).BaseType"));
    }

    [Test]
    public void Attack_TypeofGetMethod_Trusted()
    {
        Assert.Catch(() =>
            Trusted().Evaluate("""typeof(string).GetMethod("ToUpper")"""));
    }
    [Test]
    public void Attack_DelegateReturnsType_Trusted()
    {
        var engine = Trusted();
        engine.SetVariable("getType", new Func<object, Type>(o => o.GetType()));
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""getType("hello")"""));
    }

    [Test]
    public void Attack_DelegateReturnsAssembly_Trusted()
    {
        var engine = Trusted();
        engine.SetVariable("getAsm", new Func<System.Reflection.Assembly>(() => typeof(string).Assembly));
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("getAsm()"));
    }

    [Test]
    public void Attack_DelegateInvoke_SafeMode_Allowed()
    {
        var engine = Safe();
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));
        Assert.That(engine.Evaluate("fn(5)"), Is.EqualTo(6));
    }
    [Test]
    public void Attack_NewProcess_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("new System.Diagnostics.Process()"));
    }

    [Test]
    public void Attack_NewObject_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("new object()"));
    }

    [Test]
    public void Attack_NewList_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("new System.Collections.Generic.List<int>()"));
    }
    [Test]
    public void Attack_HugeArrayAllocation()
    {
        Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("new int[100000000]"));
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
        var expr = new string('(', depth) + "1" + new string(')', depth);
        Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate(expr));
    }
    [Test]
    public void Attack_GC_Collect_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("System.GC.Collect()"));
    }

    [Test]
    public void Attack_ThreadSleep_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("System.Threading.Thread.Sleep(60000)"));
    }

    [Test]
    public void Attack_ConsoleReadLine_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("System.Console.ReadLine()"));
    }
    [Test]
    public void Attack_Assignment_Strict()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() => engine.Evaluate("x = 2"));
    }

    [Test]
    public void Attack_CompoundAssignment_Strict()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() => engine.Evaluate("x += 1"));
    }

    [Test]
    public void Attack_Increment_Strict()
    {
        var engine = Strict();
        engine.SetVariable("x", 1);
        Assert.Throws<AlderException>(() => engine.Evaluate("x++"));
    }

    [Test]
    public void Attack_VarDeclaration_Strict()
    {
        var engine = Strict();
        Assert.Throws<AlderException>(() => engine.Evaluate("var x = 1"));
    }
    [Test]
    public void Attack_ConditionalDeadBranch_DangerousBranch_NeverExecuted()
    {
        var result = Safe().Evaluate("""false ? "danger" : "safe" """);
        Assert.That(result, Is.EqualTo("safe"));
    }
    [Test]
    public void Attack_UseAfterDispose()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.Evaluate("1 + 1"));
    }

    [Test]
    public void Attack_ChildAfterParentDispose()
    {
        var engine = TestEngineFactory.Create(mode);
        var child = engine.CreateChild();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => child.Evaluate("1 + 1"));
    }
    [Test]
    public void Attack_ExceptionGetType_Safe()
    {
        var engine = Safe();
        engine.SetVariable("x", 0);
        Assert.Throws<AlderException>(() => engine.Evaluate("""
            { try { var y = 1/x; return y; } catch (Exception e) { return e.GetType(); } }
        """));
    }

    [Test]
    public void Attack_ExceptionMessage_Safe_Allowed()
    {
        var engine = Safe();
        engine.SetVariable("x", 0);
        var result = engine.Evaluate("""
            { try { var y = 1/x; return y; } catch (Exception e) { return e.Message; } }
        """);
        Assert.That(result, Is.Not.Null);
    }
    [Test]
    public void Info_Typeof_FullName_Safe()
    {
        Assert.That(Safe().Evaluate("typeof(string).FullName"), Is.EqualTo("System.String"));
    }

    [Test]
    public void Info_Typeof_IsPublic_Safe()
    {
        Assert.That(Safe().Evaluate("typeof(string).IsPublic"), Is.EqualTo(true));
    }
    [Test]
    public void Parity_Construction_SameErrorCode()
    {
        var intEx = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(CompilationMode.Interpreted, o => o.Sandbox = SandboxOptions.Safe())
                .Evaluate("new object()"));
        var compEx = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(CompilationMode.Compiled, o => o.Sandbox = SandboxOptions.Safe())
                .Evaluate("new object()"));
        Assert.That(intEx!.ErrorCode, Is.EqualTo(compEx!.ErrorCode));
    }
    [Test]
    public void Safe_Arithmetic() => Assert.That(Safe().Evaluate("2 + 3 * 4"), Is.EqualTo(14));

    [Test]
    public void Safe_PropertyRead()
    {
        var engine = Safe();
        engine.SetVariable("text", "hello");
        Assert.That(engine.Evaluate("text.Length"), Is.EqualTo(5));
    }

    [Test]
    public void Safe_StringInterpolation()
    {
        var engine = Safe();
        engine.SetVariable("name", "World");
        Assert.That(engine.Evaluate("""$"Hello {name}" """), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Safe_NullCoalesce()
    {
        var engine = Safe();
        engine.SetVariable("x", (string?)null);
        Assert.That(engine.Evaluate("""x ?? "default" """), Is.EqualTo("default"));
    }

    [Test]
    public void Safe_Conditional()
    {
        var engine = Safe();
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("""x > 3 ? "big" : "small" """), Is.EqualTo("big"));
    }

    [Test]
    public void Safe_Nameof()
    {
        var engine = Safe();
        engine.SetVariable("x", 42);
        Assert.That(engine.Evaluate("nameof(x)"), Is.EqualTo("x"));
    }

    [Test]
    public void Attack_ConstructAlderEngine_Safe()
    {
        Assert.Throws<AlderException>(() =>
            Safe().Evaluate("""new Alder.AlderEngine().Evaluate("1 + 1")"""));
    }

    [Test]
    public void Attack_ConstructAlderEngine_Strict()
    {
        Assert.Throws<AlderException>(() =>
            Strict().Evaluate("""new Alder.AlderEngine().Evaluate("1 + 1")"""));
    }

    [Test]
    public void Attack_ConstructAlderEngine_WithRegisteredAssembly_Trusted()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.Types.AddAssembly(typeof(AlderEngine).Assembly);
            o.Types.AddNamespace("Alder");
        });
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new AlderEngine().Evaluate("System.Environment.MachineName")"""));
    }

    [Test]
    public void Attack_ConstructAlderEngine_FQN_Trusted()
    {
        var engine = Trusted();
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new Alder.AlderEngine().Evaluate("System.Environment.MachineName")"""));
    }

    [Test]
    public void Attack_AccessAlderOptions_FQN_Trusted()
    {
        Assert.Throws<AlderException>(() =>
            Trusted().Evaluate("new Alder.AlderOptions()"));
    }

    [Test]
    public void Attack_ConstructAlderEngine_ViaShortName_Trusted()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.Types.AddAssembly(typeof(AlderEngine).Assembly);
            o.Types.AddNamespace("Alder");
        });
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new AlderEngine()"""));
    }

    [Test]
    public void Attack_AlderNamespace_StaticAccess_Trusted()
    {
        Assert.Throws<AlderException>(() =>
            Trusted().Evaluate("Alder.LanguageMode.Extended"));
    }

    [Test]
    public void Attack_SandboxOptions_Construction_Trusted()
    {
        Assert.Throws<AlderException>(() =>
            Trusted().Evaluate("new Alder.SandboxOptions()"));
    }

    [Test]
    public void Attack_AlderEngine_ViaVariable_MethodCall()
    {
        var engine = Safe();
        engine.SetVariable("eng", new AlderEngine());
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""eng.Evaluate("1 + 1")"""));
    }

    [Test]
    public void Attack_AlderEngine_ViaVariable_Trusted()
    {
        var engine = Trusted();
        engine.SetVariable("eng", new AlderEngine());
        Assert.Throws<AlderException>(() =>
            engine.Evaluate("""eng.Evaluate("System.Environment.MachineName")"""));
    }
}
