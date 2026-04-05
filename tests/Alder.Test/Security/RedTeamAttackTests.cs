using System.Reflection;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class RedTeamAttackTests(CompilationMode mode)
{
    [Test]
    public void Attack_FQN_Environment_MachineName_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_UserName_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.UserName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_CurrentDirectory_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.CurrentDirectory"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_CommandLine_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.CommandLine"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_OSVersion_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.OSVersion"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_ProcessPath_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Environment.ProcessPath"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_Strict()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict()).Evaluate("System.Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ShortName_Environment_Safe()
    {
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }
    [Test]
    public void Attack_FQN_ProcessStart_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""System.Diagnostics.Process.Start("calc")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_FQN_FileRead_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_FileWrite_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""System.IO.File.WriteAllText("/tmp/pwned", "data")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_FileDelete_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""System.IO.File.Delete("important.db")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_DirectoryGetFiles_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""System.IO.Directory.GetFiles("C:\\")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_PathGetTempPath_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.IO.Path.GetTempPath()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_ReflectionAssembly_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Reflection.Assembly.GetCallingAssembly()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }
    [Test]
    public void Attack_GetType_ReflectionChain_Trusted()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello");
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("text.GetType()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_TypeofAssembly_Trusted()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("typeof(string).Assembly"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_TypeofBaseType_Trusted()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("typeof(string).BaseType"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_TypeofGetMethod_Trusted()
    {
        Assert.Catch(() =>
            TestEngineFactory.Create(mode).Evaluate("""typeof(string).GetMethod("ToUpper")"""));
    }
    [Test]
    public void Attack_DelegateReturnsType_Trusted()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("getType", new Func<object, Type>(o => o.GetType()));
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""getType("hello")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_DelegateReturnsAssembly_Trusted()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("getAsm", new Func<Assembly>(() => typeof(string).Assembly));
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("getAsm()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_DelegateInvoke_SafeMode_Allowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));
        Assert.That(engine.Evaluate("fn(5)"), Is.EqualTo(6));
    }
    [Test]
    public void Attack_NewProcess_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("new System.Diagnostics.Process()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_NewObject_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("new object()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_NewList_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("new System.Collections.Generic.List<int>()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }
    [Test]
    public void Attack_HugeArrayAllocation()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("new int[100000000]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0202));
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
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode).Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8078));
    }
    [Test]
    public void Attack_GC_Collect_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.GC.Collect()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ThreadSleep_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Threading.Thread.Sleep(60000)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ConsoleReadLine_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("System.Console.ReadLine()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }
    [Test]
    public void Attack_Assignment_Strict()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x = 2"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_CompoundAssignment_Strict()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x += 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_Increment_Strict()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        engine.SetVariable("x", 1);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x++"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0101));
    }

    [Test]
    public void Attack_VarDeclaration_Strict()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict());
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("var x = 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1003));
    }
    [Test]
    public void Attack_ConditionalDeadBranch_DangerousBranch_NeverExecuted()
    {
        var result = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""false ? "danger" : "safe" """);
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
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("x", 0);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("    { try { var y = 1/x; return y; } catch (Exception e) { return e.GetType(); } }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_ExceptionMessage_Safe_Allowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("x", 0);
        var result = engine.Evaluate("    { try { var y = 1/x; return y; } catch (Exception e) { return e.Message; } }");
        Assert.That(result, Is.Not.Null);
    }
    [Test]
    public void Info_Typeof_FullName_Safe()
    {
        Assert.That(TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("typeof(string).FullName"), Is.EqualTo("System.String"));
    }

    [Test]
    public void Info_Typeof_IsPublic_Safe()
    {
        Assert.That(TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("typeof(string).IsPublic"), Is.True);
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
    public void Safe_Arithmetic() => Assert.That(TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("2 + 3 * 4"), Is.EqualTo(14));

    [Test]
    public void Safe_PropertyRead()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("text", "hello");
        Assert.That(engine.Evaluate("text.Length"), Is.EqualTo(5));
    }

    [Test]
    public void Safe_StringInterpolation()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("name", "World");
        Assert.That(engine.Evaluate("""$"Hello {name}" """), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Safe_NullCoalesce()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("x", (string?)null);
        Assert.That(engine.Evaluate("""x ?? "default" """), Is.EqualTo("default"));
    }

    [Test]
    public void Safe_Conditional()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("""x > 3 ? "big" : "small" """), Is.EqualTo("big"));
    }

    [Test]
    public void Safe_Nameof()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("x", 42);
        Assert.That(engine.Evaluate("nameof(x)"), Is.EqualTo("x"));
    }

    [Test]
    public void Attack_ConstructAlderEngine_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe()).Evaluate("""new Alder.AlderEngine().Evaluate("1 + 1")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_ConstructAlderEngine_Strict()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Strict()).Evaluate("""new Alder.AlderEngine().Evaluate("1 + 1")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_ConstructAlderEngine_WithRegisteredAssembly_Trusted()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.Types.AddAssembly(typeof(AlderEngine).Assembly);
            o.Types.AddNamespace("Alder");
        });
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new AlderEngine().Evaluate("System.Environment.MachineName")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ConstructAlderEngine_FQN_Trusted()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new Alder.AlderEngine().Evaluate("System.Environment.MachineName")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_AccessAlderOptions_FQN_Trusted()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("new Alder.AlderOptions()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_ConstructAlderEngine_ViaShortName_Trusted()
    {
        var engine = TestEngineFactory.Create(mode, o => {
            o.Types.AddAssembly(typeof(AlderEngine).Assembly);
            o.Types.AddNamespace("Alder");
        });
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("new AlderEngine()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_AlderNamespace_StaticAccess_Trusted()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("Alder.LanguageMode.Extended"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void Attack_SandboxOptions_Construction_Trusted()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("new Alder.SandboxOptions()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_AlderEngine_ViaVariable_MethodCall()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Sandbox = SandboxOptions.Safe());
        engine.SetVariable("eng", new AlderEngine());
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""eng.Evaluate("1 + 1")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_AlderEngine_ViaVariable_Trusted()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("eng", new AlderEngine());
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""eng.Evaluate("System.Environment.MachineName")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }
}
