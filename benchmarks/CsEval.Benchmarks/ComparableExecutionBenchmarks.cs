using DynamicExpresso;
using Flee.PublicTypes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;
using NCalc;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComparableExecutionBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private ScriptRunner<object> _roslynRunner = null!;
    private CsEvalEngine _interpretedReflectionEngine = null!;
    private CsEvalExpression _interpretedReflectionExpression = null!;
    private CsEvalExpression _interpretedExpression = null!;
    private CsEvalExpression _compiledExpression = null!;
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
        _interpretedExpression = InterpretedEngine.Parse(Scenario.CsEvalExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.CsEvalExpression);

        _interpretedReflectionEngine = new CsEvalEngine();
        _interpretedReflectionEngine.ClearGeneratedContexts();
        ApplyGlobals(_interpretedReflectionEngine, _globals);
        _interpretedReflectionExpression = _interpretedReflectionEngine.Parse(Scenario.CsEvalExpression);

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
    public object CsEval_Interpreted_Reflection() => _interpretedReflectionEngine.Evaluate(_interpretedReflectionExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object CsEval_Interpreted_Generated() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

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
