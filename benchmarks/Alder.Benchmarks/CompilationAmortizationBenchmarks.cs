using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Measures when paying the upfront compilation cost becomes cheaper than repeated interpreted evaluation.
/// Each benchmark invocation creates a fresh engine, parses the expression, optionally compiles it,
/// and then evaluates it <see cref="ReuseCount"/> times. The reported cost is therefore total ownership cost
/// for a reusable expression, not isolated warm-path execution.
/// </summary>
[Config(typeof(MonitoringConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompilationAmortizationBenchmarks
{
    [ParamsSource(nameof(ReuseCounts))]
    public int ReuseCount { get; set; }

    public IEnumerable<int> ReuseCounts() =>
        BenchmarkProfileContext.CurrentDefinition.CompilationReuseCounts;

    private const string ScalarExpr = "Math.Abs(x - y) + Math.Max(y, z) * 2";
    private const string LinqExpr = "products.Where(p => p.Price > 100m && p.IsActive).Count()";

    private BenchmarkData _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/CompilationAmortization/Scalar")]
    public object Interpreted_Scalar()
    {
        using var engine = new AlderEngine();
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(ScalarExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/CompilationAmortization/Scalar")]
    public object Compiled_Scalar()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(ScalarExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/CompilationAmortization/Scalar")]
    public object CompiledFec_Scalar()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(ScalarExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/CompilationAmortization/Linq")]
    public object Interpreted_LINQ()
    {
        using var engine = new AlderEngine();
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(LinqExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/CompilationAmortization/Linq")]
    public object Compiled_LINQ()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(LinqExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

}
