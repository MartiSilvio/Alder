using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CsEval.Benchmarks;

/// <summary>
/// Benchmarks for IL-compiled expression evaluation.
/// Measures the performance of various expression types.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CompilationBenchmarks
{
    private CsEvalEngine _engine = null!;

    private CsEvalExpression _simpleArithmetic = null!;
    private CsEvalExpression _withVariables = null!;
    private CsEvalExpression _ternary = null!;
    private CsEvalExpression _propertyAccess = null!;
    private CsEvalExpression _comparison = null!;
    private CsEvalExpression _nullCoalesce = null!;
    private CsEvalExpression _whileLoop = null!;
    private CsEvalExpression _forLoop = null!;

    public class TestPerson
    {
        public string Name { get; set; } = "Alice";
        public int Age { get; set; } = 30;
    }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();
        _engine.SetVariable("x", 10L);
        _engine.SetVariable("y", 20L);
        _engine.SetVariable("z", 30L);
        _engine.SetVariable("a", null);
        _engine.SetVariable("b", "fallback");
        _engine.SetVariable("person", new TestPerson());

        // All expressions are automatically IL-compiled on Parse()
        _simpleArithmetic = _engine.Parse("1 + 2 * 3");
        _withVariables = _engine.Parse("x + y * z");
        _ternary = _engine.Parse("x > 5 ? x * 2 : x + 10");
        _propertyAccess = _engine.Parse("person.Name");
        _comparison = _engine.Parse("x < y && y < z");
        _nullCoalesce = _engine.Parse("a ?? b");
        _whileLoop = _engine.Parse("{ var i = 0; while (i < 100) { i = i + 1; } return i; }");
        _forLoop = _engine.Parse("{ var sum = 0; for (var i = 0; i < 100; i = i + 1) { sum = sum + i; } return sum; }");
    }

    #region Simple Expressions

    [Benchmark(Baseline = true)]
    public object Evaluate_SimpleArithmetic() => _engine.Evaluate(_simpleArithmetic)!;

    [Benchmark]
    public object Evaluate_WithVariables() => _engine.Evaluate(_withVariables)!;

    [Benchmark]
    public object Evaluate_Ternary() => _engine.Evaluate(_ternary)!;

    [Benchmark]
    public object Evaluate_PropertyAccess() => _engine.Evaluate(_propertyAccess)!;

    [Benchmark]
    public object Evaluate_Comparison() => _engine.Evaluate(_comparison)!;

    [Benchmark]
    public object Evaluate_NullCoalesce() => _engine.Evaluate(_nullCoalesce)!;

    #endregion

    #region Control Flow (IL-compiled loops)

    [Benchmark]
    public object Evaluate_WhileLoop_100Iterations() => _engine.Evaluate(_whileLoop)!;

    [Benchmark]
    public object Evaluate_ForLoop_100Iterations() => _engine.Evaluate(_forLoop)!;

    #endregion

    #region Parse + Compile Time

    [Benchmark]
    public CsEvalExpression ParseAndCompile_SimpleArithmetic()
    {
        var engine = new CsEvalEngine();
        return engine.Parse("1 + 2 * 3");
    }

    [Benchmark]
    public CsEvalExpression ParseAndCompile_WithVariables()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 10L);
        engine.SetVariable("y", 20L);
        engine.SetVariable("z", 30L);
        return engine.Parse("x + y * z");
    }

    [Benchmark]
    public CsEvalExpression ParseAndCompile_Loop()
    {
        var engine = new CsEvalEngine();
        return engine.Parse("{ var i = 0; while (i < 100) { i = i + 1; } return i; }");
    }

    #endregion
}
