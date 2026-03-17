using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CsEval.Compiled;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class VariableScalingBenchmarks
{
    [Params(10, 100, 1000)]
    public int VariableCount { get; set; }

    private CsEvalEngine _interpreted = null!;
    private CsEvalEngine _compiled = null!;
    private CsEvalExpression _interpretedExpr = null!;
    private CsEvalExpression _compiledExpr = null!;

    // Always evaluates the last variable — forces lookup through the full scope
    private const string Expression = "target + 1";

    [GlobalSetup]
    public void Setup()
    {
        _interpreted = new CsEvalEngine();
        _compiled = new CsEvalEngine(CsEvalOptions.Default.UseCompiler());

        for (var i = 0; i < VariableCount; i++)
        {
            _interpreted.SetVariable<int>($"v{i}", i);
            _compiled.SetVariable<int>($"v{i}", i);
        }

        _interpreted.SetVariable<int>("target", 42);
        _compiled.SetVariable<int>("target", 42);

        _interpretedExpr = _interpreted.Parse(Expression);
        _compiledExpr = _compiled.Parse(Expression);

        // Warm up compiled path
        _compiled.Evaluate(_compiledExpr);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("VarScale")]
    public object? Interpreted() => _interpreted.Evaluate(_interpretedExpr);

    [Benchmark]
    [BenchmarkCategory("VarScale")]
    public object? Compiled() => _compiled.Evaluate(_compiledExpr);
}
