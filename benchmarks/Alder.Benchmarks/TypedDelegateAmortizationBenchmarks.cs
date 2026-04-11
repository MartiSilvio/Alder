using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Measures when typed-delegate compilation pays for itself relative to repeated engine evaluation.
/// <see cref="MonitoringConfig"/> is used because each benchmark method owns the reuse loop and therefore represents the unit of work directly.
/// </summary>
[Config(typeof(MonitoringConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TypedDelegateAmortizationBenchmarks : BenchmarkBase
{
    [Params(1, 10, 100, 1_000)]
    public int ReuseCount { get; set; }

    private BenchmarkData _data = null!;

    private const string ScalarCode = "Math.Abs(a - b) + Math.Max(a, b) * 2";

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/TypedDelegateAmortization")]
    public int Amortize_EngineEvaluate()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        engine.SetVariable<int>("a", _data.X);
        engine.SetVariable<int>("b", _data.Y);
        var expr = engine.Parse(ScalarCode);
        var result = 0;
        for (int i = 0; i < ReuseCount; i++)
            result = (int)engine.Evaluate(expr)!;
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/TypedDelegateAmortization")]
    public int Amortize_TypedDelegate()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var fn = engine.Compile<Func<int, int, int>>(ScalarCode, "a", "b");
        var result = 0;
        for (int i = 0; i < ReuseCount; i++)
            result = fn(_data.X, _data.Y);
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/TypedDelegateAmortization")]
    public int Amortize_TypedDelegate_Fec()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
        var fn = engine.Compile<Func<int, int, int>>(ScalarCode, "a", "b");
        var result = 0;
        for (int i = 0; i < ReuseCount; i++)
            result = fn(_data.X, _data.Y);
        return result;
    }
}
