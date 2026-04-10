using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Alder-specific feature benchmark measuring the overhead of extended syntax
/// relative to equivalent standard Alder expressions.
///
/// Each scenario has an Extended-mode expression and its Standard-mode
/// equivalent. Both are compiled with Alder Compiled+FEC. The difference
/// reveals the overhead of extended syntax desugaring.
///
/// This benchmark is only valid when every execution backend produces the same
/// result for both the standard and extended forms.
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
    private AlderEngine _extFec = null!;
    private AlderEngine _stdInterp = null!;
    private AlderEngine _stdComp = null!;
    private AlderEngine _stdFec = null!;

    private AlderExpression _extInterpExpr = null!;
    private AlderExpression _extCompExpr = null!;
    private AlderExpression _extFecExpr = null!;
    private AlderExpression _stdInterpExpr = null!;
    private AlderExpression _stdCompExpr = null!;
    private AlderExpression _stdFecExpr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();

        _extInterp = BenchmarkBase.CreateEngine(CompilationMode.Interpreted, _data, LanguageMode.Extended, ConfigureExtended);
        _extComp = BenchmarkBase.CreateEngine(CompilationMode.Compiled, _data, LanguageMode.Extended, ConfigureExtended);
        _extFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, _data, LanguageMode.Extended, ConfigureExtended);
        _stdInterp = BenchmarkBase.CreateEngine(CompilationMode.Interpreted, _data, LanguageMode.Standard, ConfigureExtended);
        _stdComp = BenchmarkBase.CreateEngine(CompilationMode.Compiled, _data, LanguageMode.Standard, ConfigureExtended);
        _stdFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, _data, LanguageMode.Standard, ConfigureExtended);

        _extInterpExpr = _extInterp.Parse(Scenario.ExtendedExpr);
        _extCompExpr = _extComp.Parse(Scenario.ExtendedExpr);
        _extFecExpr = _extFec.Parse(Scenario.ExtendedExpr);
        _stdInterpExpr = _stdInterp.Parse(Scenario.StandardExpr);
        _stdCompExpr = _stdComp.Parse(Scenario.StandardExpr);
        _stdFecExpr = _stdFec.Parse(Scenario.StandardExpr);

        var parity = BenchmarkParityVerifier.VerifyExtendedScenario(Scenario, _data);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _extInterp?.Dispose();
        _extComp?.Dispose();
        _extFec?.Dispose();
        _stdInterp?.Dispose();
        _stdComp?.Dispose();
        _stdFec?.Dispose();
    }

    #region Standard baseline (what it costs without sugar)

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Standard_Interpreted() => _stdInterp.Evaluate(_stdInterpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Standard_Compiled() => _stdComp.Evaluate(_stdCompExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Standard_CompiledFec() => _stdFec.Evaluate(_stdFecExpr)!;

    #endregion

    #region Extended mode (sugar cost)

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Extended_Interpreted() => _extInterp.Evaluate(_extInterpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Extended_Compiled() => _extComp.Evaluate(_extCompExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExtendedSyntax")]
    public object Extended_CompiledFec() => _extFec.Evaluate(_extFecExpr)!;

    #endregion

    private static void ConfigureExtended(AlderOptions opts)
    {
        opts.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1);
    }
}
