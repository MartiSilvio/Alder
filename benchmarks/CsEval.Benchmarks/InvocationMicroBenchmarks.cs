using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DynamicExpresso;

namespace CsEval.Benchmarks;

public sealed record InvocationScenario(
    string Name,
    string CsEvalExpression,
    string DynamicExpressoExpression)
{
    public override string ToString() => Name;
}

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class InvocationMicroBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private readonly InvocationTarget _target = new();
    private CsEvalExpression _interpretedExpression = null!;
    private CsEvalExpression _compiledExpression = null!;
    private CsEvalExpression _compiledFecExpression = null!;
    private Lambda _dynamicExpressoExpression = null!;

    [ParamsSource(nameof(ScenarioSource))]
    public InvocationScenario Scenario { get; set; } = null!;

    public IEnumerable<InvocationScenario> ScenarioSource() => GetScenarios();

    public static IReadOnlyList<InvocationScenario> GetScenarios() =>
    [
        new(
            "StaticMathMix",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Math.Abs(x - y) + Math.Max(y, z)"),
        new(
            "InstanceStringContains",
            "text.Contains(\"a\")",
            "text.Contains(\"a\")"),
        new(
            "OverloadResolution_Int",
            "target.Overload(x)",
            "target.Overload(x)"),
        new(
            "OverloadResolution_LongLiteral",
            "target.Overload(10000000000L)",
            "target.Overload(10000000000L)"),
        new(
            "ImplicitNumericConversion",
            "target.ConsumeDouble(x)",
            "target.ConsumeDouble(x)"),
        new(
            "ParamsExpansion",
            "target.Sum(x, y, z, value)",
            "target.Sum(x, y, z, value)"),
        new(
            "OptionalArgument",
            "target.WithOptional(x)",
            "target.WithOptional(x)"),
        new(
            "ChainedInstanceCalls",
            "target.Normalize(text).Contains(\"AL\")",
            "target.Normalize(text).Contains(\"AL\")")
    ];

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        InterpretedEngine.SetVariable("target", _target);
        CompiledEngine.SetVariable("target", _target);
        CompiledFecEngine.SetVariable("target", _target);

        _interpretedExpression = InterpretedEngine.Parse(Scenario.CsEvalExpression);
        _compiledExpression = CompiledEngine.Parse(Scenario.CsEvalExpression);
        _compiledFecExpression = CompiledFecEngine.Parse(Scenario.CsEvalExpression);

        var interpreter = CreateDynamicExpressoInterpreter(_globals);
        interpreter.SetVariable("target", _target);
        _dynamicExpressoExpression = interpreter.Parse(Scenario.DynamicExpressoExpression);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        InterpretedEngine.Dispose();
        CompiledEngine.Dispose();
        CompiledFecEngine.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("Invocation/Warm")]
    public object CsEval_Interpreted() => InterpretedEngine.Evaluate(_interpretedExpression)!;

    [Benchmark]
    [BenchmarkCategory("Invocation/Warm")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("Invocation/Warm")]
    public object CsEval_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Invocation/Warm")]
    public object DynamicExpresso() => _dynamicExpressoExpression.Invoke()!;
}

public sealed class InvocationTarget
{
    public int Overload(int value) => value + 1;
    public long Overload(long value) => value + 1L;

    public double ConsumeDouble(double value) => value / 2.0;

    public int Sum(params int[] values)
    {
        var total = 0;
        for (var i = 0; i < values.Length; i++)
            total += values[i];
        return total;
    }

    public int WithOptional(int value, int extra = 10) => value + extra;

    public string Normalize(string input) => input.Trim().ToUpperInvariant();
}
