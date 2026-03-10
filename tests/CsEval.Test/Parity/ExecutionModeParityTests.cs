namespace CsEval.Test;

[TestFixture]
public sealed class ExecutionModeParityTests
{
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
