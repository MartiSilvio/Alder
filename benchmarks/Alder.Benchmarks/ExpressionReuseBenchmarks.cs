using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExpressionReuseBenchmarks
{
    private AlderEngine _interpreted = null!;
    private AlderEngine _compiled = null!;
    private AlderExpression _interpretedExpr = null!;
    private AlderExpression _compiledExpr = null!;

    private const string Expression = "(x + y) * z - x / 2";
    private const int Iterations = 1000;

    [GlobalSetup]
    public void Setup()
    {
        _interpreted = new AlderEngine();
        _interpreted.SetVariable<int>("x", 10);
        _interpreted.SetVariable<int>("y", 3);
        _interpreted.SetVariable<int>("z", 7);
        _interpretedExpr = _interpreted.Parse(Expression);

        _compiled = new AlderEngine(new AlderOptions().UseCompiler());
        _compiled.SetVariable<int>("x", 10);
        _compiled.SetVariable<int>("y", 3);
        _compiled.SetVariable<int>("z", 7);
        _compiledExpr = _compiled.Parse(Expression);

        // Warm up compiled path
        _compiled.Evaluate(_compiledExpr);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reuse")]
    public object? Interpreted_Reuse_1000x()
    {
        object? result = null;
        for (var i = 0; i < Iterations; i++)
        {
            _interpreted.SetVariable<int>("x", i);
            result = _interpreted.Evaluate(_interpretedExpr);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Reuse")]
    public object? Compiled_Reuse_1000x()
    {
        object? result = null;
        for (var i = 0; i < Iterations; i++)
        {
            _compiled.SetVariable<int>("x", i);
            result = _compiled.Evaluate(_compiledExpr);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Reuse")]
    public object? Interpreted_ParseAndEval_1000x()
    {
        object? result = null;
        for (var i = 0; i < Iterations; i++)
        {
            _interpreted.SetVariable<int>("x", i);
            result = _interpreted.Evaluate(Expression);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Reuse")]
    public object? Compiled_ParseAndEval_1000x()
    {
        object? result = null;
        for (var i = 0; i < Iterations; i++)
        {
            _compiled.SetVariable<int>("x", i);
            result = _compiled.Evaluate(Expression);
        }
        return result;
    }
}
