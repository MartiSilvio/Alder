using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Compilation break-even analysis for reusable expressions. This class answers
/// when the upfront compile cost is worth paying for repeated execution.
///
/// Each benchmark invocation creates a FRESH engine, parses the expression,
/// optionally compiles it, then evaluates it N times. This measures the
/// total cost of ownership (setup + N evaluations) — not just warm-path speed.
///
/// If Compiled total &lt; Interpreted total at N=K, the break-even point is K.
/// Results tell users: "if you'll evaluate this expression fewer than K times,
/// skip compilation."
///
/// Two expression categories:
///   Scalar — lightweight, dispatch-overhead-dominant
///   LINQ   — work-dominant, compilation wins earlier
/// </summary>
[Config(typeof(MonitoringConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompilationAmortizationBenchmarks
{
    [Params(1, 5, 10, 50, 100, 500, 1_000)]
    public int ReuseCount { get; set; }

    private const string ScalarExpr = "Math.Abs(x - y) + Math.Max(y, z) * 2";
    private const string LinqExpr = "products.Where(p => p.Price > 100m && p.IsActive).Count()";

    private BenchmarkData _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
    }

    #region Scalar break-even

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

    #endregion

    #region LINQ break-even

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

    [Benchmark]
    [BenchmarkCategory("Operational/CompilationAmortization/Linq")]
    public object CompiledFec_LINQ()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
        BenchmarkBase.ApplyVariables(engine, _data);
        var expr = engine.Parse(LinqExpr);
        object? result = null;
        for (int i = 0; i < ReuseCount; i++)
            result = engine.Evaluate(expr);
        return result!;
    }

    #endregion
}
