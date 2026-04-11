using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DynamicExpresso;
using Flee.PublicTypes;
using NCalc;

namespace Alder.Benchmarks;

/// <summary>
/// Measures end-to-end first-use cost on workloads that every compared engine can express.
/// This suite is intended for short-lived process scenarios and must not be used to argue steady-state throughput.
/// There is intentionally no <c>GlobalSetup</c>, because <see cref="ColdStartConfig"/> relies on fresh processes
/// to keep startup work inside the measured sample.
/// </summary>
[Config(typeof(ColdStartConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ColdStartBenchmarks : BenchmarkBase
{
    private readonly BenchmarkData _data = BenchmarkData.CreateStandard();

    [ParamsSource(nameof(Scenarios))]
    public CompetitorScenario Scenario { get; set; } = null!;

    public IEnumerable<CompetitorScenario> Scenarios() => BenchmarkScenarios.GetCompetitorScenarios();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object Alder_Interpreted()
    {
        using var engine = CreateEngine(CompilationMode.Interpreted, _data);
        return engine.Evaluate(Scenario.AlderExpr)!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object Alder_Compiled()
    {
        using var engine = CreateEngine(CompilationMode.Compiled, _data);
        return engine.Evaluate(Scenario.AlderExpr)!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object Alder_CompiledFec()
    {
        using var engine = CreateEngine(CompilationMode.CompiledFec, _data);
        return engine.Evaluate(Scenario.AlderExpr)!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public async Task<object> Roslyn()
    {
        return (await EvaluateRoslynAsync(Scenario.RoslynExpr, _data))!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object NCalc_Cold()
    {
        var expression = new Expression(Scenario.NCalcExpr);
        BenchmarkParityVerifier.ApplyNCalcParameters(expression, _data);
        return expression.Evaluate()!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object DynamicExpresso_Cold()
    {
        var interpreter = CreateDynamicExpressoInterpreter(_data);
        Lambda parsed = interpreter.Parse(Scenario.DExpressoExpr);
        return parsed.Invoke()!;
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/ColdStart")]
    public object Flee_Cold()
    {
        var context = CreateFleeContext(_data);
        IDynamicExpression expression = context.CompileDynamic(Scenario.FleeExpr);
        return expression.Evaluate()!;
    }
}
