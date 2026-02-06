using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace CsEval.Benchmarks;

/// <summary>
/// Measures cold start performance (parse + evaluate from scratch).
/// This is the typical first-time evaluation scenario.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ColdStartBenchmarks : BenchmarkBase
{
    private const string Simple = "1 + 2 * 3";

    private const string Loop = """
        {
            var sum = 0;
            for (var i = 0; i < 100; i++) {
                sum += i;
            }
            return sum;
        }
        """;

    private const string Linq = """
        {
            var items = new List<int> { 1, 2, 3, 4, 5 };
            return items.Where(x => x > 2).Sum();
        }
        """;

    // CsEval version for LINQ (uses CsEval list syntax)
    private const string CsEvalLinq = """
        {
            var items = [1, 2, 3, 4, 5];
            return items.Where(x => x > 2).Sum();
        }
        """;

    [Benchmark]
    public object Interpreted_Simple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.Interpreted });
        return engine.Evaluate(Simple)!;
    }

    [Benchmark]
    public object Compiled_Simple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.StrictCompiled });
        return engine.Evaluate(Simple)!;
    }

    [Benchmark(Baseline = true)]
    public object Roslyn_Simple()
    {
        return CSharpScript.EvaluateAsync<object>(Simple, RoslynOptions).Result!;
    }

    [Benchmark]
    public object Interpreted_Loop()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.Interpreted });
        return engine.Evaluate(Loop)!;
    }

    [Benchmark]
    public object Compiled_Loop()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.StrictCompiled });
        return engine.Evaluate(Loop)!;
    }

    [Benchmark]
    public object Roslyn_Loop()
    {
        return CSharpScript.EvaluateAsync<object>(Loop, RoslynOptions).Result!;
    }

    [Benchmark]
    public object Interpreted_Linq()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.Interpreted });
        return engine.Evaluate(CsEvalLinq)!;
    }

    [Benchmark]
    public object Compiled_Linq()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.StrictCompiled });
        return engine.Evaluate(CsEvalLinq)!;
    }

    [Benchmark]
    public object Roslyn_Linq()
    {
        return CSharpScript.EvaluateAsync<object>(Linq, RoslynOptions).Result!;
    }
}
