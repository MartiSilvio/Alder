using DynamicExpresso;
using Flee.PublicTypes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;
using NCalc;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComparableExecutionBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private ScriptRunner<object> _roslynRunner = null!;
    private AlderEngine _interpretedReflectionEngine = null!;
    private AlderExpression _interpretedReflectionExpression = null!;
    private AlderExpression _interpretedExpression = null!;
    private AlderExpression _compiledExpression = null!;
    private AlderExpression _compiledFecExpression = null!;
    private Expression _ncalcExpression = null!;
    private Lambda _dynamicExpressoExpression = null!;
    private IDynamicExpression _fleeExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public ComparableScenario Scenario { get; set; } = null!;

    public IEnumerable<ComparableScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetComparableExecutionScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.AlderExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.AlderExpression);
        _compiledFecExpression = CompiledFecEngine.Parse(Scenario.AlderExpression);

        _interpretedReflectionEngine = new AlderEngine(o => o.Aot.ClearBuiltInContext());
        ApplyGlobals(_interpretedReflectionEngine, _globals);
        _interpretedReflectionExpression = _interpretedReflectionEngine.Parse(Scenario.AlderExpression);

        var script = CreateRoslynScript(Scenario.RoslynExpression);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        _ncalcExpression = new Expression(Scenario.NCalcExpression);
        BenchmarkParityVerifier.ApplyNCalcParameters(_ncalcExpression, _globals);

        var dynamicExpressoInterpreter = CreateDynamicExpressoInterpreter(_globals);
        _dynamicExpressoExpression = dynamicExpressoInterpreter.Parse(Scenario.DynamicExpressoExpression);

        var fleeContext = CreateFleeContext(_globals);
        _fleeExpression = fleeContext.CompileDynamic(Scenario.FleeExpression);

        var parity = BenchmarkParityVerifier.VerifyComparableScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WarmExecution")]
    public object NativeDelegate_Baseline() => Scenario.NativeEvaluator(_globals)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Interpreted_Reflection() => _interpretedReflectionEngine.Evaluate(_interpretedReflectionExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Interpreted_Generated() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Roslyn_ScriptCompiledRunner() => _roslynRunner(_globals).GetAwaiter().GetResult()!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object NCalc() => _ncalcExpression.Evaluate()!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object DynamicExpresso() => _dynamicExpressoExpression.Invoke()!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Flee() => _fleeExpression.Evaluate()!;
}
