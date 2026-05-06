using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

/// <summary>
/// Measures the cost of Alder extended syntax against equivalent standard expressions.
/// Each scenario pairs an extended form with a standard form that should produce the same result.
/// The benchmark is only meaningful because setup verifies parity across the exercised backends.
/// </summary>
[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExtendedSyntaxBenchmarks
{
    [ParamsSource(nameof(Scenarios))]
    public ExtendedScenario Scenario { get; set; } = null!;

    public IEnumerable<ExtendedScenario> Scenarios() => BenchmarkScenarios.GetExtendedScenarios();

    private BenchmarkData _data = null!;

    private AlderEngine _extInterp = null!;
    private AlderEngine _extComp = null!;
    private AlderEngine _stdInterp = null!;
    private AlderEngine _stdComp = null!;

    private AlderExpression _extInterpExpr = null!;
    private AlderExpression _extCompExpr = null!;
    private AlderExpression _stdInterpExpr = null!;
    private AlderExpression _stdCompExpr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();

        _extInterp = BenchmarkBase.CreateEngine(CompilationMode.Interpreted, _data, LanguageMode.Extended, ConfigureExtended);
        _extComp = BenchmarkBase.CreateEngine(CompilationMode.Compiled, _data, LanguageMode.Extended, ConfigureExtended);
        _stdInterp = BenchmarkBase.CreateEngine(CompilationMode.Interpreted, _data, LanguageMode.Standard, ConfigureExtended);
        _stdComp = BenchmarkBase.CreateEngine(CompilationMode.Compiled, _data, LanguageMode.Standard, ConfigureExtended);

        _extInterpExpr = _extInterp.Parse(Scenario.ExtendedExpr);
        _extCompExpr = _extComp.Parse(Scenario.ExtendedExpr);
        _stdInterpExpr = _stdInterp.Parse(Scenario.StandardExpr);
        _stdCompExpr = _stdComp.Parse(Scenario.StandardExpr);

        var parity = BenchmarkParityVerifier.VerifyExtendedScenario(Scenario, _data, includeFec: false);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _extInterp?.Dispose();
        _extComp?.Dispose();
        _stdInterp?.Dispose();
        _stdComp?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Standard_Interpreted() => _stdInterp.Evaluate(_stdInterpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Standard_Compiled() => _stdComp.Evaluate(_stdCompExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Extended_Interpreted() => _extInterp.Evaluate(_extInterpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Extended_Compiled() => _extComp.Evaluate(_extCompExpr)!;

    private static void ConfigureExtended(AlderOptions opts)
    {
        opts.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
    }
}
