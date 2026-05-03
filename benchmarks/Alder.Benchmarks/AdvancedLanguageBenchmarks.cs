using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Scripting;

namespace Alder.Benchmarks;

/// <summary>
/// Measures Alder language features that exercise the full runtime engine: control flow, statement blocks, lambdas, async, and dispatch.
/// The comparison set is Native C# and Roslyn scripting.
/// </summary>
[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AdvancedLanguageBenchmarks : BenchmarkBase
{
    private readonly BenchmarkData _data = BenchmarkData.CreateStandard();
    private ScriptRunner<object> _roslynRunner = null!;
    private AlderExpression _interpExpr = null!;
    private AlderExpression _compExpr = null!;

    [ParamsSource(nameof(Scenarios))]
    public AlderScenario Scenario { get; set; } = null!;

    public IEnumerable<AlderScenario> Scenarios() => BenchmarkScenarios.GetAdvancedScenarios();

    [GlobalSetup]
    public void Setup()
    {
        InterpretedEngine = CreateEngine(CompilationMode.Interpreted, _data);
        CompiledEngine = CreateEngine(CompilationMode.Compiled, _data);
        _interpExpr = InterpretedEngine.Parse(Scenario.AlderExpr);
        _compExpr = CompiledEngine.Parse(Scenario.AlderExpr);

        var script = CreateRoslynScript(Scenario.RoslynExpr);
        script.Compile();
        _roslynRunner = script.CreateDelegate();

        // Capability benchmarks still require parity checks so feature coverage claims remain tied to correctness.
        var parity = BenchmarkParityVerifier.VerifyAlderScenario(Scenario, _data);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine?.Dispose();
        CompiledEngine?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Capability/AdvancedLanguage")]
    public object Native() => Scenario.Native(_data)!;

    [Benchmark]
    [BenchmarkCategory("Capability/AdvancedLanguage")]
    public object Alder_Interpreted() => InterpretedEngine.Evaluate(_interpExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/AdvancedLanguage")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compExpr)!;

    [Benchmark]
    [BenchmarkCategory("Capability/AdvancedLanguage")]
    public async Task<object> Roslyn() => (await _roslynRunner(_data))!;
}
