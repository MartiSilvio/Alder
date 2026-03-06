using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using Flee.PublicTypes;
using NCalc;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ColdStartComparableBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();

    [ParamsSource(nameof(ScenarioSource))]
    public ComparableScenario Scenario { get; set; } = null!;

    public IEnumerable<ComparableScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetComparableExecutionScenarios();

    [GlobalSetup]
    public void Setup()
    {
        var parity = BenchmarkParityVerifier.VerifyComparableScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ColdStart")]
    public object CsEval_Interpreted_Cold()
    {
        using var engine = CreateEngine(CompilationMode.Interpreted, _globals);
        return engine.Evaluate(Scenario.CsEvalExpression)!;
    }

    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public object CsEval_Compiled_Cold()
    {
        using var engine = CreateEngine(CompilationMode.StrictCompiled, _globals);
        return engine.Evaluate(Scenario.CsEvalExpression)!;
    }

    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public object Roslyn_Scripting_EvaluateAsync_Cold()
    {
        return EvaluateRoslynAsync(Scenario.RoslynExpression, _globals).GetAwaiter().GetResult()!;
    }

    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public object NCalc_Cold()
    {
        var expression = new Expression(Scenario.NCalcExpression);
        BenchmarkParityVerifier.ApplyNCalcParameters(expression, _globals);
        return expression.Evaluate()!;
    }

    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public object DynamicExpresso_Cold()
    {
        var interpreter = CreateDynamicExpressoInterpreter(_globals);
        Lambda parsed = interpreter.Parse(Scenario.DynamicExpressoExpression);
        return parsed.Invoke()!;
    }

    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public object Flee_Cold()
    {
        var context = CreateFleeContext(_globals);
        IDynamicExpression expression = context.CompileDynamic(Scenario.FleeExpression);
        return expression.Evaluate()!;
    }
}
