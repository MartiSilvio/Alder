using DynamicExpresso;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

public sealed record MicroScenario(
    string Name,
    string AlderExpression,
    string? DynamicExpressoExpression)
{
    public override string ToString() => Name;
}

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MicroBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private AlderEngine _interpretedReflectionEngine = null!;
    private AlderExpression _interpretedExpression = null!;
    private AlderExpression _interpretedReflectionExpression = null!;
    private AlderExpression _compiledExpression = null!;
    private AlderExpression _compiledFecExpression = null!;
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
            null),
        new("ArithmeticOnly",
            "x + y * z",
            "x + y * z"),
        new("TernaryOnly",
            "x > 0 ? x : -x",
            "x > 0 ? x : -x"),
        new("PropertyChain",
            "text.Length > 3 && text.Contains(\"l\")",
            "text.Length > 3 && text.Contains(\"l\")"),
        new("CastExpression",
            "(double)x / y",
            "(double)x / y"),
        new("StringInterpolation",
            "$\"{text} is {x + y}\"",
            null)
    ];

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _interpretedExpression = InterpretedEngine.Parse(Scenario.AlderExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.AlderExpression);
        _compiledFecExpression = CompiledFecEngine.Parse(Scenario.AlderExpression);

        _interpretedReflectionEngine = new AlderEngine(o => o.Aot.ClearBuiltInContext());
        ApplyGlobals(_interpretedReflectionEngine, _globals);
        _interpretedReflectionExpression = _interpretedReflectionEngine.Parse(Scenario.AlderExpression);

        if (Scenario.DynamicExpressoExpression is not null)
        {
            var interpreter = CreateDynamicExpressoInterpreter(_globals);
            _dynamicExpressoExpression = interpreter.Parse(Scenario.DynamicExpressoExpression);
        }
    }

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Interpreted_Reflection() => _interpretedReflectionEngine.Evaluate(_interpretedReflectionExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Interpreted_Generated() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("WarmExecution")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WarmExecution")]
    public object? DynamicExpresso() => _dynamicExpressoExpression?.Invoke();
}
