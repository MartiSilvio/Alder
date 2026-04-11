using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;

namespace Alder.Benchmarks;

/// <summary>
/// Measures collection-processing workloads over realistic product data.
/// The scale factor controls dataset size, and setup validates parity before any timings are recorded.
/// </summary>
public sealed record PipelineQuery(
    string Name,
    string AlderExpr,
    string RoslynExpr,
    Func<List<Product>, object?> Native)
{
    public override string ToString() => Name;
}

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CollectionPipelineBenchmarks : BenchmarkBase
{
    [Params(100, 1_000, 10_000, 100_000)]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public PipelineQuery Query { get; set; } = null!;

    public IEnumerable<PipelineQuery> Queries() => GetPipelineQueries();

    private BenchmarkData _data = null!;
    private ScriptRunner<object> _roslynRunner = null!;
    private AlderExpression _interpExpr = null!;
    private AlderExpression _compExpr = null!;
    private AlderExpression _fecExpr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.Create(productCount: ScaleFactor);

        InterpretedEngine = CreateEngine(CompilationMode.Interpreted, _data);
        CompiledEngine = CreateEngine(CompilationMode.Compiled, _data);
        CompiledFecEngine = CreateEngine(CompilationMode.CompiledFec, _data);

        _interpExpr = InterpretedEngine.Parse(Query.AlderExpr);
        _compExpr = CompiledEngine.Parse(Query.AlderExpr);
        _fecExpr = CompiledFecEngine.Parse(Query.AlderExpr);

        var script = CreateRoslynScript(Query.RoslynExpr);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        // Operational pipeline results are only worth publishing if the compared implementations stay semantically aligned.
        var native = Query.Native(_data.Products);
        var interp = InterpretedEngine.Evaluate(_interpExpr);
        var comp = CompiledEngine.Evaluate(_compExpr);
        var fec = CompiledFecEngine.Evaluate(_fecExpr);
        var roslyn = _roslynRunner(_data).GetAwaiter().GetResult();
        if (!BenchmarkParityVerifier.AreEquivalent(native, interp))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, Interpreted={interp}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, comp))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, Compiled={comp}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, fec))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, CompiledFec={fec}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, roslyn))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, Roslyn={roslyn}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine?.Dispose();
        CompiledEngine?.Dispose();
        CompiledFecEngine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/CollectionPipeline")]
    public object Native() => Query.Native(_data.Products)!;

    [Benchmark]
    [BenchmarkCategory("Operational/CollectionPipeline")]
    public object Alder_Interpreted() => InterpretedEngine.Evaluate(_interpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Operational/CollectionPipeline")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compExpr)!;

    [Benchmark]
    [BenchmarkCategory("Operational/CollectionPipeline")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_fecExpr)!;

    [Benchmark]
    [BenchmarkCategory("Operational/CollectionPipeline")]
    public async Task<object> Roslyn() => (await _roslynRunner(_data))!;

    public static IReadOnlyList<PipelineQuery> GetPipelineQueries() =>
    [
        new("Filter+Count",
            "products.Where(p => p.Price > 100m && p.IsActive).Count()",
            "Products.Where(p => p.Price > 100m && p.IsActive).Count()",
            ps => ps.Where(p => p.Price > 100m && p.IsActive).Count()),

        new("Filter+Project+Sum",
            """products.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum()""",
            """Products.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum()""",
            ps => ps.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum()),

        new("ComplexPredicate",
            "products.Where(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive).Count()",
            "Products.Where(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive).Count()",
            ps => ps.Where(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive).Count()),

        new("Sort+Take",
            "products.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum()",
            "Products.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum()",
            ps => ps.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum()),

        new("Distinct+Count",
            "products.Where(p => p.IsActive).Select(p => p.Category).Distinct().Count()",
            "Products.Where(p => p.IsActive).Select(p => p.Category).Distinct().Count()",
            ps => ps.Where(p => p.IsActive).Select(p => p.Category).Distinct().Count()),

        new("Any+Complex",
            "products.Any(p => p.Price > 900m && p.Stock > 500)",
            "Products.Any(p => p.Price > 900m && p.Stock > 500)",
            ps => ps.Any(p => p.Price > 900m && p.Stock > 500)),
    ];
}
