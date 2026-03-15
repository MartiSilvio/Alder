using DynamicExpresso;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace CsEval.Benchmarks;

public sealed record MicroScenario(
    string Name,
    string CsEvalExpression,
    string DynamicExpressoExpression)
{
    public override string ToString() => Name;
}

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MicroBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private CsEvalEngine _interpretedReflectionEngine = null!;
    private CsEvalExpression _interpretedExpression = null!;
    private CsEvalExpression _interpretedReflectionExpression = null!;
    private CsEvalExpression _compiledExpression = null!;
    private Lambda _dynamicExpressoExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public MicroScenario Scenario { get; set; } = null!;

    public IEnumerable<MicroScenario> ScenarioSource() => GetMicroScenarios();

    public static IReadOnlyList<MicroScenario> GetMicroScenarios() =>
    [
        new("StaticMethodCall",
            "Math.Abs(-5)",
            "Math.Abs(-5)"),
        new("ChainedStaticCalls",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Math.Abs(x - y) + Math.Max(y, z)"),
        new("InstanceMethodCall",
            "text.Contains(\"a\")",
            "text.Contains(\"a\")"),
        new("PropertyAccess",
            "text.Length",
            "text.Length"),
        new("LinqWhereCount",
            "numbers.Where((n) => n > value).Count()",
            "numbers.Where(n => n > value).Count()"),
        new("ArithmeticOnly",
            "x + y * z",
            "x + y * z"),
        new("TernaryOnly",
            "x > 0 ? x : -x",
            "x > 0 ? x : -x")
    ];

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.CsEvalExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.CsEvalExpression);

        _interpretedReflectionEngine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.Interpreted
        });
        _interpretedReflectionEngine.ClearGeneratedContexts();
        ApplyGlobals(_interpretedReflectionEngine, _globals);
        _interpretedReflectionExpression = _interpretedReflectionEngine.Parse(Scenario.CsEvalExpression);

        var interpreter = CreateDynamicExpressoInterpreter(_globals);
        _dynamicExpressoExpression = interpreter.Parse(Scenario.DynamicExpressoExpression);
    }

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object CsEval_Interpreted_Reflection() => _interpretedReflectionEngine.Evaluate(_interpretedReflectionExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object CsEval_Interpreted_Generated() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WarmExecution")]
    public object DynamicExpresso() => _dynamicExpressoExpression.Invoke()!;
}
