using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

/// <summary>
/// Measures concurrent throughput for a shared reusable engine under a fixed
/// operation budget. This reflects server-style request fan-out, not single-
/// expression microbenchmarking.
///
/// Fixed total operations (10,000) distributed across [1, 2, 4, 8] threads.
/// If the engine scales linearly, doubling threads halves wall-clock time.
/// Contention or lock overhead shows up as sub-linear scaling.
///
/// Tests both a simple expression (dispatch overhead dominant)
/// and a LINQ pipeline (work-dominant, should scale near-linearly).
/// </summary>
[Config(typeof(MonitoringConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ThroughputBenchmarks : BenchmarkBase
{
    [Params(1, 2, 4, 8)]
    public int ThreadCount { get; set; }

    private const int TotalOperations = 10_000;

    private BenchmarkData _data = null!;

    private AlderEngine _interpEngine = null!;
    private AlderEngine _compEngine = null!;
    private AlderEngine _fecEngine = null!;
    private AlderExpression _interpScalar = null!;
    private AlderExpression _compScalar = null!;
    private AlderExpression _fecScalar = null!;
    private AlderExpression _interpLinq = null!;
    private AlderExpression _compLinq = null!;
    private AlderExpression _fecLinq = null!;

    private const string ScalarExpr = "Math.Abs(10 - 3) + Math.Max(3, 7)";
    private const string LinqExpr = "numbers.Where(x => x > 500).Count()";

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();

        _interpEngine = CreateEngine(CompilationMode.Interpreted, _data);
        _compEngine = CreateEngine(CompilationMode.Compiled, _data);
        _fecEngine = CreateEngine(CompilationMode.CompiledFec, _data);

        _interpScalar = _interpEngine.Parse(ScalarExpr);
        _compScalar = _compEngine.Parse(ScalarExpr);
        _fecScalar = _fecEngine.Parse(ScalarExpr);

        _interpLinq = _interpEngine.Parse(LinqExpr);
        _compLinq = _compEngine.Parse(LinqExpr);
        _fecLinq = _fecEngine.Parse(LinqExpr);

        _interpEngine.Evaluate(_interpScalar);
        _compEngine.Evaluate(_compScalar);
        _fecEngine.Evaluate(_fecScalar);
        _interpEngine.Evaluate(_interpLinq);
        _compEngine.Evaluate(_compLinq);
        _fecEngine.Evaluate(_fecLinq);

        var expectedScalar = _interpEngine.Evaluate(_interpScalar);
        var compiledScalar = _compEngine.Evaluate(_compScalar);
        var fecScalar = _fecEngine.Evaluate(_fecScalar);
        if (!BenchmarkParityVerifier.AreEquivalent(expectedScalar, compiledScalar)
            || !BenchmarkParityVerifier.AreEquivalent(expectedScalar, fecScalar))
            throw new InvalidOperationException(
                $"Throughput scalar parity failure: interpreted={expectedScalar}, compiled={compiledScalar}, fec={fecScalar}");

        var expectedLinq = _interpEngine.Evaluate(_interpLinq);
        var compiledLinq = _compEngine.Evaluate(_compLinq);
        var fecLinq = _fecEngine.Evaluate(_fecLinq);
        if (!BenchmarkParityVerifier.AreEquivalent(expectedLinq, compiledLinq)
            || !BenchmarkParityVerifier.AreEquivalent(expectedLinq, fecLinq))
            throw new InvalidOperationException(
                $"Throughput LINQ parity failure: interpreted={expectedLinq}, compiled={compiledLinq}, fec={fecLinq}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _interpEngine?.Dispose();
        _compEngine?.Dispose();
        _fecEngine?.Dispose();
    }

    private object RunParallel(AlderEngine engine, AlderExpression expr)
    {
        object? last = null;
        Parallel.For(0, TotalOperations,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => last = engine.Evaluate(expr));
        return last!;
    }

    #region Scalar expression throughput

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Scalar")]
    public object Interpreted_Scalar() => RunParallel(_interpEngine, _interpScalar);

    [Benchmark]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Scalar")]
    public object Compiled_Scalar() => RunParallel(_compEngine, _compScalar);

    [Benchmark]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Scalar")]
    public object CompiledFec_Scalar() => RunParallel(_fecEngine, _fecScalar);

    #endregion

    #region LINQ expression throughput

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Linq")]
    public object Interpreted_LINQ() => RunParallel(_interpEngine, _interpLinq);

    [Benchmark]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Linq")]
    public object Compiled_LINQ() => RunParallel(_compEngine, _compLinq);

    [Benchmark]
    [BenchmarkCategory("Operational/ConcurrentThroughput/Linq")]
    public object CompiledFec_LINQ() => RunParallel(_fecEngine, _fecLinq);

    #endregion
}
