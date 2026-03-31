using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AnonymousVariableBenchmarks
{
    private AlderEngine _interpreted = null!;
    private AlderEngine _compiled = null!;
    private AlderExpression _interpretedExpr = null!;
    private AlderExpression _compiledExpr = null!;

    private const string SimpleExpression = "x + y * z";
    private const string PropertyChainExpression = "order.Category + \": \" + order.Quantity";

    [GlobalSetup]
    public void Setup()
    {
        _interpreted = new AlderEngine();
        _compiled = new AlderEngine(new AlderOptions().UseCompiler());

        _interpretedExpr = _interpreted.Parse(SimpleExpression);
        _compiledExpr = _compiled.Parse(SimpleExpression);

        // Warm up both paths
        _interpreted.Evaluate(_interpretedExpr, new { x = 1, y = 2, z = 3 });
        _compiled.Evaluate(_compiledExpr, new { x = 1, y = 2, z = 3 });
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AnonymousVars")]
    public object? Interpreted_SetVariable_PreParsed()
    {
        _interpreted.SetVariable<int>("x", 10);
        _interpreted.SetVariable<int>("y", 3);
        _interpreted.SetVariable<int>("z", 7);
        return _interpreted.Evaluate(_interpretedExpr);
    }

    [Benchmark]
    [BenchmarkCategory("AnonymousVars")]
    public object? Interpreted_AnonymousObject_PreParsed()
        => _interpreted.Evaluate(_interpretedExpr, new { x = 10, y = 3, z = 7 });

    [Benchmark]
    [BenchmarkCategory("AnonymousVars")]
    public object? Compiled_SetVariable_PreParsed()
    {
        _compiled.SetVariable<int>("x", 10);
        _compiled.SetVariable<int>("y", 3);
        _compiled.SetVariable<int>("z", 7);
        return _compiled.Evaluate(_compiledExpr);
    }

    [Benchmark]
    [BenchmarkCategory("AnonymousVars")]
    public object? Compiled_AnonymousObject_PreParsed()
        => _compiled.Evaluate(_compiledExpr, new { x = 10, y = 3, z = 7 });

    [Benchmark]
    [BenchmarkCategory("AnonymousVars")]
    public object? Interpreted_AnonymousObject_FromString()
        => _interpreted.Evaluate(SimpleExpression, new { x = 10, y = 3, z = 7 });

    [Benchmark]
    [BenchmarkCategory("AnonymousVars")]
    public object? Compiled_AnonymousObject_FromString()
        => _compiled.Evaluate(SimpleExpression, new { x = 10, y = 3, z = 7 });

    [Benchmark]
    [BenchmarkCategory("AnonymousVars/Typed")]
    public object? Interpreted_TypedObject()
        => _interpreted.Evaluate(
            PropertyChainExpression,
            new { order = new Order("A", 4, 12.5m) });

    [Benchmark]
    [BenchmarkCategory("AnonymousVars/Typed")]
    public object? Compiled_TypedObject()
        => _compiled.Evaluate(
            PropertyChainExpression,
            new { order = new Order("A", 4, 12.5m) });
}
