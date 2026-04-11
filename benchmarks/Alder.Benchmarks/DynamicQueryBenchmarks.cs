using System.Linq.Dynamic.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Measures dynamic string-query execution over large in-memory collections.
/// Each scenario includes the full query path used by the library under test.
/// </summary>
public sealed record DynamicQuery(
    string Name,
    Func<List<Product>, object?> Native,
    Func<List<Product>, AlderEngine, object?> Alder,
    Func<List<Product>, object?> DynLinqCore)
{
    public override string ToString() => Name;
}

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicQueryBenchmarks : BenchmarkBase
{
    [Params(100, 1_000, 10_000, 100_000)]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicQuery Query { get; set; } = null!;

    public IEnumerable<DynamicQuery> Queries() => GetDynamicQueries();

    private BenchmarkData _data = null!;
    private AlderEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.Create(productCount: ScaleFactor);
        _engine = new AlderEngine(new AlderOptions().UseCompiler());

        var native = Query.Native(_data.Products);
        var alder = Query.Alder(_data.Products, _engine);
        var dynLinq = Query.DynLinqCore(_data.Products);
        if (!BenchmarkParityVerifier.AreEquivalent(native, alder))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, Alder={alder}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, dynLinq))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, DynLinqCore={dynLinq}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/DynamicQuery")]
    public object Native() => Query.Native(_data.Products)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicQuery")]
    public object Alder_DynamicLinq() => Query.Alder(_data.Products, _engine)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicQuery")]
    public object SystemLinqDynamicCore() => Query.DynLinqCore(_data.Products)!;

    public static IReadOnlyList<DynamicQuery> GetDynamicQueries() =>
    [
        new("Filter+Count",
            ps => ps.Where(p => p.Price > 100m && p.IsActive).Count(),
            (ps, engine) => ps.WhereDynamic<Product>(engine, """p => p.Price > 100m && p.IsActive""").Count(),
            ps => ps.AsQueryable().Where("Price > 100 && IsActive").Count()),

        new("Filter+Project+Sum",
            ps => ps.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, """p => p.Category == "Electronics" """)
                .SelectDynamic<Product, decimal>(engine, "p => p.Price")
                .Sum(),
            ps => ps.AsQueryable().Where("Category == \"Electronics\"").Sum("Price")),

        new("ComplexPredicate",
            ps => ps.Where(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive).Count(),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, "p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive")
                .Count(),
            ps => ps.AsQueryable().Where("Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive").Count()),

        new("Sort+Take+Sum",
            ps => ps.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .OrderByDescendingDynamic<Product, decimal>(engine, "p => p.Price")
                .Take(10)
                .SelectDynamic<Product, decimal>(engine, "p => p.Price")
                .Sum(),
            ps => ps.AsQueryable().OrderBy("Price descending").Take(10).Sum("Price")),

        new("Any+Complex",
            ps => ps.Any(p => p.Price > 900m && p.Stock > 500),
            (ps, engine) => ps.AnyDynamic<Product>(engine, "p => p.Price > 900m && p.Stock > 500"),
            ps => ps.AsQueryable().Any("Price > 900 && Stock > 500")),

        new("MultiStage",
            ps => ps.Where(p => p.IsActive && p.Stock > 0).Where(p => p.Price > 25m).Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, "p => p.IsActive && p.Stock > 0")
                .WhereDynamic<Product>(engine, "p => p.Price > 25m")
                .SelectDynamic<Product, decimal>(engine, "p => p.Price")
                .Sum(),
            ps => ps.AsQueryable().Where("IsActive && Stock > 0").Where("Price > 25").Sum("Price")),
    ];
}
