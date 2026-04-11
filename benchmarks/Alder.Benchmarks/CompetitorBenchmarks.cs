using DynamicExpresso;
using Flee.PublicTypes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;
using NCalc;

namespace Alder.Benchmarks;

/// <summary>
/// Measures warm execution on workloads that every compared engine can express and evaluate with verified parity.
/// Setup performs each engine's required preparation so the benchmark samples steady execution rather than setup cost.
/// </summary>
[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompetitorBenchmarks : BenchmarkBase
{
    private readonly BenchmarkData _data = BenchmarkData.CreateStandard();
    private ScriptRunner<object> _roslynRunner = null!;
    private AlderExpression _interpretedExpr = null!;
    private AlderExpression _compiledExpr = null!;
    private AlderExpression _compiledFecExpr = null!;
    private Expression _ncalcExpr = null!;
    private Lambda _dExpressoExpr = null!;
    private IDynamicExpression _fleeExpr = null!;

    [ParamsSource(nameof(Scenarios))]
    public CompetitorScenario Scenario { get; set; } = null!;

    public IEnumerable<CompetitorScenario> Scenarios() => BenchmarkScenarios.GetCompetitorScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_data);
        _interpretedExpr = InterpretedEngine.Parse(Scenario.AlderExpr);
        _compiledExpr = CompiledEngine.Parse(Scenario.AlderExpr);
        _compiledFecExpr = CompiledFecEngine.Parse(Scenario.AlderExpr);

        var script = CreateRoslynScript(Scenario.RoslynExpr);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        _ncalcExpr = new Expression(Scenario.NCalcExpr);
        BenchmarkParityVerifier.ApplyNCalcParameters(_ncalcExpr, _data);

        var dExpressoInterpreter = CreateDynamicExpressoInterpreter(_data);
        _dExpressoExpr = dExpressoInterpreter.Parse(Scenario.DExpressoExpr);

        var fleeContext = CreateFleeContext(_data);
        _fleeExpr = fleeContext.CompileDynamic(Scenario.FleeExpr);

        // Head-to-head results are only meaningful when every engine produces the same answer for the selected scenario.
        var parity = BenchmarkParityVerifier.VerifyCompetitorScenario(Scenario, _data);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine?.Dispose();
        CompiledEngine?.Dispose();
        CompiledFecEngine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object Native() => Scenario.Native(_data)!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object Alder_Interpreted() => InterpretedEngine.Evaluate(_interpretedExpr)!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compiledExpr)!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpr)!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public async Task<object> Roslyn() => (await _roslynRunner(_data))!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object NCalc() => _ncalcExpr.Evaluate()!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object DynamicExpresso() => _dExpressoExpr.Invoke()!;

    [Benchmark]
    [BenchmarkCategory("HeadToHead/WarmExecution")]
    public object Flee() => _fleeExpr.Evaluate()!;
}
