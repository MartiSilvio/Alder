using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ArithmeticBenchmarks : BenchmarkBase
{
    private const string Simple = "1 + 2 * 3 - 4 / 2";
    private const string Complex = "((1 + 2) * (3 + 4) - (5 * 6)) / 2 + 10 % 3";
    private const string WithParens = "(10 + 20) * (30 - 15) / 5";

    private CsEvalExpression _interpreted_Simple = null!;
    private CsEvalExpression _interpreted_Complex = null!;
    private CsEvalExpression _interpreted_WithParens = null!;

    private CsEvalExpression _compiled_Simple = null!;
    private CsEvalExpression _compiled_Complex = null!;
    private CsEvalExpression _compiled_WithParens = null!;

    private Script<object> _roslyn_Simple = null!;
    private Script<object> _roslyn_Complex = null!;
    private Script<object> _roslyn_WithParens = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines();

        _interpreted_Simple = InterpretedEngine.Parse(Simple);
        _interpreted_Complex = InterpretedEngine.Parse(Complex);
        _interpreted_WithParens = InterpretedEngine.Parse(WithParens);

        _compiled_Simple = CompiledEngine.Parse(Simple);
        _compiled_Complex = CompiledEngine.Parse(Complex);
        _compiled_WithParens = CompiledEngine.Parse(WithParens);

        _roslyn_Simple = CreateRoslynScript(Simple);
        _roslyn_Complex = CreateRoslynScript(Complex);
        _roslyn_WithParens = CreateRoslynScript(WithParens);

        // Warm up Roslyn
        _roslyn_Simple.RunAsync().Wait();
        _roslyn_Complex.RunAsync().Wait();
        _roslyn_WithParens.RunAsync().Wait();
    }

    [Benchmark]
    public object Interpreted_Simple() => InterpretedEngine.Evaluate(_interpreted_Simple)!;

    [Benchmark]
    public object Compiled_Simple() => CompiledEngine.Evaluate(_compiled_Simple)!;

    [Benchmark(Baseline = true)]
    public object Roslyn_Simple() => _roslyn_Simple.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Complex() => InterpretedEngine.Evaluate(_interpreted_Complex)!;

    [Benchmark]
    public object Compiled_Complex() => CompiledEngine.Evaluate(_compiled_Complex)!;

    [Benchmark]
    public object Roslyn_Complex() => _roslyn_Complex.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_WithParens() => InterpretedEngine.Evaluate(_interpreted_WithParens)!;

    [Benchmark]
    public object Compiled_WithParens() => CompiledEngine.Evaluate(_compiled_WithParens)!;

    [Benchmark]
    public object Roslyn_WithParens() => _roslyn_WithParens.RunAsync().Result.ReturnValue!;
}
