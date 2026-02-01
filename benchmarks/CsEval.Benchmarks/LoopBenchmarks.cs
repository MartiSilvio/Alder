using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class LoopBenchmarks : BenchmarkBase
{
    private const string ForLoop = """
        {
            var sum = 0;
            for (var i = 0; i < 100; i++) {
                sum += i;
            }
            return sum;
        }
        """;

    private const string WhileLoop = """
        {
            var sum = 0;
            var i = 0;
            while (i < 100) {
                sum += i;
                i++;
            }
            return sum;
        }
        """;

    private const string DoWhileLoop = """
        {
            var sum = 0;
            var i = 0;
            do {
                sum += i;
                i++;
            } while (i < 100);
            return sum;
        }
        """;

    private const string NestedLoop = """
        {
            var sum = 0;
            for (var i = 0; i < 10; i++) {
                for (var j = 0; j < 10; j++) {
                    sum += 1;
                }
            }
            return sum;
        }
        """;

    private CsEvalExpression _interpreted_For = null!;
    private CsEvalExpression _interpreted_While = null!;
    private CsEvalExpression _interpreted_DoWhile = null!;
    private CsEvalExpression _interpreted_Nested = null!;

    private CsEvalExpression _compiled_For = null!;
    private CsEvalExpression _compiled_While = null!;
    private CsEvalExpression _compiled_DoWhile = null!;
    private CsEvalExpression _compiled_Nested = null!;

    private Script<object> _roslyn_For = null!;
    private Script<object> _roslyn_While = null!;
    private Script<object> _roslyn_DoWhile = null!;
    private Script<object> _roslyn_Nested = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines();

        _interpreted_For = InterpretedEngine.Parse(ForLoop);
        _interpreted_While = InterpretedEngine.Parse(WhileLoop);
        _interpreted_DoWhile = InterpretedEngine.Parse(DoWhileLoop);
        _interpreted_Nested = InterpretedEngine.Parse(NestedLoop);

        _compiled_For = CompiledEngine.Parse(ForLoop);
        _compiled_While = CompiledEngine.Parse(WhileLoop);
        _compiled_DoWhile = CompiledEngine.Parse(DoWhileLoop);
        _compiled_Nested = CompiledEngine.Parse(NestedLoop);

        _roslyn_For = CreateRoslynScript(ForLoop);
        _roslyn_While = CreateRoslynScript(WhileLoop);
        _roslyn_DoWhile = CreateRoslynScript(DoWhileLoop);
        _roslyn_Nested = CreateRoslynScript(NestedLoop);

        // Warm up Roslyn
        _roslyn_For.RunAsync().Wait();
        _roslyn_While.RunAsync().Wait();
        _roslyn_DoWhile.RunAsync().Wait();
        _roslyn_Nested.RunAsync().Wait();
    }

    [Benchmark]
    public object Interpreted_ForLoop() => InterpretedEngine.Evaluate(_interpreted_For)!;

    [Benchmark]
    public object Compiled_ForLoop() => CompiledEngine.Evaluate(_compiled_For)!;

    [Benchmark(Baseline = true)]
    public object Roslyn_ForLoop() => _roslyn_For.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_WhileLoop() => InterpretedEngine.Evaluate(_interpreted_While)!;

    [Benchmark]
    public object Compiled_WhileLoop() => CompiledEngine.Evaluate(_compiled_While)!;

    [Benchmark]
    public object Roslyn_WhileLoop() => _roslyn_While.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_DoWhileLoop() => InterpretedEngine.Evaluate(_interpreted_DoWhile)!;

    [Benchmark]
    public object Compiled_DoWhileLoop() => CompiledEngine.Evaluate(_compiled_DoWhile)!;

    [Benchmark]
    public object Roslyn_DoWhileLoop() => _roslyn_DoWhile.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_NestedLoop() => InterpretedEngine.Evaluate(_interpreted_Nested)!;

    [Benchmark]
    public object Compiled_NestedLoop() => CompiledEngine.Evaluate(_compiled_Nested)!;

    [Benchmark]
    public object Roslyn_NestedLoop() => _roslyn_Nested.RunAsync().Result.ReturnValue!;
}
