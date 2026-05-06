using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Measures a reusable rules-engine workload over an in-memory product batch.
/// Each benchmark evaluates the same selected rules against the same entities and counts matches.
/// The workload is intended to represent operational rules evaluation rather than isolated expression dispatch.
/// </summary>
[Config(typeof(MonitoringConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BusinessRulesBenchmarks : BenchmarkBase
{
    [ParamsSource(nameof(RuleCounts))]
    public int RuleCount { get; set; }

    [ParamsSource(nameof(EntityCounts))]
    public int EntityCount { get; set; }

    public IEnumerable<int> RuleCounts() =>
        BenchmarkProfileContext.CurrentDefinition.BusinessRuleCounts;

    public IEnumerable<int> EntityCounts() =>
        BenchmarkProfileContext.CurrentDefinition.BusinessRuleEntityCounts;

    private List<Product> _entities = null!;
    private Func<Product, bool>[] _nativeRules = null!;
    private AlderExpression[] _interpRules = null!;
    private AlderExpression[] _compRules = null!;
    private AlderExpression[] _fecRules = null!;
    private Func<Product, bool>[] _typedDelegateRules = null!;
    private AlderEngine _interpreted = null!;
    private AlderEngine _compiled = null!;
    private AlderEngine _compiledFec = null!;
    private AlderEngine _typedDelegateEngine = null!;

    internal static readonly (string Expr, Func<Product, bool> Native)[] RulePool =
    [
        ("""product.Price > 100m && product.IsActive""", p => p.Price > 100m && p.IsActive),
        ("""product.Stock < 10 && product.IsActive""", p => p.Stock < 10 && p.IsActive),
        ("product.Rating >= 4.5", p => p.Rating >= 4.5),
        ("""product.Category == "Electronics" """, p => p.Category == "Electronics"),
        ("product.Price < 20m && product.Stock > 100", p => p.Price < 20m && p.Stock > 100),
        ("product.Rating < 2.0 || !product.IsActive", p => p.Rating < 2.0 || !p.IsActive),
        ("product.Stock > 500 && product.Price > 50m", p => p.Stock > 500 && p.Price > 50m),
        ("""product.Category == "Books" && product.Price < 30m""", p => p.Category == "Books" && p.Price < 30m),
        ("product.IsActive && product.Rating > 3.0 && product.Stock > 0",
            p => p.IsActive && p.Rating > 3.0 && p.Stock > 0),
        ("product.Price * product.Stock > 10000m", p => p.Price * p.Stock > 10000m),
        ("product.CreatedDate > new DateTime(2023, 1, 1)", p => p.CreatedDate > new DateTime(2023, 1, 1)),
        ("""product.Category != "Food" && product.Price > 75m""", p => p.Category != "Food" && p.Price > 75m),
        ("product.Stock == 0", p => p.Stock == 0),
        ("""product.Name.StartsWith("Product_0")""", p => p.Name.StartsWith("Product_0")),
        ("product.Rating >= 3.0 && product.Rating <= 4.0", p => p.Rating >= 3.0 && p.Rating <= 4.0),
        ("product.Price > 200m || product.Stock > 800", p => p.Price > 200m || p.Stock > 800),
        ("""product.Category == "Clothing" && product.IsActive && product.Stock > 50""",
            p => p.Category == "Clothing" && p.IsActive && p.Stock > 50),
        ("product.Price < 10m", p => p.Price < 10m),
        ("!product.IsActive && product.Stock > 0", p => !p.IsActive && p.Stock > 0),
        ("""product.Rating > 4.0 && product.Price < 100m && product.Category == "Home" """,
            p => p.Rating > 4.0 && p.Price < 100m && p.Category == "Home"),
        ("product.Stock >= 100 && product.Stock <= 500", p => p.Stock >= 100 && p.Stock <= 500),
        ("product.Price > 500m && product.Rating >= 4.0", p => p.Price > 500m && p.Rating >= 4.0),
        ("""product.Category == "Sports" || product.Category == "Toys" """,
            p => p.Category == "Sports" || p.Category == "Toys"),
        ("product.Price > (decimal)product.Stock", p => p.Price > (decimal)p.Stock),
        ("""product.Name.Contains("00")""", p => p.Name.Contains("00")),
    ];

    [GlobalSetup]
    public void Setup()
    {
        _entities = BenchmarkData.CreateProducts(EntityCount);

        _interpreted = new AlderEngine();
        _compiled = new AlderEngine(o => o.UseCompiler());
        _compiledFec = new AlderEngine(o => o.UseCompiler(new FastExpressionCompilerAdapter()));
        _typedDelegateEngine = new AlderEngine(o => o.UseCompiler());

        var selectedRules = RulePool.Take(RuleCount).ToArray();
        _nativeRules = selectedRules.Select(r => r.Native).ToArray();

        _interpRules = new AlderExpression[RuleCount];
        _compRules = new AlderExpression[RuleCount];
        _fecRules = new AlderExpression[RuleCount];
        _typedDelegateRules = new Func<Product, bool>[RuleCount];
        for (int i = 0; i < RuleCount; i++)
        {
            _interpRules[i] = _interpreted.Parse(selectedRules[i].Expr);
            _compRules[i] = _compiled.Parse(selectedRules[i].Expr);
            _fecRules[i] = _compiledFec.Parse(selectedRules[i].Expr);
            _typedDelegateRules[i] = _typedDelegateEngine.Compile<Func<Product, bool>>(
                selectedRules[i].Expr, "product");
        }

        // Setup verifies parity before timing begins so benchmark results are not detached from correctness.
        var entity = _entities[0];
        _interpreted.SetVariable<Product>("product", entity);
        _compiled.SetVariable<Product>("product", entity);
        _compiledFec.SetVariable<Product>("product", entity);

        for (int i = 0; i < RuleCount; i++)
        {
            var native = _nativeRules[i](entity);
            var interp = (bool)_interpreted.Evaluate(_interpRules[i])!;
            var comp = (bool)_compiled.Evaluate(_compRules[i])!;
            var fec = (bool)_compiledFec.Evaluate(_fecRules[i])!;
            var typed = _typedDelegateRules[i](entity);
            if (native != interp || native != comp || native != fec || native != typed)
                throw new InvalidOperationException(
                    $"Rule {i} parity failure on entity {entity.Id}: native={native}, interp={interp}, comp={comp}, fec={fec}, typed={typed}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _interpreted?.Dispose();
        _compiled?.Dispose();
        _compiledFec?.Dispose();
        _typedDelegateEngine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/BusinessRules")]
    public int Native()
    {
        int matches = 0;
        foreach (var entity in _entities)
            foreach (var rule in _nativeRules)
                if (rule(entity)) matches++;
        return matches;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/BusinessRules")]
    public int Alder_Interpreted()
    {
        int matches = 0;
        foreach (var entity in _entities)
        {
            _interpreted.SetVariable<Product>("product", entity);
            foreach (var rule in _interpRules)
                if ((bool)_interpreted.Evaluate(rule)!) matches++;
        }
        return matches;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/BusinessRules")]
    public int Alder_Compiled()
    {
        int matches = 0;
        foreach (var entity in _entities)
        {
            _compiled.SetVariable<Product>("product", entity);
            foreach (var rule in _compRules)
                if ((bool)_compiled.Evaluate(rule)!) matches++;
        }
        return matches;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/BusinessRules")]
    public int Alder_CompiledFec()
    {
        int matches = 0;
        foreach (var entity in _entities)
        {
            _compiledFec.SetVariable<Product>("product", entity);
            foreach (var rule in _fecRules)
                if ((bool)_compiledFec.Evaluate(rule)!) matches++;
        }
        return matches;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/BusinessRules")]
    public int Alder_TypedDelegate()
    {
        int matches = 0;
        foreach (var entity in _entities)
            foreach (var rule in _typedDelegateRules)
                if (rule(entity)) matches++;
        return matches;
    }
}
