namespace CsEval.Test;

[TestFixture]
public sealed class ExecutionModeParityTests
{
    [TestCaseSource(typeof(BinderParityFixture), nameof(BinderParityFixture.StandardScenarios))]
    public void StandardMode_InterpretedAndStrictCompiled_ShouldMatch(ExecutionParityScenario scenario)
    {
        var interpreted = BinderParityFixture.CreateEngine(
            CompilationMode.Interpreted,
            LanguageMode.Standard,
            scenario);
        var compiled = BinderParityFixture.CreateEngine(
            CompilationMode.StrictCompiled,
            LanguageMode.Standard,
            scenario);

        var interpretedResult = interpreted.Evaluate(scenario.Expression, scenario.CloneVariables());
        var compiledResult = compiled.Evaluate(scenario.Expression, scenario.CloneVariables());

        Assert.That(compiledResult, Is.EqualTo(interpretedResult), scenario.Expression);
    }

    [TestCaseSource(typeof(BinderParityFixture), nameof(BinderParityFixture.ExtendedScenarios))]
    public void ExtendedMode_InterpretedAndStrictCompiled_ShouldMatch(ExecutionParityScenario scenario)
    {
        var interpreted = BinderParityFixture.CreateEngine(
            CompilationMode.Interpreted,
            LanguageMode.Extended,
            scenario);
        var compiled = BinderParityFixture.CreateEngine(
            CompilationMode.StrictCompiled,
            LanguageMode.Extended,
            scenario);

        var interpretedResult = interpreted.Evaluate(scenario.Expression, scenario.CloneVariables());
        var compiledResult = compiled.Evaluate(scenario.Expression, scenario.CloneVariables());

        Assert.That(compiledResult, Is.EqualTo(interpretedResult), scenario.Expression);
    }
}
