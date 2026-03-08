using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LinqBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private ScriptRunner<object> _roslynRunner = null!;
    private CsEvalExpression _interpretedExpression = null!;
    private CsEvalExpression _compiledExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public LinqScenario Scenario { get; set; } = null!;

    public IEnumerable<LinqScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetLinqScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.CsEvalExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.CsEvalExpression);

        var script = CreateRoslynScript(Scenario.RoslynExpression);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        var parity = BenchmarkParityVerifier.VerifyLinqScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("LinqExecution")]
    public object NativeDelegate_Baseline() => Scenario.NativeEvaluator(_globals)!;

    [Benchmark]
    [BenchmarkCategory("LinqExecution")]
    public object Roslyn_ScriptCompiledRunner() => _roslynRunner(_globals).GetAwaiter().GetResult()!;

    [Benchmark]
    [BenchmarkCategory("LinqExecution")]
    public object CsEval_Interpreted() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("LinqExecution")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;
}
