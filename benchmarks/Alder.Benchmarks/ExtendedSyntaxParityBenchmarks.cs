using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExtendedSyntaxParityBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();

    private AlderEngine _extendedInterpreted = null!;
    private AlderEngine _extendedCompiled = null!;
    private AlderEngine _standardInterpreted = null!;
    private AlderEngine _standardCompiled = null!;

    private AlderExpression _extendedInterpretedExpression = null!;
    private AlderExpression _extendedCompiledExpression = null!;
    private AlderExpression _standardInterpretedExpression = null!;
    private AlderExpression _standardCompiledExpression = null!;

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

    private static void ConfigureShared(AlderEngine engine)
    {
        engine.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);
    }
}
