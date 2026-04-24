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
    Func<IQueryable<Product>, AlderEngine, object?> AlderNonGeneric,
    Func<IQueryable<Product>, AlderEngine, object?> AlderGeneric,
    Func<AlderEngine, Func<IQueryable<Product>, object?>>? AlderParsedLambda,
    Func<IQueryable<Product>, object?> DynamicCoreString,
    Func<Func<IQueryable<Product>, object?>>? DynamicCoreParsedLambda,
    Func<AlderEngine, Func<IQueryable<Product>, object?>>? AlderParsedPlan = null)
{
    public override string ToString() => Name;
}

[Config(typeof(DynamicLinqConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqBenchmarks : BenchmarkBase
{
    private static readonly int[] DefaultScaleFactors = [10_000];
    private static readonly int[] ExhaustiveScaleFactors = [100, 1_000, 10_000, 100_000];
    private static readonly string[] DefaultQueryNames =
    [
        "Filter+Count",
        "Filter+Project+Sum",
        "Projection+AnonymousMaterialization",
        "Sort+Take+Sum",
        "Aggregate+MinMaxAverage",
        "GroupBy+CategoryCount"
    ];
    private static readonly string[] DefaultColdQueryNames =
    [
        "Filter+Count",
        "Projection+AnonymousMaterialization",
        "Aggregate+MinMaxAverage"
    ];

    [ParamsSource(nameof(ScaleFactors))]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicLinqQuery Query { get; set; } = null!;

    public IEnumerable<int> ScaleFactors() => GetBenchmarkScaleFactors();

    public IEnumerable<DynamicLinqQuery> Queries() => GetBenchmarkQueries();

    private BenchmarkData _data = null!;
    private IQueryable<Product> _productsQuery = null!;
    private AlderEngine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.Create(productCount: ScaleFactor);
        _productsQuery = _data.Products.AsQueryable();
        _engine = new AlderEngine(new AlderOptions().UseCompiler());

        var native = Query.Native(_productsQuery);
        VerifyParity(native, Query.AlderNonGeneric(_productsQuery, _engine), "AlderNonGeneric");
        VerifyParity(native, Query.AlderGeneric(_productsQuery, _engine), "AlderGeneric");
        VerifyParity(native, Query.DynamicCoreString(_productsQuery), "DynamicCoreString");
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
    public object Alder_DynamicLinq_NonGeneric() => Query.AlderNonGeneric(_productsQuery, _engine)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Warm")]
    public object Alder_DynamicLinq_Generic() => Query.AlderGeneric(_productsQuery, _engine)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Warm")]
    public object SystemDynamicLinqCore_String() => Query.DynamicCoreString(_productsQuery)!;

    private void VerifyParity(object? expected, object? actual, string implementation)
    {
        if (!BenchmarkParityVerifier.AreEquivalent(expected, actual))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={expected}, {implementation}={actual}");
    }

    private static Expression<Func<Product, TResult>> ParseProductLambda<TResult>(string expression) =>
        DynamicExpressionParser.ParseLambda<Product, TResult>(
            new ParsingConfig(),
            createParameterCtor: true,
            expression);

    private static Expression<Func<TOuter, TInner, TResult>> ParseProductBinaryLambda<TOuter, TInner, TResult>(string expression) =>
        (Expression<Func<TOuter, TInner, TResult>>)DynamicExpressionParser.ParseLambda(
            new ParsingConfig(),
            createParameterCtor: true,
            [Expression.Parameter(typeof(TOuter), "outer"), Expression.Parameter(typeof(TInner), "inner")],
            typeof(TResult),
            expression);

    private static Expression<Func<Product, bool>> ParseAlderPredicate(AlderEngine engine, string expression) =>
        engine.ParsePredicate<Product>(expression).ToExpression<Func<Product, bool>>();

    private static Expression<Func<Product, TResult>> ParseAlderSelector<TResult>(AlderEngine engine, string expression) =>
        engine.ParseSelector<Product, TResult>(expression).ToExpression<Func<Product, TResult>>();

    private static Expression<Func<TOuter, TInner, TResult>> ParseAlderBinarySelector<TOuter, TInner, TResult>(
        AlderEngine engine,
        string expression) =>
        engine.ParseLambda(
            [typeof(TOuter), typeof(TInner)],
            ["outer", "inner"],
            typeof(TResult),
            expression)
            .ToExpression<Func<TOuter, TInner, TResult>>();

    public static IReadOnlyList<DynamicLinqQuery> GetDynamicLinqQueries() =>
    [
        new("Filter+Count",
            ps => ps.Count(p => p.Price > 100m && p.IsActive),
            (ps, engine) => ps.CountDynamic(engine, "Price > 100 && IsActive"),
            (ps, engine) => ps.CountDynamic<Product>(engine, "Price > 100 && IsActive"),
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, "Price > 100 && IsActive");
                return ps => ps.CountDynamic(predicate);
            },
            ps => ps.Where("Price > 100 && IsActive").Count(),
            () =>
            {
                var predicate = ParseProductLambda<bool>("Price > 100 && IsActive");
                return ps => ps.Count(predicate);
            },
            AlderParsedPlan: engine =>
            {
                var predicate = engine.ParsePredicate<Product>("Price > 100 && IsActive");
                return ps => ps.CountDynamic(predicate);
            }),

        new("Filter+Project+Sum",
            ps => ps.Where(p => p.Category == "Electronics").Select(p => p.Price).Sum(),
            (ps, engine) => ps.WhereDynamic(engine, """Category == "Electronics" """).SumDynamic(engine, "Price"),
            (ps, engine) => ps.WhereDynamic<Product>(engine, """Category == "Electronics" """).SumDynamic<Product, decimal>(engine, "Price"),
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, """Category == "Electronics" """);
                var selector = ParseAlderSelector<decimal>(engine, "Price");
                return ps => ps.WhereDynamic(predicate).SumDynamic(selector);
            },
            ps => ps.Where("""Category == "Electronics" """).Sum("Price"),
            () =>
            {
                var predicate = ParseProductLambda<bool>("Category == \"Electronics\"");
                var selector = ParseProductLambda<decimal>("Price");
                return ps => ps.Where(predicate).Select(selector).Sum();
            },
            AlderParsedPlan: engine =>
            {
                var predicate = engine.ParsePredicate<Product>("""Category == "Electronics" """);
                var selector = engine.ParseSelector<Product, decimal>("Price");
                return ps => ps.WhereDynamic(predicate).SumDynamic(selector);
            }),

        new("Projection+DistinctCategoryCount",
            ps => ps.Select(p => p.Category).Distinct().Count(),
            (ps, engine) => ps.SelectDynamic(engine, "Category").Cast<string>().DistinctDynamic().Count(),
            (ps, engine) => ps.SelectDynamic<Product, string>(engine, "Category").DistinctDynamic().Count(),
            engine =>
            {
                var selector = ParseAlderSelector<string>(engine, "Category");
                return ps => ps.SelectDynamic(selector).DistinctDynamic().Count();
            },
            ps => ps.Select("Category").Cast<string>().Distinct().Count(),
            () =>
            {
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.Select(selector).Distinct().Count();
            }),

        new("Projection+Contains",
            ps => ps.Select(p => p.Category).Contains("Electronics"),
            (ps, engine) => ps.SelectDynamic(engine, "Category").Cast<string>().ContainsDynamic("Electronics"),
            (ps, engine) => ps.SelectDynamic<Product, string>(engine, "Category").ContainsDynamic("Electronics"),
            engine =>
            {
                var selector = ParseAlderSelector<string>(engine, "Category");
                return ps => ps.SelectDynamic(selector).ContainsDynamic("Electronics");
            },
            ps => ps.Select("Category").Cast<string>().Contains("Electronics"),
            () =>
            {
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.Select(selector).Contains("Electronics");
            }),

        new("Projection+AnonymousMaterialization",
            ps => ps.Select(p => new { p.Category, p.Price }).Take(256).Count(),
            (ps, engine) => ps.SelectDynamic(engine, "new { Category, Price }").Cast<object>().TakeDynamic(256).Count(),
            (ps, engine) => ps.SelectDynamic<Product, object>(engine, "new { Category, Price }").TakeDynamic(256).Count(),
            AlderParsedLambda: null,
            DynamicCoreString: ps => ps.Select("new (Category, Price)").Take(256).Cast<object>().Count(),
            DynamicCoreParsedLambda: null),

        new("SetOperator+UnionCount",
            ps => ps.Where(p => p.IsActive).Select(p => p.Category)
                .Union(ps.Where(p => p.Price > 500m).Select(p => p.Category))
                .Count(),
            (ps, engine) =>
            {
                var active = ps.WhereDynamic(engine, "IsActive").SelectDynamic(engine, "Category").Cast<string>();
                var expensive = ps.WhereDynamic(engine, "Price > 500").SelectDynamic(engine, "Category").Cast<string>();
                return active.UnionDynamic(expensive).Count();
            },
            (ps, engine) =>
            {
                var active = ps.WhereDynamic<Product>(engine, "IsActive").SelectDynamic<Product, string>(engine, "Category");
                var expensive = ps.WhereDynamic<Product>(engine, "Price > 500").SelectDynamic<Product, string>(engine, "Category");
                return active.UnionDynamic(expensive).Count();
            },
            engine =>
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
            },
            ps => ps.Where("IsActive").Select("Category").Cast<string>()
                .Union(ps.Where("Price > 500").Select("Category").Cast<string>())
                .Count(),
            () =>
            {
                var activePredicate = ParseProductLambda<bool>("IsActive");
                var expensivePredicate = ParseProductLambda<bool>("Price > 500");
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.Where(activePredicate).Select(selector)
                    .Union(ps.Where(expensivePredicate).Select(selector))
                    .Count();
            }),

        new("SetOperator+IntersectCount",
            ps => ps.Where(p => p.IsActive).Select(p => p.Category)
                .Intersect(ps.Where(p => p.Price > 500m).Select(p => p.Category))
                .Count(),
            (ps, engine) =>
            {
                var active = ps.WhereDynamic(engine, "IsActive").SelectDynamic(engine, "Category").Cast<string>();
                var expensive = ps.WhereDynamic(engine, "Price > 500").SelectDynamic(engine, "Category").Cast<string>();
                return active.IntersectDynamic(expensive).Count();
            },
            (ps, engine) =>
            {
                var active = ps.WhereDynamic<Product>(engine, "IsActive").SelectDynamic<Product, string>(engine, "Category");
                var expensive = ps.WhereDynamic<Product>(engine, "Price > 500").SelectDynamic<Product, string>(engine, "Category");
                return active.IntersectDynamic(expensive).Count();
            },
            engine =>
            {
                var activePredicate = ParseAlderPredicate(engine, "IsActive");
                var expensivePredicate = ParseAlderPredicate(engine, "Price > 500");
                var selector = ParseAlderSelector<string>(engine, "Category");
                return ps => ps.WhereDynamic(activePredicate).SelectDynamic(selector)
                    .IntersectDynamic(ps.WhereDynamic(expensivePredicate).SelectDynamic(selector))
                    .Count();
            },
            ps => ps.Where("IsActive").Select("Category").Cast<string>()
                .Intersect(ps.Where("Price > 500").Select("Category").Cast<string>())
                .Count(),
            () =>
            {
                var activePredicate = ParseProductLambda<bool>("IsActive");
                var expensivePredicate = ParseProductLambda<bool>("Price > 500");
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.Where(activePredicate).Select(selector)
                    .Intersect(ps.Where(expensivePredicate).Select(selector))
                    .Count();
            }),

        new("SetOperator+ExceptCount",
            ps => ps.Where(p => p.IsActive).Select(p => p.Category)
                .Except(ps.Where(p => p.Price > 500m).Select(p => p.Category))
                .Count(),
            (ps, engine) =>
            {
                var active = ps.WhereDynamic(engine, "IsActive").SelectDynamic(engine, "Category").Cast<string>();
                var expensive = ps.WhereDynamic(engine, "Price > 500").SelectDynamic(engine, "Category").Cast<string>();
                return active.ExceptDynamic(expensive).Count();
            },
            (ps, engine) =>
            {
                var active = ps.WhereDynamic<Product>(engine, "IsActive").SelectDynamic<Product, string>(engine, "Category");
                var expensive = ps.WhereDynamic<Product>(engine, "Price > 500").SelectDynamic<Product, string>(engine, "Category");
                return active.ExceptDynamic(expensive).Count();
            },
            engine =>
            {
                var activePredicate = ParseAlderPredicate(engine, "IsActive");
                var expensivePredicate = ParseAlderPredicate(engine, "Price > 500");
                var selector = ParseAlderSelector<string>(engine, "Category");
                return ps => ps.WhereDynamic(activePredicate).SelectDynamic(selector)
                    .ExceptDynamic(ps.WhereDynamic(expensivePredicate).SelectDynamic(selector))
                    .Count();
            },
            ps => ps.Where("IsActive").Select("Category").Cast<string>()
                .Except(ps.Where("Price > 500").Select("Category").Cast<string>())
                .Count(),
            () =>
            {
                var activePredicate = ParseProductLambda<bool>("IsActive");
                var expensivePredicate = ParseProductLambda<bool>("Price > 500");
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.Where(activePredicate).Select(selector)
                    .Except(ps.Where(expensivePredicate).Select(selector))
                    .Count();
            }),

        new("Projection+TypedDtoFirst",
            ps => ps.OrderBy(p => p.Id)
                .Select(p => new ProductSummaryDto { Name = p.Name, Price = p.Price })
                .First()
                .Price,
            (ps, engine) =>
            {
                var projection = ps.OrderByDynamic(engine, "Id").SelectDynamic(engine, "new { Name, Price }").First();
                return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(projection)!.Price;
            },
            (ps, engine) =>
            {
                var projection = ps.OrderByDynamic<Product, int>(engine, "Id")
                    .SelectDynamic<Product, object>(engine, "new { Name, Price }")
                    .First();
                return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(projection)!.Price;
            },
            AlderParsedLambda: null,
            DynamicCoreString: ps => ReadProjectedDecimal(ps.OrderBy("Id").Select("new (Name, Price)").First(), nameof(ProductSummaryDto.Price)),
            DynamicCoreParsedLambda: null),

        new("Sequence+SequenceEqual",
            ps =>
            {
                var left = ps.Where(p => p.IsActive).OrderBy(p => p.Id).Take(128).Select(p => p.Category);
                var right = ps.Where(p => p.IsActive).OrderBy(p => p.Id).Take(128).Select(p => p.Category);
                return left.SequenceEqual(right);
            },
            (ps, engine) =>
            {
                var left = ps.WhereDynamic(engine, "IsActive").OrderByDynamic(engine, "Id").TakeDynamic(128).SelectDynamic(engine, "Category").Cast<string>();
                var right = ps.WhereDynamic(engine, "IsActive").OrderByDynamic(engine, "Id").TakeDynamic(128).SelectDynamic(engine, "Category").Cast<string>();
                return left.SequenceEqualDynamic(right);
            },
            (ps, engine) =>
            {
                var left = ps.WhereDynamic<Product>(engine, "IsActive").OrderByDynamic<Product, int>(engine, "Id").TakeDynamic(128).SelectDynamic<Product, string>(engine, "Category");
                var right = ps.WhereDynamic<Product>(engine, "IsActive").OrderByDynamic<Product, int>(engine, "Id").TakeDynamic(128).SelectDynamic<Product, string>(engine, "Category");
                return left.SequenceEqualDynamic(right);
            },
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, "IsActive");
                var idSelector = ParseAlderSelector<int>(engine, "Id");
                var categorySelector = ParseAlderSelector<string>(engine, "Category");
                return ps =>
                {
                    var left = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).TakeDynamic(128).SelectDynamic(categorySelector);
                    var right = ps.WhereDynamic(predicate).OrderByDynamic(idSelector).TakeDynamic(128).SelectDynamic(categorySelector);
                    return left.SequenceEqualDynamic(right);
                };
            },
            ps =>
            {
                var left = ps.Where("IsActive").OrderBy("Id").Take(128).Select("Category").Cast<string>();
                var right = ps.Where("IsActive").OrderBy("Id").Take(128).Select("Category").Cast<string>();
                return left.SequenceEqual(right);
            },
            () =>
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
            }),

        new("ComplexPredicate",
            ps => ps.Count(p => p.Price > 50m && p.Stock > 0 && p.Rating >= 4.0 && p.IsActive),
            (ps, engine) => ps.CountDynamic(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive"),
            (ps, engine) => ps.CountDynamic<Product>(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive"),
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, "Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive");
                return ps => ps.CountDynamic(predicate);
            },
            ps => ps.Where("Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive").Count(),
            () =>
            {
                var predicate = ParseProductLambda<bool>("Price > 50 && Stock > 0 && Rating >= 4.0 && IsActive");
                return ps => ps.Count(predicate);
            }),

        new("Sort+Take+Sum",
            ps => ps.OrderByDescending(p => p.Price).Take(10).Select(p => p.Price).Sum(),
            (ps, engine) => ps.OrderByDescendingDynamic(engine, "Price").TakeDynamic(10).SumDynamic(engine, "Price"),
            (ps, engine) => ps.OrderByDescendingDynamic<Product, decimal>(engine, "Price").TakeDynamic(10).SelectDynamic<Product, decimal>(engine, "Price").Sum(),
            engine =>
            {
                var keySelector = ParseAlderSelector<decimal>(engine, "Price");
                var selector = ParseAlderSelector<decimal>(engine, "Price");
                return ps => ps.OrderByDescendingDynamic(keySelector).TakeDynamic(10).SumDynamic(selector);
            },
            ps => ps.OrderBy("Price descending").Take(10).Sum("Price"),
            () =>
            {
                var keySelector = ParseProductLambda<decimal>("Price");
                var selector = ParseProductLambda<decimal>("Price");
                return ps => ps.OrderByDescending(keySelector).Take(10).Select(selector).Sum();
            },
            AlderParsedPlan: engine =>
            {
                var price = engine.ParseSelector<Product, decimal>("Price");
                return ps => ps.OrderByDescendingDynamic<Product, decimal>(price).TakeDynamic(10).SumDynamic(price);
            }),

        new("Sort+ThenBy+First",
            ps => ps.OrderBy(p => p.Category).ThenByDescending(p => p.Price).First().Id,
            (ps, engine) => ((Product)ps.OrderByDynamic(engine, "Category").ThenByDescendingDynamic(engine, "Price").First()).Id,
            (ps, engine) => ps.OrderByDynamic<Product, string>(engine, "Category").ThenByDescendingDynamic<Product, decimal>(engine, "Price").First().Id,
            engine =>
            {
                var categorySelector = ParseAlderSelector<string>(engine, "Category");
                var priceSelector = ParseAlderSelector<decimal>(engine, "Price");
                return ps => ps.OrderByDynamic(categorySelector).ThenByDescendingDynamic(priceSelector).First().Id;
            },
            ps => ((Product)ps.OrderBy("Category").ThenBy("Price descending").First()).Id,
            () =>
            {
                var categorySelector = ParseProductLambda<string>("Category");
                var priceSelector = ParseProductLambda<decimal>("Price");
                return ps => ps.OrderBy(categorySelector).ThenByDescending(priceSelector).First().Id;
            }),

        new("Any+Complex",
            ps => ps.Any(p => p.Price > 900m && p.Stock > 500),
            (ps, engine) => ps.AnyDynamic(engine, "Price > 900 && Stock > 500"),
            (ps, engine) => ps.AnyDynamic<Product>(engine, "Price > 900 && Stock > 500"),
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, "Price > 900 && Stock > 500");
                return ps => ps.AnyDynamic(predicate);
            },
            ps => ps.Any("Price > 900 && Stock > 500"),
            () =>
            {
                var predicate = ParseProductLambda<bool>("Price > 900 && Stock > 500");
                return ps => ps.Any(predicate);
            }),

        new("Quantifier+AllAnyLongCount",
            ps => ps.All(p => p.Price >= 0m) && ps.Any(p => p.IsActive) && ps.LongCount(p => p.Stock > 0) > 0,
            (ps, engine) => ps.AllDynamic(engine, "Price >= 0") && ps.AnyDynamic(engine, "IsActive") && ps.LongCountDynamic(engine, "Stock > 0") > 0,
            (ps, engine) => ps.AllDynamic<Product>(engine, "Price >= 0") && ps.AnyDynamic<Product>(engine, "IsActive") && ps.LongCountDynamic<Product>(engine, "Stock > 0") > 0,
            engine =>
            {
                var nonNegative = ParseAlderPredicate(engine, "Price >= 0");
                var active = ParseAlderPredicate(engine, "IsActive");
                var stocked = ParseAlderPredicate(engine, "Stock > 0");
                return ps => ps.All(nonNegative) && ps.AnyDynamic(active) && ps.LongCount(stocked) > 0;
            },
            ps => ps.All("Price >= 0") && ps.Any("IsActive") && ps.LongCount("Stock > 0") > 0,
            () =>
            {
                var nonNegative = ParseProductLambda<bool>("Price >= 0");
                var active = ParseProductLambda<bool>("IsActive");
                var stocked = ParseProductLambda<bool>("Stock > 0");
                return ps => ps.All(nonNegative) && ps.Any(active) && ps.LongCount(stocked) > 0;
            }),

        new("Aggregate+MinMaxAverage",
            ps => ps.Min(p => p.Price) + ps.Max(p => p.Price) + (decimal)ps.Average(p => p.Rating),
            (ps, engine) => (decimal)ps.MinDynamic(engine, "Price")! + (decimal)ps.MaxDynamic(engine, "Price")! + Convert.ToDecimal(ps.AverageDynamic(engine, "Rating")),
            (ps, engine) => ps.SelectDynamic<Product, decimal>(engine, "Price").Min() + ps.SelectDynamic<Product, decimal>(engine, "Price").Max() + (decimal)ps.SelectDynamic<Product, double>(engine, "Rating").Average(),
            engine =>
            {
                var price = ParseAlderSelector<decimal>(engine, "Price");
                var rating = ParseAlderSelector<double>(engine, "Rating");
                return ps => ps.Min(price) + ps.Max(price) + (decimal)ps.Average(rating);
            },
            ps => Convert.ToDecimal(ps.Min("Price")) + Convert.ToDecimal(ps.Max("Price")) + (decimal)ps.Select("Rating").Cast<double>().Average(),
            () =>
            {
                var price = ParseProductLambda<decimal>("Price");
                var rating = ParseProductLambda<double>("Rating");
                return ps => ps.Min(price) + ps.Max(price) + (decimal)ps.Average(rating);
            },
            AlderParsedPlan: engine =>
            {
                var price = engine.ParseSelector<Product, decimal>("Price");
                var rating = engine.ParseSelector<Product, double>("Rating");
                return ps => ps.SelectDynamic<Product, decimal>(price).Min()
                    + ps.SelectDynamic<Product, decimal>(price).Max()
                    + (decimal)ps.SelectDynamic<Product, double>(rating).Average();
            }),

        new("GroupBy+CategoryCount",
            ps => ps.GroupBy(p => p.Category).Count(),
            (ps, engine) => ps.GroupByDynamic(engine, "Category").Cast<object>().Count(),
            (ps, engine) => ps.GroupByDynamic<Product, string>(engine, "Category").Count(),
            engine =>
            {
                var selector = ParseAlderSelector<string>(engine, "Category");
                return ps => ps.GroupBy(selector).Count();
            },
            ps => ps.GroupBy("Category").Cast<object>().Count(),
            () =>
            {
                var selector = ParseProductLambda<string>("Category");
                return ps => ps.GroupBy(selector).Count();
            },
            AlderParsedPlan: engine =>
            {
                var selector = engine.ParseSelector<Product, string>("Category");
                return ps => ps.GroupByDynamic<Product, string>(selector).Count();
            }),

        new("Join+SelfCategoryCount",
            ps => ps.Join(ps, outer => outer.Category, inner => inner.Category, (outer, inner) => outer).Take(512).Count(),
            (ps, engine) => ps.JoinDynamic(ps, engine, "Category", "Category", "outer").Cast<object>().TakeDynamic(512).Count(),
            (ps, engine) => ps.JoinDynamic<Product, Product, string, Product>(ps, engine, "Category", "Category", "outer").TakeDynamic(512).Count(),
            engine =>
            {
                var outerKey = ParseAlderSelector<string>(engine, "Category");
                var innerKey = ParseAlderSelector<string>(engine, "Category");
                var result = ParseAlderBinarySelector<Product, Product, Product>(engine, "outer");
                return ps => ps.Join(ps, outerKey, innerKey, result).Take(512).Count();
            },
            ps => ps.Join(ps, "Category", "Category", "outer").Take(512).Cast<object>().Count(),
            () =>
            {
                var outerKey = ParseProductLambda<string>("Category");
                var innerKey = ParseProductLambda<string>("Category");
                var result = ParseProductBinaryLambda<Product, Product, Product>("outer");
                return ps => ps.Join(ps, outerKey, innerKey, result).Take(512).Count();
            }),

        new("SelectMany+FlattenNameChars",
            ps => ps.SelectMany(p => p.Name).Count(),
            (ps, engine) => ps.SelectManyDynamic(engine, "Name.ToCharArray()").Cast<char>().Count(),
            (ps, engine) => ps.SelectManyDynamic<Product, char>(engine, "Name.ToCharArray()").Count(),
            engine =>
            {
                var selector = ParseAlderSelector<char[]>(engine, "Name.ToCharArray()");
                var compiledSelector = selector.Compile();
                return ps => ps.AsEnumerable().SelectMany(compiledSelector).Count();
            },
            ps => ps.SelectMany("Name.ToCharArray()").Cast<char>().Count(),
            () =>
            {
                var selector = ParseProductLambda<char[]>("Name.ToCharArray()");
                var compiledSelector = selector.Compile();
                return ps => ps.AsEnumerable().SelectMany(compiledSelector).Count();
            }),

        new("Paging+SkipTakeElementAt",
            ps => ps.OrderBy(p => p.Id).Skip(10).Take(50).ElementAt(5).Id,
            (ps, engine) => ((Product)ps.OrderByDynamic(engine, "Id").SkipDynamic(10).TakeDynamic(50).ElementAtDynamic(5)).Id,
            (ps, engine) => ps.OrderByDynamic<Product, int>(engine, "Id").SkipDynamic(10).TakeDynamic(50).ElementAtDynamic(5).Id,
            engine =>
            {
                var selector = ParseAlderSelector<int>(engine, "Id");
                return ps => ps.OrderByDynamic(selector).SkipDynamic(10).TakeDynamic(50).ElementAtDynamic(5).Id;
            },
            ps => ((Product)ps.OrderBy("Id").Skip(10).Take(50).ElementAt(5)).Id,
            () =>
            {
                var selector = ParseProductLambda<int>("Id");
                return ps => ps.OrderBy(selector).Skip(10).Take(50).ElementAt(5).Id;
            }),

        new("Element+FirstLastSingle",
            ps => ps.First(p => p.Id == 1).Id + ps.Last(p => p.Id == ps.Count()).Id + ps.Single(p => p.Id == 42).Id,
            (ps, engine) => ps.FirstDynamic(engine, "Id == 1").Id + ps.LastDynamic(engine, "Id == @0", ps.Count()).Id + ps.SingleDynamic(engine, "Id == 42").Id,
            (ps, engine) => ps.FirstDynamic<Product>(engine, "Id == 1").Id + ps.LastDynamic<Product>(engine, "Id == @0", ps.Count()).Id + ps.SingleDynamic<Product>(engine, "Id == 42").Id,
            engine =>
            {
                var firstPredicate = ParseAlderPredicate(engine, "Id == 1");
                var singlePredicate = ParseAlderPredicate(engine, "Id == 42");
                return ps => ps.First(firstPredicate).Id + ps.Last(p => p.Id == ps.Count()).Id + ps.Single(singlePredicate).Id;
            },
            ps => ((Product)ps.First("Id == 1")).Id + ((Product)ps.Last("Id == @0", ps.Count())).Id + ((Product)ps.Single("Id == 42")).Id,
            () =>
            {
                var firstPredicate = ParseProductLambda<bool>("Id == 1");
                var singlePredicate = ParseProductLambda<bool>("Id == 42");
                return ps => ps.First(firstPredicate).Id + ps.Last(p => p.Id == ps.Count()).Id + ps.Single(singlePredicate).Id;
            }),

        new("SkipTakeWhile+Count",
            ps => ps.OrderBy(p => p.Id).SkipWhile(p => p.Id < 10).TakeWhile(p => p.Id < 500).Count(),
            (ps, engine) => ps.OrderByDynamic(engine, "Id").SkipWhileDynamic(engine, "Id < 10").TakeWhileDynamic(engine, "Id < 500").Cast<object>().Count(),
            (ps, engine) => ps.OrderByDynamic<Product, int>(engine, "Id").SkipWhileDynamic<Product>(engine, "Id < 10").TakeWhileDynamic<Product>(engine, "Id < 500").Count(),
            engine =>
            {
                var idSelector = ParseAlderSelector<int>(engine, "Id");
                var skipPredicate = ParseAlderPredicate(engine, "Id < 10");
                var takePredicate = ParseAlderPredicate(engine, "Id < 500");
                return ps => ps.OrderBy(idSelector).SkipWhile(skipPredicate).TakeWhile(takePredicate).Count();
            },
            ps => ps.OrderBy("Id").SkipWhile("Id < 10").TakeWhile("Id < 500").Cast<object>().Count(),
            () =>
            {
                var idSelector = ParseProductLambda<int>("Id");
                var skipPredicate = ParseProductLambda<bool>("Id < 10");
                var takePredicate = ParseProductLambda<bool>("Id < 500");
                return ps => ps.OrderBy(idSelector).SkipWhile(skipPredicate).TakeWhile(takePredicate).Count();
            }),

        new("AppendPrependReverse+First",
            ps =>
            {
                var first = ps.OrderBy(p => p.Id).First();
                var last = ps.OrderBy(p => p.Id).Last();
                return ps.OrderBy(p => p.Id).Select(p => p.Id).Append(last.Id + 1).Prepend(first.Id - 1).Reverse().First();
            },
            (ps, engine) =>
            {
                var ordered = ps.OrderByDynamic(engine, "Id");
                var first = ((Product)ordered.First()).Id;
                var last = ((Product)ordered.Last()).Id;
                return ordered.SelectDynamic(engine, "Id").Cast<int>().AppendDynamic(last + 1).PrependDynamic(first - 1).ReverseDynamic().First();
            },
            (ps, engine) =>
            {
                var ordered = ps.OrderByDynamic<Product, int>(engine, "Id");
                var first = ordered.First().Id;
                var last = ordered.Last().Id;
                return ordered.SelectDynamic<Product, int>(engine, "Id").AppendDynamic(last + 1).PrependDynamic(first - 1).ReverseDynamic().First();
            },
            engine =>
            {
                var idSelector = ParseAlderSelector<int>(engine, "Id");
                return ps =>
                {
                    var ordered = ps.OrderByDynamic(idSelector);
                    var first = ordered.First().Id;
                    var last = ordered.Last().Id;
                    return ordered.SelectDynamic(idSelector).AppendDynamic(last + 1).PrependDynamic(first - 1).ReverseDynamic().First();
                };
            },
            ps =>
            {
                var ordered = ps.OrderBy("Id");
                var first = ((Product)ordered.First()).Id;
                var last = ((Product)ordered.Last()).Id;
                return ordered.Select("Id").Cast<int>().Append(last + 1).Prepend(first - 1).Reverse().First();
            },
            () =>
            {
                var idSelector = ParseProductLambda<int>("Id");
                return ps =>
                {
                    var ordered = ps.OrderBy(idSelector);
                    var first = ordered.First().Id;
                    var last = ordered.Last().Id;
                    return ordered.Select(idSelector).Append(last + 1).Prepend(first - 1).Reverse().First();
                };
            }),

        new("DefaultIfEmpty+Count",
            ps => ps.Where(p => p.Price < 0m).DefaultIfEmpty().Count(),
            (ps, engine) => ps.WhereDynamic(engine, "Price < 0").DefaultIfEmptyDynamic().Cast<object>().Count(),
            (ps, engine) => ps.WhereDynamic<Product>(engine, "Price < 0").DefaultIfEmptyDynamic().Count(),
            engine =>
            {
                var predicate = ParseAlderPredicate(engine, "Price < 0");
                return ps => ps.Where(predicate).DefaultIfEmpty().Count();
            },
            ps => ps.Where("Price < 0").DefaultIfEmpty().Cast<object>().Count(),
            () =>
            {
                var predicate = ParseProductLambda<bool>("Price < 0");
                return ps => ps.Where(predicate).DefaultIfEmpty().Count();
            }),

        new("MultiStage",
            ps => ps.Where(p => p.IsActive && p.Stock > 0).Where(p => p.Price > 25m).Select(p => p.Price).Sum(),
            (ps, engine) => ps.WhereDynamic(engine, "IsActive && Stock > 0").WhereDynamic(engine, "Price > 25").SumDynamic(engine, "Price"),
            (ps, engine) => ps.WhereDynamic<Product>(engine, "IsActive && Stock > 0").WhereDynamic<Product>(engine, "Price > 25").SelectDynamic<Product, decimal>(engine, "Price").Sum(),
            engine =>
            {
                var predicate1 = ParseAlderPredicate(engine, "IsActive && Stock > 0");
                var predicate2 = ParseAlderPredicate(engine, "Price > 25");
                var selector = ParseAlderSelector<decimal>(engine, "Price");
                return ps => ps.WhereDynamic(predicate1).WhereDynamic(predicate2).SumDynamic(selector);
            },
            ps => ps.Where("IsActive && Stock > 0").Where("Price > 25").Sum("Price"),
            () =>
            {
                var predicate1 = ParseProductLambda<bool>("IsActive && Stock > 0");
                var predicate2 = ParseProductLambda<bool>("Price > 25");
                var selector = ParseProductLambda<decimal>("Price");
                return ps => ps.Where(predicate1).Where(predicate2).Select(selector).Sum();
            }),
    ];

    public static IReadOnlyList<DynamicLinqQuery> GetParsedLambdaQueries() =>
        GetDynamicLinqQueries()
            .Where(query => query.AlderParsedLambda is not null && query.DynamicCoreParsedLambda is not null && query.AlderParsedPlan is not null)
            .ToArray();

    public static IReadOnlyList<DynamicLinqQuery> GetBenchmarkQueries() =>
        SelectBenchmarkQueries(DefaultQueryNames);

    public static IReadOnlyList<DynamicLinqQuery> GetBenchmarkParsedLambdaQueries() =>
        GetBenchmarkQueries()
            .Where(query => query.AlderParsedLambda is not null && query.DynamicCoreParsedLambda is not null && query.AlderParsedPlan is not null)
            .ToArray();

    public static IReadOnlyList<DynamicLinqQuery> GetBenchmarkColdStartQueries() =>
        SelectBenchmarkQueries(DefaultColdQueryNames);

    public static IReadOnlyList<int> GetBenchmarkScaleFactors() =>
        IsExhaustiveDynamicLinqRun()
            ? ExhaustiveScaleFactors
            : DefaultScaleFactors;

    public static IReadOnlyList<int> GetBenchmarkColdStartScaleFactors() =>
        IsExhaustiveDynamicLinqRun()
            ? ExhaustiveScaleFactors
            : DefaultScaleFactors;

    private static IReadOnlyList<DynamicLinqQuery> SelectBenchmarkQueries(IReadOnlyCollection<string> defaultNames)
    {
        var queries = GetDynamicLinqQueries();
        if (IsExhaustiveDynamicLinqRun())
            return queries;

        return queries
            .Where(query => defaultNames.Contains(query.Name))
            .ToArray();
    }

    private static bool IsExhaustiveDynamicLinqRun() =>
        Environment.GetEnvironmentVariable("ALDER_DYNAMIC_LINQ_EXHAUSTIVE") != null;

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

