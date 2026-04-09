using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqBenchmarks
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();

    [ParamsSource(nameof(ScenarioSource))]
    public DynamicLinqScenario Scenario { get; set; } = null!;

    public IEnumerable<DynamicLinqScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetDynamicLinqScenarios();

    [GlobalSetup]
    public void Setup()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        var parity = BenchmarkParityVerifier.VerifyDynamicLinqScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DynamicLinqExecution")]
    public object NativeDelegate_Baseline() => Scenario.NativeEvaluator(_globals)!;

    [Benchmark]
    [BenchmarkCategory("DynamicLinqExecution")]
    public object Alder_DynamicLinq() => Scenario.AlderEvaluator(_globals)!;

    [Benchmark]
    [BenchmarkCategory("DynamicLinqExecution")]
    public object SystemLinqDynamicCore() => Scenario.DynamicLinqCoreEvaluator(_globals)!;
}
