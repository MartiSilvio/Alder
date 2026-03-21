using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LinqBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private ScriptRunner<object> _roslynRunner = null!;
    private AlderExpression _interpretedExpression = null!;
    private AlderExpression _compiledExpression = null!;
    private AlderExpression _compiledFecExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public LinqScenario Scenario { get; set; } = null!;

    public IEnumerable<LinqScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetLinqScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.AlderExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.AlderExpression);
        _compiledFecExpression = CompiledFecEngine.Parse(Scenario.AlderExpression);

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
    public object Alder_Interpreted() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("LinqExecution")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("LinqExecution")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;
}