[Config(typeof(DynamicLinqConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqParsedLambdaBenchmarks : BenchmarkBase
{
    [ParamsSource(nameof(ScaleFactors))]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicLinqQuery Query { get; set; } = null!;

    public IEnumerable<int> ScaleFactors() => DynamicLinqBenchmarks.GetBenchmarkScaleFactors();

    public IEnumerable<DynamicLinqQuery> Queries() => DynamicLinqBenchmarks.GetBenchmarkParsedLambdaQueries();

    private BenchmarkData _data = null!;
    private IQueryable<Product> _productsQuery = null!;
    private AlderEngine _engine = null!;
    private Func<IQueryable<Product>, object?> _alderParsedPlan = null!;
    private Func<IQueryable<Product>, object?> _alderParsedLambda = null!;
    private Func<IQueryable<Product>, object?> _dynamicCoreParsedLambda = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.Create(productCount: ScaleFactor);
        _productsQuery = _data.Products.AsQueryable();
        _engine = new AlderEngine(new AlderOptions().UseCompiler());
        _alderParsedPlan = Query.AlderParsedPlan!(_engine);
        _alderParsedLambda = Query.AlderParsedLambda!(_engine);
        _dynamicCoreParsedLambda = Query.DynamicCoreParsedLambda!();

        var native = Query.Native(_productsQuery);
        VerifyParity(native, _alderParsedPlan(_productsQuery), "AlderParsedPlan");
        VerifyParity(native, _alderParsedLambda(_productsQuery), "AlderParsedLambda");
        VerifyParity(native, _dynamicCoreParsedLambda(_productsQuery), "DynamicCoreParsedLambda");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object Native() => Query.Native(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object Alder_DynamicLinq_ParsedPlan() => _alderParsedPlan(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object Alder_DynamicLinq_ParsedLambda() => _alderParsedLambda(_productsQuery)!;

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/PreParsed")]
    public object SystemDynamicLinqCore_ParsedLambda() => _dynamicCoreParsedLambda(_productsQuery)!;

    private void VerifyParity(object? expected, object? actual, string implementation)
    {
        if (!BenchmarkParityVerifier.AreEquivalent(expected, actual))
            throw new InvalidOperationException(
                $"Parity failure: {Query.Name} SF{ScaleFactor} | Native={expected}, {implementation}={actual}");
    }
}

/// <summary>
/// Measures end-to-end dynamic query cost (data materialization + engine setup + parse/bind/execute) in a cold-start process.
/// There is intentionally no <c>GlobalSetup</c> so setup work stays inside the measured sample.
/// </summary>
[Config(typeof(DynamicLinqColdStartConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DynamicLinqBenchmarksColdStart
{
    [ParamsSource(nameof(ScaleFactors))]
    public int ScaleFactor { get; set; }

    [ParamsSource(nameof(Queries))]
    public DynamicLinqQuery Query { get; set; } = null!;

    public IEnumerable<int> ScaleFactors() => DynamicLinqBenchmarks.GetBenchmarkColdStartScaleFactors();

    public IEnumerable<DynamicLinqQuery> Queries() => DynamicLinqBenchmarks.GetBenchmarkColdStartQueries();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object Native()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        return Query.Native(data.Products.AsQueryable())!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object Alder_DynamicLinq_NonGeneric()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        return Query.AlderNonGeneric(data.Products.AsQueryable(), engine)!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object Alder_DynamicLinq_Generic()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        return Query.AlderGeneric(data.Products.AsQueryable(), engine)!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/DynamicLinq/Cold")]
    public object SystemDynamicLinqCore_String()
    {
        var data = BenchmarkData.Create(productCount: ScaleFactor);
        return Query.DynamicCoreString(data.Products.AsQueryable())!;
    }
}
