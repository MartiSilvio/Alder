using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Measures hot-path invocation cost for typed delegates versus repeated engine evaluation.
/// The scenarios cover both scalar arithmetic and a reusable business-rule predicate.
/// </summary>
[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TypedDelegateCompilationBenchmarks : BenchmarkBase
{
    private BenchmarkData _data = null!;

    private Func<int, int, int> _typedScalarFunc = null!;
    private AlderEngine _compiledEngine = null!;
    private AlderExpression _compiledExpr = null!;

    private Func<Product, bool> _typedRuleFunc = null!;
    private AlderEngine _ruleEngine = null!;
    private AlderExpression _ruleExpr = null!;
    private Product _sampleProduct = null!;

    private const string ScalarCode = "Math.Abs(a - b) + Math.Max(a, b) * 2";
    private const string RuleCode = "p.Price > 100m && p.IsActive && p.Rating >= 4.0";

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
        _sampleProduct = _data.Products[0];

        using (var scalarEngine = new AlderEngine(new AlderOptions().UseCompiler()))
            _typedScalarFunc = scalarEngine.Compile<Func<int, int, int>>(ScalarCode, "a", "b");

        _compiledEngine = new AlderEngine(new AlderOptions().UseCompiler());
        _compiledEngine.SetVariable<int>("a", _data.X);
        _compiledEngine.SetVariable<int>("b", _data.Y);
        _compiledExpr = _compiledEngine.Parse(ScalarCode);
        _compiledEngine.Evaluate(_compiledExpr);

        using (var ruleEngine = new AlderEngine(new AlderOptions().UseCompiler()))
            _typedRuleFunc = ruleEngine.Compile<Func<Product, bool>>(RuleCode, "p");

        _ruleEngine = new AlderEngine(new AlderOptions().UseCompiler());
        _ruleEngine.SetVariable<Product>("p", _sampleProduct);
        _ruleExpr = _ruleEngine.Parse(RuleCode);
        _ruleEngine.Evaluate(_ruleExpr);
        var scalarExpected = _typedScalarFunc(_data.X, _data.Y);
        var scalarActual = _compiledEngine.Evaluate(_compiledExpr);
        if (!BenchmarkParityVerifier.AreEquivalent(scalarExpected, scalarActual))
            throw new InvalidOperationException(
                $"Typed delegate scalar parity failure: typed={scalarExpected}, engine={scalarActual}");

        var sampleProducts = _data.Products.Take(32).ToArray();
        foreach (var product in sampleProducts)
        {
            _ruleEngine.SetVariable<Product>("p", product);
            var typed = _typedRuleFunc(product);
            var evaluated = _ruleEngine.Evaluate(_ruleExpr);
            if (!BenchmarkParityVerifier.AreEquivalent(typed, evaluated))
                throw new InvalidOperationException(
                    $"Typed delegate rule parity failure on product {product.Id}: typed={typed}, engine={evaluated}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compiledEngine?.Dispose();
        _ruleEngine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/TypedDelegate/ScalarInvocation")]
    public object Scalar_EngineEvaluate()
    {
        return _compiledEngine.Evaluate(_compiledExpr)!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/TypedDelegate/ScalarInvocation")]
    public int Scalar_TypedDelegate()
    {
        return _typedScalarFunc(_data.X, _data.Y);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/TypedDelegate/BusinessRuleInvocation")]
    public object Rule_EngineEvaluate()
    {
        return _ruleEngine.Evaluate(_ruleExpr)!;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/TypedDelegate/BusinessRuleInvocation")]
    public bool Rule_TypedDelegate()
    {
        return _typedRuleFunc(_sampleProduct);
    }
}
