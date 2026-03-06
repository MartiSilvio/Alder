using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AdvancedLanguageBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private ScriptRunner<object> _roslynRunner = null!;
    private CsEvalExpression _interpretedExpression = null!;
    private CsEvalExpression _compiledExpression = null!;
    private Lambda _dynamicExpressoExpression = null!;
    private IDynamicExpression _fleeExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public AdvancedScenario Scenario { get; set; } = null!;

    public IEnumerable<AdvancedScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetAdvancedLanguageScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.CsEvalExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.CsEvalExpression);

        var script = CreateRoslynScript(Scenario.RoslynExpression);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        var dynamicExpressoInterpreter = CreateDynamicExpressoInterpreter(_globals);
        _dynamicExpressoExpression = dynamicExpressoInterpreter.Parse(Scenario.DynamicExpressoExpression);

        var fleeContext = CreateFleeContext(_globals);
        _fleeExpression = fleeContext.CompileDynamic(Scenario.FleeExpression);

        var parity = BenchmarkParityVerifier.VerifyAdvancedScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AdvancedLanguage")]
    public object Roslyn_ScriptCompiledRunner() => _roslynRunner(_globals).GetAwaiter().GetResult()!;

    [Benchmark]
    [BenchmarkCategory("AdvancedLanguage")]
    public object CsEval_Interpreted() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("AdvancedLanguage")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("AdvancedLanguage")]
    public object DynamicExpresso() => _dynamicExpressoExpression.Invoke()!;

    [Benchmark]
    [BenchmarkCategory("AdvancedLanguage")]
    public object Flee() => _fleeExpression.Evaluate()!;
}
