using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;
using Alder.Compiled.DynamicLinq;

namespace Alder.Benchmarks;

/// <summary>
/// Measures dynamic string-query execution over large in-memory collections.
/// Each scenario includes the full query path used by the library under test.
/// </summary>
public sealed record DynamicLinqQuery(
    string Name,
    Func<IQueryable<Product>, object?> Native,
    Func<IQueryable<Product>, AlderEngine, object?> Alder,
    Func<IQueryable<Product>, AlderEngine, object?> AlderPreParsed,
    Func<IQueryable<Product>, object?> DynLinqCore)
{
    public override string ToString() => Name;
}

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqBenchmarks : BenchmarkBase
{
    [Params(100, 1_000, 10_000, 100_000)]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicLinqQuery Query { get; set; } = null!;

    public IEnumerable<DynamicLinqQuery> Queries() => GetDynamicLinqQueries();

    private BenchmarkData _data = null!;
    private IQueryable<Product> _productsQuery = null!;
    private AlderEngine _engine = null!;
    private Func<IQueryable<Product>, object?> _alderCached = null!;
    private Func<IQueryable<Product>, object?> _dynLinqCoreCached = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.Create(productCount: ScaleFactor);
        _productsQuery = _data.Products.AsQueryable();
        _engine = new AlderEngine(new AlderOptions().UseCompiler());
        _alderCached = CreateAlderCachedQuery(Query.Name, _engine);
        _dynLinqCoreCached = CreateDynamicCoreCachedQuery(Query.Name);

        var native = Query.Native(_productsQuery);
        var alder = Query.Alder(_productsQuery, _engine);
        var alderCached = _alderCached(_productsQuery);
        var dynLinq = Query.DynLinqCore(_productsQuery);
        var dynLinqCached = _dynLinqCoreCached(_productsQuery);
        if (!BenchmarkParityVerifier.AreEquivalent(native, alder))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, Alder={alder}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, alderCached))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, AlderPreParsed={alderCached}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, dynLinq))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, DynLinqCore={dynLinq}");
        if (!BenchmarkParityVerifier.AreEquivalent(native, dynLinqCached))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={native}, DynLinqCoreCached={dynLinqCached}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/DynamicLinq/Warm")]
    public object Native() => Query.Native(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Warm")]
    public object Alder_DynamicLinq() => Query.Alder(_productsQuery, _engine)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Warm")]
    public object SystemDynamicLinqCore() => Query.DynLinqCore(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object Alder_DynamicLinq_PreParsed() => _alderCached(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object SystemDynamicLinqCore_CachedLambda() => _dynLinqCoreCached(_productsQuery)!;

    private static Expression<Func<Product, TResult>> ParseProductLambda<TResult>(string expression) =>
        DynamicExpressionParser.ParseLambda<Product, TResult>(
            new ParsingConfig(),
            createParameterCtor: true,
            expression);

    private static Expression<Func<Product, bool>> ParseAlderPredicate(AlderEngine engine, string expression) =>
        (Expression<Func<Product, bool>>)engine.ParsePredicateExpression(typeof(Product), expression);

    private static Expression<Func<Product, TResult>> ParseAlderSelector<TResult>(AlderEngine engine, string expression) =>
        (Expression<Func<Product, TResult>>)engine.ParseSelectorExpression(typeof(Product), typeof(TResult), expression);

    private static Func<IQueryable<Product>, object?> CreateAlderCachedQuery(string queryName, AlderEngine engine) =>
        queryName switch
        {
            "Filter+Count" => BuildAlderFilterCountCached(engine),
            "Filter+Project+Sum" => BuildAlderFilterProjectSumCached(engine),
            "Projection+DistinctCategoryCount" => BuildAlderProjectionDistinctCategoryCountCached(engine),
            "Projection+Contains" => BuildAlderProjectionContainsCached(engine),
            "Projection+AnonymousMaterialization" => BuildAlderProjectionAnonymousMaterializationCached(engine),
            "SetOperator+UnionCount" => BuildAlderSetOperatorUnionCountCached(engine),
            "Projection+TypedDtoFirst" => BuildAlderProjectionTypedDtoFirstCached(engine),
            "Sequence+SequenceEqual" => BuildAlderSequenceEqualCached(engine),
            "ComplexPredicate" => BuildAlderComplexPredicateCached(engine),
            "Sort+Take+Sum" => BuildAlderSortTakeSumCached(engine),
            "Any+Complex" => BuildAlderAnyComplexCached(engine),
            "MultiStage" => BuildAlderMultiStageCached(engine),
            _ => throw new NotSupportedException($"No Alder cached variant for query '{queryName}'.")
        };

    private static Func<IQueryable<Product>, object?> BuildAlderFilterCountCached(AlderEngine engine)
    {
        var predicate = ParseAlderPredicate(engine, "Price > 100 && IsActive");
        return ps => ps.WhereDynamic(predicate).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildAlderFilterProjectSumCached(AlderEngine engine)
    {
        var predicate = ParseAlderPredicate(engine, """Category == "Electronics" """);
        var selector = ParseAlderSelector<decimal>(engine, "Price");
        return ps => ps.WhereDynamic(predicate).SumDynamic(selector);
    }

    private static Func<IQueryable<Product>, object?> BuildAlderProjectionDistinctCategoryCountCached(AlderEngine engine)
    {
        var selector = ParseAlderSelector<string>(engine, "Category");
        return ps => ps.SelectDynamic(selector).Distinct().Count();
    }

    private static Func<IQueryable<Product>, object?> BuildAlderProjectionContainsCached(AlderEngine engine)
    {
        var selector = ParseAlderSelector<string>(engine, "Category");
        return ps => ps.SelectDynamic(selector).ContainsDynamic("Electronics");
    }

    private static Func<IQueryable<Product>, object?> BuildAlderProjectionAnonymousMaterializationCached(AlderEngine engine)
    {
        var selector = ParseAlderSelector<object>(engine, "new { Category, Price }");
        return ps => ps.SelectDynamic(selector).Take(256).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildAlderSetOperatorUnionCountCached(AlderEngine engine)
    {
        var activePredicate = ParseAlderPredicate(engine, "IsActive");
        var expensivePredicate = ParseAlderPredicate(engine, "Price > 500");
        var selector = ParseAlderSelector<string>(engine, "Category");

        return ps =>
        {
            var active = ps.WhereDynamic(activePredicate).SelectDynamic(selector);
            var expensive = ps.WhereDynamic(expensivePredicate).SelectDynamic(selector);
            return active.UnionDynamic(expensive).Count();
        };
    }

    private static Func<IQueryable<Product>, object?> BuildAlderProjectionTypedDtoFirstCached(AlderEngine engine)
    {
        var selector = ParseAlderSelector<object>(engine, "new { Name, Price }");
        var keySelector = ParseAlderSelector<int>(engine, "Id");

        return ps =>
        {
            var projection = ps.OrderByDynamic(keySelector).SelectDynamic(selector).First();
            return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(projection)!.Price;
        };
    }

    private static Func<IQueryable<Product>, object?> BuildAlderSequenceEqualCached(AlderEngine engine)
    {
        var predicate = ParseAlderPredicate(engine, "IsActive");
        var categorySelector = ParseAlderSelector<string>(engine, "Category");
        var idSelector = ParseAlderSelector<int>(engine, "Id");

        return ps =>
        {
            var left = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).Take(128).SelectDynamic(categorySelector);
            var right = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).Take(128).SelectDynamic(categorySelector);
            return left.SequenceEqualDynamic(right);
        };
    }

    private static Func<IQueryable<Product>, object?> BuildAlderComplexPredicateCached(AlderEngine engine)
    {
        var predicate = ParseAlderPredicate(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive");
        return ps => ps.WhereDynamic(predicate).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildAlderSortTakeSumCached(AlderEngine engine)
    {
        var keySelector = ParseAlderSelector<decimal>(engine, "Price");
        var selector = ParseAlderSelector<decimal>(engine, "Price");
        return ps => ps.OrderByDescendingDynamic(keySelector).Take(10).SumDynamic(selector);
    }

    private static Func<IQueryable<Product>, object?> BuildAlderAnyComplexCached(AlderEngine engine)
    {
        var predicate = ParseAlderPredicate(engine, "Price > 900 && Stock > 500");
        return ps => ps.AnyDynamic(predicate);
    }

    private static Func<IQueryable<Product>, object?> BuildAlderMultiStageCached(AlderEngine engine)
    {
        var predicate1 = ParseAlderPredicate(engine, "IsActive && Stock > 0");
        var predicate2 = ParseAlderPredicate(engine, "Price > 25");
        var selector = ParseAlderSelector<decimal>(engine, "Price");
        return ps => ps.WhereDynamic(predicate1).WhereDynamic(predicate2).SumDynamic(selector);
    }

    private static Func<IQueryable<Product>, object?> CreateDynamicCoreCachedQuery(string queryName) =>
        queryName switch
        {
            "Filter+Count" => BuildFilterCountCached(),
            "Filter+Project+Sum" => BuildFilterProjectSumCached(),
            "Projection+DistinctCategoryCount" => BuildProjectionDistinctCategoryCountCached(),
            "Projection+Contains" => BuildProjectionContainsCached(),
            "Projection+AnonymousMaterialization" => BuildProjectionAnonymousMaterializationCached(),
            "SetOperator+UnionCount" => BuildSetOperatorUnionCountCached(),
            "Projection+TypedDtoFirst" => BuildProjectionTypedDtoFirstCached(),
            "Sequence+SequenceEqual" => BuildSequenceEqualCached(),
            "ComplexPredicate" => BuildComplexPredicateCached(),
            "Sort+Take+Sum" => BuildSortTakeSumCached(),
            "Any+Complex" => BuildAnyComplexCached(),
            "MultiStage" => BuildMultiStageCached(),
            _ => throw new NotSupportedException($"No Dynamic.Core cached variant for query '{queryName}'.")
        };

    private static Func<IQueryable<Product>, object?> BuildFilterCountCached()
    {
        var predicate = ParseProductLambda<bool>("Price > 100 && IsActive");
        return ps => ps.Where(predicate).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildFilterProjectSumCached()
    {
        var predicate = ParseProductLambda<bool>("Category == \"Electronics\"");
        var selector = ParseProductLambda<decimal>("Price");
        return ps => ps.Where(predicate).Select(selector).Sum();
    }

    private static Func<IQueryable<Product>, object?> BuildProjectionDistinctCategoryCountCached()
    {
        var selector = ParseProductLambda<string>("Category");
        return ps => ps.Select(selector).Distinct().Count();
    }

    private static Func<IQueryable<Product>, object?> BuildProjectionContainsCached()
    {
        var selector = ParseProductLambda<string>("Category");
        return ps => ps.Select(selector).Contains("Electronics");
    }

    private static Func<IQueryable<Product>, object?> BuildProjectionAnonymousMaterializationCached()
    {
        var selector = ParseProductLambda<object>("new (Category, Price)");
        return ps => ps.Select(selector).Take(256).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildSetOperatorUnionCountCached()
    {
        var activePredicate = ParseProductLambda<bool>("IsActive");
        var expensivePredicate = ParseProductLambda<bool>("Price > 500");
        var selector = ParseProductLambda<string>("Category");

        return ps =>
        {
            var active = ps.Where(activePredicate).Select(selector);
            var expensive = ps.Where(expensivePredicate).Select(selector);
            return active.Union(expensive).Count();
        };
    }

    private static Func<IQueryable<Product>, object?> BuildProjectionTypedDtoFirstCached()
    {
        var selector = ParseProductLambda<object>("new (Name, Price)");
        var idSelector = ParseProductLambda<int>("Id");

        return ps =>
        {
            var projection = ps.OrderBy(idSelector).Select(selector).First();
            return ReadProjectedDecimal(projection, nameof(ProductSummaryDto.Price));
        };
    }

    private static Func<IQueryable<Product>, object?> BuildSequenceEqualCached()
    {
        var predicate = ParseProductLambda<bool>("IsActive");
        var idSelector = ParseProductLambda<int>("Id");
        var categorySelector = ParseProductLambda<string>("Category");

        return ps =>
        {
            var left = ps.Where(predicate).OrderBy(idSelector).Take(128).Select(categorySelector);
            var right = ps.Where(predicate).OrderBy(idSelector).Take(128).Select(categorySelector);
            return left.SequenceEqual(right);
        };
    }

    private static Func<IQueryable<Product>, object?> BuildComplexPredicateCached()
    {
        var predicate = ParseProductLambda<bool>("Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive");
        return ps => ps.Where(predicate).Count();
    }

    private static Func<IQueryable<Product>, object?> BuildSortTakeSumCached()
    {
        var keySelector = ParseProductLambda<decimal>("Price");
        var selector = ParseProductLambda<decimal>("Price");
        return ps => ps.OrderByDescending(keySelector).Take(10).Select(selector).Sum();
    }

    private static Func<IQueryable<Product>, object?> BuildAnyComplexCached()
    {
        var predicate = ParseProductLambda<bool>("Price > 900 && Stock > 500");
        return ps => ps.Any(predicate);
    }

    private static Func<IQueryable<Product>, object?> BuildMultiStageCached()
    {
        var predicate1 = ParseProductLambda<bool>("IsActive && Stock > 0");
        var predicate2 = ParseProductLambda<bool>("Price > 25");
        var selector = ParseProductLambda<decimal>("Price");
        return ps => ps.Where(predicate1).Where(predicate2).Select(selector).Sum();
    }

    public static IReadOnlyList<DynamicLinqQuery> GetDynamicLinqQueries() =>
    [
        new("Filter+Count",
            ps => ps.Count(p => p.Price > 100m && p.IsActive),
            (ps, engine) => ps.WhereDynamic<Product>(engine, "Price > 100 && IsActive").Count(),
            (ps, engine) => ps.WhereDynamic(ParseAlderPredicate(engine, "Price > 100 && IsActive")).Count(),
            ps => ps.Where("Price > 100 && IsActive").Count()),

        new("Filter+Project+Sum",
            ps => ps.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, """Category == "Electronics" """)
                .SelectDynamic<Product, decimal>(engine, "Price")
                .Sum(),
            (ps, engine) => ps
                .WhereDynamic(ParseAlderPredicate(engine, """Category == "Electronics" """))
                .SumDynamic(ParseAlderSelector<decimal>(engine, "Price")),
            ps => ps.Where("Category == \"Electronics\"").Sum("Price")),

        new("Projection+DistinctCategoryCount",
            ps => ps.Select(p => p.Category).Distinct().Count(),
            (ps, engine) => ps.SelectDynamic<Product, string>(engine, "Category").Distinct().Count(),
            (ps, engine) => ps.SelectDynamic(ParseAlderSelector<string>(engine, "Category")).Distinct().Count(),
            ps => ps.Select("Category").Cast<string>().Distinct().Count()),

        new("Projection+Contains",
            ps => ps.Select(p => p.Category).Contains("Electronics"),
            (ps, engine) => ps.SelectDynamic<Product, string>(engine, "Category").ContainsDynamic("Electronics"),
            (ps, engine) => ps.SelectDynamic(ParseAlderSelector<string>(engine, "Category")).ContainsDynamic("Electronics"),
            ps => ps.Select("Category").Cast<string>().Contains("Electronics")),

        new("Projection+AnonymousMaterialization",
            ps => ps.Select(p => new { p.Category, p.Price }).Take(256).Count(),
            (ps, engine) => ps
                .SelectDynamic<Product, object>(engine, "new { Category, Price }")
                .Take(256)
                .Count(),
            (ps, engine) => ps
                .SelectDynamic(ParseAlderSelector<object>(engine, "new { Category, Price }"))
                .Take(256)
                .Count(),
            ps => ps.Select("new (Category, Price)").Take(256).Cast<object>().Count()),

        new("SetOperator+UnionCount",
            ps => ps
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Union(ps.Where(p => p.Price > 500m).Select(p => p.Category))
                .Count(),
            (ps, engine) =>
            {
                var active = ps.WhereDynamic<Product>(engine, "IsActive").SelectDynamic<Product, string>(engine, "Category");
                var expensive = ps.WhereDynamic<Product>(engine, "Price > 500").SelectDynamic<Product, string>(engine, "Category");
                return active.UnionDynamic(expensive).Count();
            },
            (ps, engine) =>
            {
                var activePredicate = ParseAlderPredicate(engine, "IsActive");
                var expensivePredicate = ParseAlderPredicate(engine, "Price > 500");
                var selector = ParseAlderSelector<string>(engine, "Category");
                var active = ps.WhereDynamic(activePredicate).SelectDynamic(selector);
                var expensive = ps.WhereDynamic(expensivePredicate).SelectDynamic(selector);
                return active.UnionDynamic(expensive).Count();
            },
            ps => ps.Where("IsActive").Select("Category").Cast<string>()
                .Union(ps.Where("Price > 500").Select("Category").Cast<string>())
                .Count()),

        new("Projection+TypedDtoFirst",
            ps => ps.OrderBy(p => p.Id)
                .Select(p => new ProductSummaryDto { Name = p.Name, Price = p.Price })
                .First()
                .Price,
            (ps, engine) =>
            {
                var projection = ps.OrderByDynamic<Product, int>(engine, "Id")
                    .SelectDynamic<Product, object>(engine, "new { Name, Price }")
                    .First();
                return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(projection)!.Price;
            },
            (ps, engine) =>
            {
                var keySelector = ParseAlderSelector<int>(engine, "Id");
                var selector = ParseAlderSelector<object>(engine, "new { Name, Price }");
                var projection = ps.OrderByDynamic(keySelector).SelectDynamic(selector).First();
                return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(projection)!.Price;
            },
            ps => ReadProjectedDecimal(
                ps.OrderBy("Id").Select("new (Name, Price)").First(),
                nameof(ProductSummaryDto.Price))),

        new("Sequence+SequenceEqual",
            ps =>
            {
                var left = ps.Where(p => p.IsActive).OrderBy(p => p.Id).Take(128).Select(p => p.Category);
                var right = ps.Where(p => p.IsActive).OrderBy(p => p.Id).Take(128).Select(p => p.Category);
                return left.SequenceEqual(right);
            },
            (ps, engine) =>
            {
                var left = ps.WhereDynamic<Product>(engine, "IsActive")
                    .OrderByDynamic<Product, int>(engine, "Id")
                    .Take(128)
                    .SelectDynamic<Product, string>(engine, "Category");
                var right = ps.WhereDynamic<Product>(engine, "IsActive")
                    .OrderByDynamic<Product, int>(engine, "Id")
                    .Take(128)
                    .SelectDynamic<Product, string>(engine, "Category");
                return left.SequenceEqualDynamic(right);
            },
            (ps, engine) =>
            {
                var predicate = ParseAlderPredicate(engine, "IsActive");
                var idSelector = ParseAlderSelector<int>(engine, "Id");
                var categorySelector = ParseAlderSelector<string>(engine, "Category");
                var left = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).Take(128).SelectDynamic(categorySelector);
                var right = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).Take(128).SelectDynamic(categorySelector);
                return left.SequenceEqualDynamic(right);
            },
            ps =>
            {
                var left = ps.Where("IsActive").OrderBy("Id").Take(128).Select("Category").Cast<string>();
                var right = ps.Where("IsActive").OrderBy("Id").Take(128).Select("Category").Cast<string>();
                return left.SequenceEqual(right);
            }),

        new("ComplexPredicate",
            ps => ps.Count(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive")
                .Count(),
            (ps, engine) => ps
                .WhereDynamic(ParseAlderPredicate(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive"))
                .Count(),
            ps => ps.Where("Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive").Count()),

        new("Sort+Take+Sum",
            ps => ps.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .OrderByDescendingDynamic<Product, decimal>(engine, "Price")
                .Take(10)
                .SelectDynamic<Product, decimal>(engine, "Price")
                .Sum(),
            (ps, engine) => ps
                .OrderByDescendingDynamic(ParseAlderSelector<decimal>(engine, "Price"))
                .Take(10)
                .SumDynamic(ParseAlderSelector<decimal>(engine, "Price")),
            ps => ps.OrderBy("Price descending").Take(10).Sum("Price")),

        new("Any+Complex",
            ps => ps.Any(p => p.Price > 900m && p.Stock > 500),
            (ps, engine) => ps.AnyDynamic<Product>(engine, "Price > 900 && Stock > 500"),
            (ps, engine) => ps.AnyDynamic(ParseAlderPredicate(engine, "Price > 900 && Stock > 500")),
            ps => ps.Any("Price > 900 && Stock > 500")),

        new("MultiStage",
            ps => ps.Where(p => p.IsActive && p.Stock > 0).Where(p => p.Price > 25m).Select(p => p.Price).Sum(),
            (ps, engine) => ps
                .WhereDynamic<Product>(engine, "IsActive && Stock > 0")
                .WhereDynamic<Product>(engine, "Price > 25")
                .SelectDynamic<Product, decimal>(engine, "Price")
                .Sum(),
            (ps, engine) => ps
                .WhereDynamic(ParseAlderPredicate(engine, "IsActive && Stock > 0"))
                .WhereDynamic(ParseAlderPredicate(engine, "Price > 25"))
                .SumDynamic(ParseAlderSelector<decimal>(engine, "Price")),
            ps => ps.Where("IsActive && Stock > 0").Where("Price > 25").Sum("Price")),
    ];

    private static decimal ReadProjectedDecimal(object projection, string memberName)
    {
        var property = projection.GetType().GetProperty(memberName)
            ?? throw new InvalidOperationException($"Projection did not expose member '{memberName}'.");
        return (decimal)(property.GetValue(projection)
            ?? throw new InvalidOperationException($"Projection member '{memberName}' was null."));
    }

    private sealed class ProductSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}

/// <summary>
/// Measures end-to-end dynamic query cost (data materialization + engine setup + parse/bind/execute) in a cold-start process.
/// There is intentionally no <c>GlobalSetup</c> so setup work stays inside the measured sample.
/// </summary>
[Config(typeof(ColdStartConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqBenchmarksColdStart
{
    [Params(100, 1_000, 10_000, 100_000)]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicLinqQuery Query { get; set; } = null!;

    public IEnumerable<DynamicLinqQuery> Queries() => DynamicLinqBenchmarks.GetDynamicLinqQueries();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object Native()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        return Query.Native(data.Products.AsQueryable())!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object Alder_DynamicLinq()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        return Query.Alder(data.Products.AsQueryable(), engine)!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object SystemDynamicLinqCore()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        return Query.DynLinqCore(data.Products.AsQueryable())!;
    }
}
