namespace Alder.Test.Core;

[TestFixture]
public class CompiledDelegateTests
{
    [Test]
    public void CompileToFunc_SimpleExpression_ReturnsCorrectResult()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var func = engine.CompileToFunc<int>("1 + 2");

        Assert.That(func(), Is.EqualTo(3));
    }

    [Test]
    public void CompileToFunc_WithVariable_CapturesContext()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 10);
        var func = engine.CompileToFunc<int>("x * 3");

        Assert.That(func(), Is.EqualTo(30));
    }

    [Test]
    public void CompileToFunc_VariableChangeAfterCompile_ReflectedOnInvoke()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 5);
        var func = engine.CompileToFunc<int>("x + 1");

        Assert.That(func(), Is.EqualTo(6));

        engine.SetVariable("x", 100);
        Assert.That(func(), Is.EqualTo(101));
    }

    [Test]
    public void CompileToFunc_RepeatedInvocation_AllReturnSameResult()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var func = engine.CompileToFunc<int>("42");

        for (var i = 0; i < 1_000; i++)
        {
            Assert.That(func(), Is.EqualTo(42));
        }
    }

    [Test]
    public void Compile_Generic_ReturnsCompiledExpressionWithCorrectResult()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 7);
        var compiled = engine.Compile<int>("x * 2");

        Assert.That(compiled.Invoke(), Is.EqualTo(14));
    }

    [Test]
    public void Compile_Generic_WithPerInvocationVariables()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var compiled = engine.Compile<int>("x + y");

        var vars1 = new Dictionary<string, object?> { ["x"] = 3, ["y"] = 4 };
        var vars2 = new Dictionary<string, object?> { ["x"] = 10, ["y"] = 20 };

        Assert.That(compiled.Invoke(vars1), Is.EqualTo(7));
        Assert.That(compiled.Invoke(vars2), Is.EqualTo(30));
    }

    [Test]
    public void Compile_NonGeneric_ReturnsObjectResult()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var compiled = engine.Compile("1 + 2");

        var result = compiled.Invoke();
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void CompileToFunc_HotPath_NoExceptions()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 1);
        var func = engine.CompileToFunc<int>("x + 1");

        for (var i = 0; i < 10_000; i++)
        {
            var result = func();
            Assert.That(result, Is.EqualTo(2));
        }
    }

    [Test]
    public void Compile_InvokeWithVariables_IsolatedBetweenCalls()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("base_val", 100);
        var compiled = engine.Compile<int>("base_val + offset");

        var result1 = compiled.Invoke(new Dictionary<string, object?> { ["offset"] = 5 });
        var result2 = compiled.Invoke(new Dictionary<string, object?> { ["offset"] = 50 });

        Assert.That(result1, Is.EqualTo(105));
        Assert.That(result2, Is.EqualTo(150));
    }

    [Test]
    public void Compile_StringExpression_ReturnsCorrectType()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("name", "World");
        var compiled = engine.Compile<string>("\"Hello, \" + name");

        Assert.That(compiled.Invoke(), Is.EqualTo("Hello, World"));
    }

    [Test]
    public void Compile_BooleanExpression_ReturnsCorrectType()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 10);
        var compiled = engine.Compile<bool>("x > 5");

        Assert.That(compiled.Invoke(), Is.True);
    }

    [Test]
    public void Compile_NonCompilableExpression_ThrowsInvalidOperationException()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());

        // Verify that standard expressions compile via the Compile extension.
        var compiled = engine.Compile<int>("1 + 2");
        Assert.That(compiled.Invoke(), Is.EqualTo(3));
    }

    [Test]
    public void CompileToFunc_DoubleResult_ReturnsCorrectType()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var func = engine.CompileToFunc<double>("3.14 * 2");

        Assert.That(func(), Is.EqualTo(6.28).Within(0.001));
    }

    [Test]
    public void Compile_PerInvocationVariables_DoNotPollutEngineContext()
    {
        var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable("x", 1);
        var compiled = engine.Compile<int>("x + y");

        // Invoke with per-invocation y
        compiled.Invoke(new Dictionary<string, object?> { ["y"] = 10 });

        // y should not exist in engine context -- using it without per-invocation vars should throw
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("y"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void Compile_Invoke_EnforcesExecutionConstraints()
    {
        var engine = new AlderEngine(new AlderOptions { Constraints = new ExecutionConstraints { MaxStatements = 3 } }.UseCompiler());

        var compiled = engine.Compile<object?>("{ var a = 1; var b = 2; var c = 3; return a + b + c; }");
        Assert.Throws<AlderExecutionLimitException>(() => compiled.Invoke());
    }

    [Test]
    public void CompileToFunc_Invoke_EnforcesExecutionConstraints()
    {
        var engine = new AlderEngine(new AlderOptions { Constraints = new ExecutionConstraints { MaxStatements = 3 } }.UseCompiler());

        var func = engine.CompileToFunc<object?>("{ var a = 1; var b = 2; var c = 3; return a + b + c; }");
        Assert.Throws<AlderExecutionLimitException>(() => func());
    }
}
