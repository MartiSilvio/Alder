using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BindingMicroBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private AlderExpression _warmInterpretedExpression = null!;
    private AlderExpression _warmCompiledExpression = null!;
    private AlderExpression _warmCompiledFecExpression = null!;
    private AlderExpression _extendedWarmExpression = null!;
    private AlderExpression _loopMutationWarmExpression = null!;
    private AlderExpression _loopMutationCompiledExpression = null!;
    private AlderExpression _loopMutationCompiledFecExpression = null!;
    private AlderEngine _extendedInterpretedEngine = null!;

    private const string MethodHeavyExpression = "Math.Abs(x - y) + Math.Max(y, z)";
    private const string ExtendedAliasExpression = "x not in numbers";
    private const string LoopMutationExpression = "{ var sum = 0; for (var i = 0; i < 128; i++) { sum += i; } return sum; }";

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _warmInterpretedExpression = InterpretedEngine.Parse(MethodHeavyExpression);
        _warmCompiledExpression = CompiledEngine.Parse(MethodHeavyExpression);
        _warmCompiledFecExpression = CompiledFecEngine.Parse(MethodHeavyExpression);
        _loopMutationWarmExpression = InterpretedEngine.Parse(LoopMutationExpression);
        _loopMutationCompiledExpression = CompiledEngine.Parse(LoopMutationExpression);
        _loopMutationCompiledFecExpression = CompiledFecEngine.Parse(LoopMutationExpression);

        _extendedInterpretedEngine = CreateEngine(CompilationMode.Interpreted, _globals, LanguageMode.Extended);
        _extendedWarmExpression = _extendedInterpretedEngine.Parse(ExtendedAliasExpression);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine.Dispose();
        CompiledEngine.Dispose();
        CompiledFecEngine.Dispose();
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
    public object EvaluateWarm_MethodHeavy_CompiledFec() =>
        CompiledFecEngine.Evaluate(_warmCompiledFecExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_ExtendedAlias_Interpreted() =>
        _extendedInterpretedEngine.Evaluate(_extendedWarmExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_LoopMutation_Interpreted() =>
        InterpretedEngine.Evaluate(_loopMutationWarmExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_LoopMutation_Compiled() =>
        CompiledEngine.Evaluate(_loopMutationCompiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("BindingWarm")]
    public object EvaluateWarm_LoopMutation_CompiledFec() =>
        CompiledFecEngine.Evaluate(_loopMutationCompiledFecExpression)!;
}
