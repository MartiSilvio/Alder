using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DynamicExpresso;
using NCalc;
using FleeExpressionContext = Flee.PublicTypes.ExpressionContext;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompetitorLifecycleBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Lifecycle")]
    public int Alder_CreateInterpretedEngine()
    {
        using var engine = CreateEngine(CompilationMode.Interpreted, _globals);
        return engine.GetHashCode();
    }

    [Benchmark]
    [BenchmarkCategory("Lifecycle")]
    public int Alder_CreateCompiledEngine()
    {
        using var engine = CreateEngine(CompilationMode.Compiled, _globals);
        return engine.GetHashCode();
    }

    [Benchmark]
    [BenchmarkCategory("Lifecycle")]
    public Interpreter DynamicExpresso_CreateInterpreter() =>
        CreateDynamicExpressoInterpreter(_globals);

    [Benchmark]
    [BenchmarkCategory("Lifecycle")]
    public FleeExpressionContext Flee_CreateContext() =>
        CreateFleeContext(_globals);

    [Benchmark]
    [BenchmarkCategory("Lifecycle")]
    public Expression NCalc_CreateExpression() =>
        new("(1 + x == 5 + y) == (42 == value)");
}
