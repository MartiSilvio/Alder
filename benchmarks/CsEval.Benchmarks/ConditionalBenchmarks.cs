using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ConditionalBenchmarks : BenchmarkBase
{
    private const string Ternary = "10 > 5 ? 10 * 2 : 10 + 10";
    private const string NestedTernary = "10 > 20 ? 1 : 10 > 5 ? 2 : 3";
    private const string Comparison = "10 < 20 && 20 < 30 || 5 == 5";
    private const string NullCoalesce = "(int?)null ?? (int?)null ?? 42";

    private const string IfElse = """
        {
            var x = 10;
            if (x > 5) {
                return x * 2;
            } else {
                return x + 10;
            }
        }
        """;

    private const string IfElseIf = """
        {
            var x = 50;
            if (x > 100) {
                return "large";
            } else if (x > 50) {
                return "medium";
            } else {
                return "small";
            }
        }
        """;

    private const string Switch = """
        {
            var x = 2;
            switch (x) {
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                default: return "other";
            }
        }
        """;

    private CsEvalExpression _interpreted_Ternary = null!;
    private CsEvalExpression _interpreted_NestedTernary = null!;
    private CsEvalExpression _interpreted_Comparison = null!;
    private CsEvalExpression _interpreted_NullCoalesce = null!;
    private CsEvalExpression _interpreted_IfElse = null!;
    private CsEvalExpression _interpreted_IfElseIf = null!;
    private CsEvalExpression _interpreted_Switch = null!;

    private CsEvalExpression _compiled_Ternary = null!;
    private CsEvalExpression _compiled_NestedTernary = null!;
    private CsEvalExpression _compiled_Comparison = null!;
    private CsEvalExpression _compiled_NullCoalesce = null!;
    private CsEvalExpression _compiled_IfElse = null!;
    private CsEvalExpression _compiled_IfElseIf = null!;
    private CsEvalExpression _compiled_Switch = null!;

    private Script<object> _roslyn_Ternary = null!;
    private Script<object> _roslyn_NestedTernary = null!;
    private Script<object> _roslyn_Comparison = null!;
    private Script<object> _roslyn_NullCoalesce = null!;
    private Script<object> _roslyn_IfElse = null!;
    private Script<object> _roslyn_IfElseIf = null!;
    private Script<object> _roslyn_Switch = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines();

        _interpreted_Ternary = InterpretedEngine.Parse(Ternary);
        _interpreted_NestedTernary = InterpretedEngine.Parse(NestedTernary);
        _interpreted_Comparison = InterpretedEngine.Parse(Comparison);
        _interpreted_NullCoalesce = InterpretedEngine.Parse(NullCoalesce);
        _interpreted_IfElse = InterpretedEngine.Parse(IfElse);
        _interpreted_IfElseIf = InterpretedEngine.Parse(IfElseIf);
        _interpreted_Switch = InterpretedEngine.Parse(Switch);

        _compiled_Ternary = CompiledEngine.Parse(Ternary);
        _compiled_NestedTernary = CompiledEngine.Parse(NestedTernary);
        _compiled_Comparison = CompiledEngine.Parse(Comparison);
        _compiled_NullCoalesce = CompiledEngine.Parse(NullCoalesce);
        _compiled_IfElse = CompiledEngine.Parse(IfElse);
        _compiled_IfElseIf = CompiledEngine.Parse(IfElseIf);
        _compiled_Switch = CompiledEngine.Parse(Switch);

        _roslyn_Ternary = CreateRoslynScript(Ternary);
        _roslyn_NestedTernary = CreateRoslynScript(NestedTernary);
        _roslyn_Comparison = CreateRoslynScript(Comparison);
        _roslyn_NullCoalesce = CreateRoslynScript(NullCoalesce);
        _roslyn_IfElse = CreateRoslynScript(IfElse);
        _roslyn_IfElseIf = CreateRoslynScript(IfElseIf);
        _roslyn_Switch = CreateRoslynScript(Switch);

        // Warm up Roslyn
        _roslyn_Ternary.RunAsync().Wait();
        _roslyn_NestedTernary.RunAsync().Wait();
        _roslyn_Comparison.RunAsync().Wait();
        _roslyn_NullCoalesce.RunAsync().Wait();
        _roslyn_IfElse.RunAsync().Wait();
        _roslyn_IfElseIf.RunAsync().Wait();
        _roslyn_Switch.RunAsync().Wait();
    }

    [Benchmark]
    public object Interpreted_Ternary() => InterpretedEngine.Evaluate(_interpreted_Ternary)!;

    [Benchmark]
    public object Compiled_Ternary() => CompiledEngine.Evaluate(_compiled_Ternary)!;

    [Benchmark(Baseline = true)]
    public object Roslyn_Ternary() => _roslyn_Ternary.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_NestedTernary() => InterpretedEngine.Evaluate(_interpreted_NestedTernary)!;

    [Benchmark]
    public object Compiled_NestedTernary() => CompiledEngine.Evaluate(_compiled_NestedTernary)!;

    [Benchmark]
    public object Roslyn_NestedTernary() => _roslyn_NestedTernary.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Comparison() => InterpretedEngine.Evaluate(_interpreted_Comparison)!;

    [Benchmark]
    public object Compiled_Comparison() => CompiledEngine.Evaluate(_compiled_Comparison)!;

    [Benchmark]
    public object Roslyn_Comparison() => _roslyn_Comparison.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_NullCoalesce() => InterpretedEngine.Evaluate(_interpreted_NullCoalesce)!;

    [Benchmark]
    public object Compiled_NullCoalesce() => CompiledEngine.Evaluate(_compiled_NullCoalesce)!;

    [Benchmark]
    public object Roslyn_NullCoalesce() => _roslyn_NullCoalesce.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_IfElse() => InterpretedEngine.Evaluate(_interpreted_IfElse)!;

    [Benchmark]
    public object Compiled_IfElse() => CompiledEngine.Evaluate(_compiled_IfElse)!;

    [Benchmark]
    public object Roslyn_IfElse() => _roslyn_IfElse.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_IfElseIf() => InterpretedEngine.Evaluate(_interpreted_IfElseIf)!;

    [Benchmark]
    public object Compiled_IfElseIf() => CompiledEngine.Evaluate(_compiled_IfElseIf)!;

    [Benchmark]
    public object Roslyn_IfElseIf() => _roslyn_IfElseIf.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Switch() => InterpretedEngine.Evaluate(_interpreted_Switch)!;

    [Benchmark]
    public object Compiled_Switch() => CompiledEngine.Evaluate(_compiled_Switch)!;

    [Benchmark]
    public object Roslyn_Switch() => _roslyn_Switch.RunAsync().Result.ReturnValue!;
}
