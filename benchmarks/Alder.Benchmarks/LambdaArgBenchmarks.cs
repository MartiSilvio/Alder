using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LambdaArgBenchmarks
{
    private readonly InvocationTarget _target = new();
    private AlderEngine _interpreted = null!;
    private AlderEngine _compiled = null!;
    private AlderEngine _compiledFec = null!;
    private AlderExpression _interpretedExpr = null!;
    private AlderExpression _compiledExpr = null!;
    private AlderExpression _compiledFecExpr = null!;

    private const string TransformExpr = "target.Transform(10, (n) => n * 2 + 1)";

    [GlobalSetup]
    public void Setup()
    {
        _interpreted = new AlderEngine();
        _interpreted.SetVariable("target", _target);
        _interpretedExpr = _interpreted.Parse(TransformExpr);

        _compiled = new AlderEngine(new AlderOptions().UseCompiler());
        _compiled.SetVariable("target", _target);
        _compiledExpr = _compiled.Parse(TransformExpr);

        _compiledFec = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
        _compiledFec.SetVariable("target", _target);
        _compiledFecExpr = _compiledFec.Parse(TransformExpr);

        // Warm up
        _interpreted.Evaluate(_interpretedExpr);
        _compiled.Evaluate(_compiledExpr);
        _compiledFec.Evaluate(_compiledFecExpr);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("LambdaArg")]
    public object Native_Baseline() => _target.Transform(10, n => n * 2 + 1);

    [Benchmark]
    [BenchmarkCategory("LambdaArg")]
    public object Alder_Interpreted() => _interpreted.Evaluate(_interpretedExpr)!;

    [Benchmark]
    [BenchmarkCategory("LambdaArg")]
    public object Alder_Compiled() => _compiled.Evaluate(_compiledExpr)!;

    [Benchmark]
    [BenchmarkCategory("LambdaArg")]
    public object Alder_CompiledFec() => _compiledFec.Evaluate(_compiledFecExpr)!;
}
