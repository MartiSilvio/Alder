using CsEval.Test._Infrastructure;
using CsEval.Test.Binding;

namespace CsEval.Test.Parity;

[TestFixture]
public sealed class ExecutionModeParityTests
{
    [Test]
    public void Trace_InterpretedAndCompiled_ShouldMatch()
    {
        var interpreted = new CsEvalEngine(CsEvalOptions.Default with
        {
            LanguageMode = LanguageMode.Extended
        });

        var compiled = new CsEvalEngine(CsEvalOptions.Default.UseCompiler() with
        {
            LanguageMode = LanguageMode.Extended
        });

        var interpretedTrace = interpreted.EvaluateWithTrace("4 * 5 + 2");
        var compiledTrace = compiled.EvaluateWithTrace("4 * 5 + 2");

        Assert.That(compiledTrace.Result, Is.EqualTo(interpretedTrace.Result));
        Assert.That(
            compiledTrace.Steps.Select(step => step.NodeKind),
            Is.EqualTo(interpretedTrace.Steps.Select(step => step.NodeKind)));
        Assert.That(
            compiledTrace.Steps.Select(step => step.Value?.ToString()),
            Is.EqualTo(interpretedTrace.Steps.Select(step => step.Value?.ToString())));
    }

    [TestCaseSource(typeof(BinderParityFixture), nameof(BinderParityFixture.StandardScenarios))]
    public void StandardMode_InterpretedAndCompiled_ShouldMatch(ExecutionParityScenario scenario)
    {
        var interpreted = BinderParityFixture.CreateEngine(
            CompilationMode.Interpreted,
            LanguageMode.Standard,
            scenario);
        var compiled = BinderParityFixture.CreateEngine(
            CompilationMode.Compiled,
            LanguageMode.Standard,
            scenario);

        var interpretedResult = interpreted.Evaluate(scenario.Expression, scenario.CloneVariables());
        var compiledResult = EvaluateCompiledWithoutFallback(compiled, scenario);

        Assert.That(compiledResult, Is.EqualTo(interpretedResult), scenario.Expression);
    }

    [TestCaseSource(typeof(BinderParityFixture), nameof(BinderParityFixture.ExtendedScenarios))]
    public void ExtendedMode_InterpretedAndCompiled_ShouldMatch(ExecutionParityScenario scenario)
    {
        var interpreted = BinderParityFixture.CreateEngine(
            CompilationMode.Interpreted,
            LanguageMode.Extended,
            scenario);
        var compiled = BinderParityFixture.CreateEngine(
            CompilationMode.Compiled,
            LanguageMode.Extended,
            scenario);

        var interpretedResult = interpreted.Evaluate(scenario.Expression, scenario.CloneVariables());
        var compiledResult = EvaluateCompiledWithoutFallback(compiled, scenario);

        Assert.That(compiledResult, Is.EqualTo(interpretedResult), scenario.Expression);
    }

    private static object? EvaluateCompiledWithoutFallback(CsEvalEngine engine, ExecutionParityScenario scenario)
    {
        var expression = engine.Parse(scenario.Expression);
        var result = engine.Evaluate(expression, scenario.CloneVariables());
        Assert.That(expression.IsCompiled, Is.True, $"Compiled mode must produce IL delegate for scenario '{scenario.Name}'.");
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0), $"Compiled mode must not use bound fallback for scenario '{scenario.Name}'.");
        return result;
    }
}
