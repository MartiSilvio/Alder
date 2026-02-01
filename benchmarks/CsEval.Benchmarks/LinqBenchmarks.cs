using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class LinqBenchmarks : BenchmarkBase
{
    private const string RoslynSetup = "var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };";

    private const string Where = "items.Where(x => x % 2 == 0).ToList()";
    private const string Select = "items.Select(x => x * 2).ToList()";
    private const string Sum = "items.Sum()";
    private const string Chain = "items.Where(x => x % 2 == 0).Select(x => x * 2).Sum()";
    private const string OrderBy = "items.OrderByDescending(x => x).ToList()";
    private const string First = "items.First(x => x > 5)";
    private const string Any = "items.Any(x => x > 5)";
    private const string Count = "items.Count(x => x % 2 == 0)";

    private List<int> _items = null!;

    private CsEvalExpression _interpreted_Where = null!;
    private CsEvalExpression _interpreted_Select = null!;
    private CsEvalExpression _interpreted_Sum = null!;
    private CsEvalExpression _interpreted_Chain = null!;
    private CsEvalExpression _interpreted_OrderBy = null!;
    private CsEvalExpression _interpreted_First = null!;
    private CsEvalExpression _interpreted_Any = null!;
    private CsEvalExpression _interpreted_Count = null!;

    private CsEvalExpression _compiled_Where = null!;
    private CsEvalExpression _compiled_Select = null!;
    private CsEvalExpression _compiled_Sum = null!;
    private CsEvalExpression _compiled_Chain = null!;
    private CsEvalExpression _compiled_OrderBy = null!;
    private CsEvalExpression _compiled_First = null!;
    private CsEvalExpression _compiled_Any = null!;
    private CsEvalExpression _compiled_Count = null!;

    private Script<object> _roslyn_Where = null!;
    private Script<object> _roslyn_Select = null!;
    private Script<object> _roslyn_Sum = null!;
    private Script<object> _roslyn_Chain = null!;
    private Script<object> _roslyn_OrderBy = null!;
    private Script<object> _roslyn_First = null!;
    private Script<object> _roslyn_Any = null!;
    private Script<object> _roslyn_Count = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines();

        _items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        InterpretedEngine.SetVariable("items", _items);
        CompiledEngine.SetVariable("items", _items);

        _interpreted_Where = InterpretedEngine.Parse(Where);
        _interpreted_Select = InterpretedEngine.Parse(Select);
        _interpreted_Sum = InterpretedEngine.Parse(Sum);
        _interpreted_Chain = InterpretedEngine.Parse(Chain);
        _interpreted_OrderBy = InterpretedEngine.Parse(OrderBy);
        _interpreted_First = InterpretedEngine.Parse(First);
        _interpreted_Any = InterpretedEngine.Parse(Any);
        _interpreted_Count = InterpretedEngine.Parse(Count);

        _compiled_Where = CompiledEngine.Parse(Where);
        _compiled_Select = CompiledEngine.Parse(Select);
        _compiled_Sum = CompiledEngine.Parse(Sum);
        _compiled_Chain = CompiledEngine.Parse(Chain);
        _compiled_OrderBy = CompiledEngine.Parse(OrderBy);
        _compiled_First = CompiledEngine.Parse(First);
        _compiled_Any = CompiledEngine.Parse(Any);
        _compiled_Count = CompiledEngine.Parse(Count);

        _roslyn_Where = CreateRoslynScript($"{RoslynSetup} {Where}");
        _roslyn_Select = CreateRoslynScript($"{RoslynSetup} {Select}");
        _roslyn_Sum = CreateRoslynScript($"{RoslynSetup} {Sum}");
        _roslyn_Chain = CreateRoslynScript($"{RoslynSetup} {Chain}");
        _roslyn_OrderBy = CreateRoslynScript($"{RoslynSetup} {OrderBy}");
        _roslyn_First = CreateRoslynScript($"{RoslynSetup} {First}");
        _roslyn_Any = CreateRoslynScript($"{RoslynSetup} {Any}");
        _roslyn_Count = CreateRoslynScript($"{RoslynSetup} {Count}");

        // Warm up Roslyn
        _roslyn_Where.RunAsync().Wait();
        _roslyn_Select.RunAsync().Wait();
        _roslyn_Sum.RunAsync().Wait();
        _roslyn_Chain.RunAsync().Wait();
        _roslyn_OrderBy.RunAsync().Wait();
        _roslyn_First.RunAsync().Wait();
        _roslyn_Any.RunAsync().Wait();
        _roslyn_Count.RunAsync().Wait();
    }

    [Benchmark]
    public object Interpreted_Where() => InterpretedEngine.Evaluate(_interpreted_Where)!;

    [Benchmark]
    public object Compiled_Where() => CompiledEngine.Evaluate(_compiled_Where)!;

    [Benchmark(Baseline = true)]
    public object Roslyn_Where() => _roslyn_Where.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Select() => InterpretedEngine.Evaluate(_interpreted_Select)!;

    [Benchmark]
    public object Compiled_Select() => CompiledEngine.Evaluate(_compiled_Select)!;

    [Benchmark]
    public object Roslyn_Select() => _roslyn_Select.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Sum() => InterpretedEngine.Evaluate(_interpreted_Sum)!;

    [Benchmark]
    public object Compiled_Sum() => CompiledEngine.Evaluate(_compiled_Sum)!;

    [Benchmark]
    public object Roslyn_Sum() => _roslyn_Sum.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Chain() => InterpretedEngine.Evaluate(_interpreted_Chain)!;

    [Benchmark]
    public object Compiled_Chain() => CompiledEngine.Evaluate(_compiled_Chain)!;

    [Benchmark]
    public object Roslyn_Chain() => _roslyn_Chain.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_OrderBy() => InterpretedEngine.Evaluate(_interpreted_OrderBy)!;

    [Benchmark]
    public object Compiled_OrderBy() => CompiledEngine.Evaluate(_compiled_OrderBy)!;

    [Benchmark]
    public object Roslyn_OrderBy() => _roslyn_OrderBy.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_First() => InterpretedEngine.Evaluate(_interpreted_First)!;

    [Benchmark]
    public object Compiled_First() => CompiledEngine.Evaluate(_compiled_First)!;

    [Benchmark]
    public object Roslyn_First() => _roslyn_First.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Any() => InterpretedEngine.Evaluate(_interpreted_Any)!;

    [Benchmark]
    public object Compiled_Any() => CompiledEngine.Evaluate(_compiled_Any)!;

    [Benchmark]
    public object Roslyn_Any() => _roslyn_Any.RunAsync().Result.ReturnValue!;

    [Benchmark]
    public object Interpreted_Count() => InterpretedEngine.Evaluate(_interpreted_Count)!;

    [Benchmark]
    public object Compiled_Count() => CompiledEngine.Evaluate(_compiled_Count)!;

    [Benchmark]
    public object Roslyn_Count() => _roslyn_Count.RunAsync().Result.ReturnValue!;
}
