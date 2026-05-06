using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
[Parallelizable(ParallelScope.Children)]
public class RedTeamAttackTests(CompilationMode mode)
{
    [Test]
    public void Attack_FQN_Environment_MachineName_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("System.Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ShortName_Environment_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_Environment_Strict()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Strict())
                .Evaluate("System.Environment.MachineName"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_ProcessStart_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("""System.Diagnostics.Process.Start("calc")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_FQN_FileRead_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("""System.IO.File.ReadAllText("/etc/passwd")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_FQN_ReflectionAssembly_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("System.Reflection.Assembly.GetCallingAssembly()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_TypeofBaseType_Allowed()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("typeof(string).BaseType");
        Assert.That(result, Is.EqualTo(typeof(object)));
    }

    [Test]
    public void Attack_TypeofGetMethod_Blocked()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode).Evaluate("""typeof(object).GetMethod("GetType")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
    }

    [Test]
    public void Attack_DelegateInvoke_SafeMode_Allowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe());
        engine.SetVariable("fn", new Func<int, int>(x => x + 1));
        Assert.That(engine.Evaluate("fn(5)"), Is.EqualTo(6));
    }

    [Test]
    public void Attack_NewProcess_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("new System.Diagnostics.Process()"));
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
        var depth = 1100;
        var expr = new string('(', depth) + "1" + new string(')', depth);
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode).Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8078));
    }

    [Test]
    public void Attack_GC_Collect_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("System.GC.Collect()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ThreadSleep_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("System.Threading.Thread.Sleep(60000)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_ConsoleReadLine_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("System.Console.ReadLine()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107).Or.EqualTo(DiagnosticCode.CS0246));
    }

    [Test]
    public void Attack_ConditionalDeadBranch_DangerousBranch_NeverExecuted()
    {
        var result = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
            .Evaluate("""false ? "danger" : "safe" """);
        Assert.That(result, Is.EqualTo("safe"));
    }

    [Test]
    public void Attack_ExceptionGetType_Safe()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe());
        engine.SetVariable("x", 0);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""
                {
                    try
                    {
                        var y = 1/x;
                        return y;
                    }
                    catch (Exception e)
                    {
                        return e.GetType();
                    }
                }
            """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }

    [Test]
    public void Attack_ExceptionMessage_Safe_Allowed()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe());
        engine.SetVariable("x", 0);
        var result = engine.Evaluate("""
            {
                try
                {
                    var y = 1/x;
                    return y;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        """);
        Assert.That(result, Is.Not.Null);
    }

#if NET8_0_OR_GREATER
    [Test]
    public void Parity_Construction_SameErrorCode()
    {
        var interpretedEx = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(CompilationMode.Interpreted, o => o.Security = SecurityOptions.Safe())
                .Evaluate("new object()"));
        var compiledEx = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(CompilationMode.Compiled, o => o.Security = SecurityOptions.Safe())
                .Evaluate("new object()"));
        Assert.That(interpretedEx!.ErrorCode, Is.EqualTo(compiledEx!.ErrorCode));
    }
#endif

    [Test]
    public void Attack_ConstructAlderEngine_Safe()
    {
        var ex = Assert.Throws<AlderException>(() =>
            TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe())
                .Evaluate("""new Alder.AlderEngine().Evaluate("1 + 1")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0106));
    }

    [Test]
    public void Attack_ConstructAlderEngine_WithRegisteredAssembly_Trusted()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Types.AddAssembly(typeof(AlderEngine).Assembly);
            o.Types.AddNamespace("Alder");
        });
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("""new AlderEngine().Evaluate("System.Environment.MachineName")"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0107));
    }

    [Test]
    public void Attack_AlderEngine_ViaVariable_MethodCall()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Security = SecurityOptions.Safe());
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
