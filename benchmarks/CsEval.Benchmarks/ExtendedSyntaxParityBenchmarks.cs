using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExtendedSyntaxParityBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();

    private CsEvalEngine _extendedInterpreted = null!;
    private CsEvalEngine _extendedCompiled = null!;
    private CsEvalEngine _standardInterpreted = null!;
    private CsEvalEngine _standardCompiled = null!;

    private CsEvalExpression _extendedInterpretedExpression = null!;
    private CsEvalExpression _extendedCompiledExpression = null!;
    private CsEvalExpression _standardInterpretedExpression = null!;
    private CsEvalExpression _standardCompiledExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public ExtendedParityScenario Scenario { get; set; } = null!;

    public IEnumerable<ExtendedParityScenario> ScenarioSource() =>
        BenchmarkScenarioCatalog.GetExtendedParityScenarios();

    [GlobalSetup]
    public void Setup()
    {
        _extendedInterpreted = CreateEngine(CompilationMode.Interpreted, _globals, LanguageMode.Extended);
        _extendedCompiled = CreateEngine(CompilationMode.Compiled, _globals, LanguageMode.Extended);
        _standardInterpreted = CreateEngine(CompilationMode.Interpreted, _globals, LanguageMode.Standard);
        _standardCompiled = CreateEngine(CompilationMode.Compiled, _globals, LanguageMode.Standard);

        ConfigureShared(_extendedInterpreted);
        ConfigureShared(_extendedCompiled);
        ConfigureShared(_standardInterpreted);
        ConfigureShared(_standardCompiled);

        _extendedInterpretedExpression = _extendedInterpreted.Parse(Scenario.ExtendedExpression);
        _extendedCompiledExpression = _extendedCompiled.Parse(Scenario.ExtendedExpression);
        _standardInterpretedExpression = _standardInterpreted.Parse(Scenario.StandardExpression);
        _standardCompiledExpression = _standardCompiled.Parse(Scenario.StandardExpression);

        var parity = BenchmarkParityVerifier.VerifyExtendedParityScenario(Scenario, _globals);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _extendedInterpreted.Dispose();
        _extendedCompiled.Dispose();
        _standardInterpreted.Dispose();
        _standardCompiled.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ExtendedParityInterpreted")]
    public object Standard_Interpreted() => _standardInterpreted.Evaluate(_standardInterpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("ExtendedParityInterpreted")]
    public object Extended_Interpreted() => _extendedInterpreted.Evaluate(_extendedInterpretedExpression)!;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ExtendedParityCompiled")]
    public object Standard_Compiled() => _standardCompiled.Evaluate(_standardCompiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("ExtendedParityCompiled")]
    public object Extended_Compiled() => _extendedCompiled.Evaluate(_extendedCompiledExpression)!;

    private static void ConfigureShared(CsEvalEngine engine)
    {
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
    }
}
