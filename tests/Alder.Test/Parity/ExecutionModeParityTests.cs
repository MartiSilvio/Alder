using System.Reflection;
using Alder.Test._Infrastructure;

namespace Alder.Test.Parity;

[TestFixture]
public sealed class ExecutionModeParityTests
{
    [Test]
    public void Trace_InterpretedAndCompiled_ShouldMatch()
    {
        var interpreted = new AlderEngine(new AlderOptions
        {
            LanguageMode = LanguageMode.Extended
        });

        var compiled = new AlderEngine(new AlderOptions { LanguageMode = LanguageMode.Extended }.UseCompiler());

        var interpretedTrace = interpreted.EvaluateWithTrace("4 * 5 + 2");
        var compiledTrace = compiled.EvaluateWithTrace("4 * 5 + 2");

        Assert.That(compiledTrace.Result, Is.EqualTo(interpretedTrace.Result));
        AssertTreesMatch(interpretedTrace.Tree, compiledTrace.Tree);
    }

    private static void AssertTreesMatch(Tracing.TraceNode expected, Tracing.TraceNode actual)
    {
        Assert.That(actual.NodeKind, Is.EqualTo(expected.NodeKind));
        Assert.That(actual.Value?.ToString(), Is.EqualTo(expected.Value?.ToString()));
        Assert.That(actual.Children.Count, Is.EqualTo(expected.Children.Count));
        for (var i = 0; i < expected.Children.Count; i++)
            AssertTreesMatch(expected.Children[i], actual.Children[i]);
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

    [Test]
    public void InterpretedMode_DoesNotExecute_PrecompiledDelegate()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        var expression = engine.Parse("1 + 1");

        var fakeCompiled = new CompiledExpressionInfo(
            Delegate: (_, _, _, _) => 999,
            IsCompilable: true,
            FailureReason: null);

        var field = typeof(AlderExpression).GetField("CompiledInfo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(field, Is.Not.Null);
        field!.SetValue(expression, fakeCompiled);

        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(2));
    }

    private static object? EvaluateCompiledWithoutFallback(AlderEngine engine, ExecutionParityScenario scenario)
    {
        var expression = engine.Parse(scenario.Expression);
        var result = engine.Evaluate(expression, scenario.CloneVariables());
        Assert.That(expression.IsCompiled, Is.True, $"Compiled mode must produce IL delegate for scenario '{scenario.Name}'.");
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0), $"Compiled mode must not use bound fallback for scenario '{scenario.Name}'.");
        return result;
    }
}
