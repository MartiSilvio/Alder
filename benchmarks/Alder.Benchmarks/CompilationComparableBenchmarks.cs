using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DynamicExpresso;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.Scripting;
using NCalc;
using FleeExpressionContext = Flee.PublicTypes.ExpressionContext;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompilationComparableBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private Interpreter _dynamicExpressoInterpreter = null!;
    private FleeExpressionContext _fleeContext = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public CompilationScenario Scenario { get; set; } = null!;

    public IEnumerable<CompilationScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetCompilationScenarios();

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _dynamicExpressoInterpreter = CreateDynamicExpressoInterpreter(_globals);
        _fleeContext = CreateFleeContext(_globals);

        var parity = BenchmarkParityVerifier.VerifyCompilationScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Compilation")]
    public AlderExpression Alder_Interpreted_Parse() => InterpretedEngine.Parse(Scenario.AlderExpression);

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public AlderExpression Alder_Compiled_Parse() => CompiledEngine.Parse(Scenario.AlderExpression);

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public AlderExpression Alder_CompiledFec_Parse() => CompiledFecEngine.Parse(Scenario.AlderExpression);

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public Script<object> Roslyn_Script_Compile()
    {
        var script = CreateRoslynScript(Scenario.RoslynExpression);
        script.Compile();
        return script;
    }

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public Expression NCalc_Parse() => new(Scenario.NCalcExpression);

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public Lambda DynamicExpresso_Parse() => _dynamicExpressoInterpreter.Parse(Scenario.DynamicExpressoExpression);

    [Benchmark]
    [BenchmarkCategory("Compilation")]
    public IDynamicExpression Flee_Compile() => _fleeContext.CompileDynamic(Scenario.FleeExpression);
}
