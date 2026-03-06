using CsEval.Diagnostics;

namespace CsEval.Test.Compilation;

[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CompiledHotPathRegressionTests(CompilationMode mode)
{
    [Test]
    public void TypeNameIdentifier_ResolvesInCompiledPath()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void LogicalOperator_WithNonBooleanOperand_ThrowsCs0019()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("1 && 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void LocalVariableTypeFlow_AssignmentThenArithmetic_RemainsValid()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 1; x = 2L; return x + 1; }");
        Assert.That(Convert.ToInt64(result), Is.EqualTo(3));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassFunctionShadowing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("x", 5);
        engine.RegisterFunction("x", _ => 123);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("x + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassModuleShadowing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("Math", 5);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Math + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void TypedIdentifierFastPath_RespectsCaseInsensitiveLookup()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            IsCaseSensitive = false
        });
        engine.SetVariable<int>("Value", 41);

        var result = engine.Evaluate("value + 1");
        Assert.That(result, Is.EqualTo(42));
    }
}
