using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BindingMicroBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private CsEvalExpression _warmInterpretedExpression = null!;
    private CsEvalExpression _warmCompiledExpression = null!;
    private CsEvalExpression _extendedWarmExpression = null!;
    private CsEvalEngine _extendedInterpretedEngine = null!;

    private const string MethodHeavyExpression = "Math.Abs(x - y) + Math.Max(y, z)";
    private const string ExtendedAliasExpression = "x not in numbers";

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _warmInterpretedExpression = InterpretedEngine.Parse(MethodHeavyExpression);
        _warmCompiledExpression = CompiledEngine.Parse(MethodHeavyExpression);

        _extendedInterpretedEngine = CreateEngine(CompilationMode.Interpreted, _globals, LanguageMode.Extended);
        _extendedWarmExpression = _extendedInterpretedEngine.Parse(ExtendedAliasExpression);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine.Dispose();
        CompiledEngine.Dispose();
        _extendedInterpretedEngine.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("BindingCold")]
    public object ParseOnly_MethodHeavy() => InterpretedEngine.Parse(MethodHeavyExpression);

    [Benchmark]
    [BenchmarkCategory("BindingCold")]
    public object EvaluateCold_MethodHeavy_Interpreted()
    {
        var cold = CreateEngine(CompilationMode.Interpreted, _globals);
        try
        {
            return cold.Evaluate(MethodHeavyExpression)!;
        }
        finally
        {
            cold.Dispose();
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_MethodHeavy_Interpreted() =>
        InterpretedEngine.Evaluate(_warmInterpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_MethodHeavy_Compiled() =>
        CompiledEngine.Evaluate(_warmCompiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_ExtendedAlias_Interpreted() =>
        _extendedInterpretedEngine.Evaluate(_extendedWarmExpression)!;
}
