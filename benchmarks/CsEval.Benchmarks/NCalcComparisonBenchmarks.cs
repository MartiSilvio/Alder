using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NCalc;

namespace CsEval.Benchmarks;

/// <summary>
/// Benchmarks comparing CsEval vs NCalc performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class NCalcComparisonBenchmarks
{
    // Pre-parsed expressions
    private CsEvalExpression _csEval_SimpleArithmetic = null!;
    private CsEvalExpression _csEval_WithVariables = null!;
    private CsEvalExpression _csEval_Ternary = null!;
    private CsEvalExpression _csEval_Compiled_SimpleArithmetic = null!;
    private CsEvalExpression _csEval_Compiled_WithVariables = null!;
    private CsEvalExpression _csEval_Compiled_Ternary = null!;

    private CsEvalEngine _csEvalEngine = null!;
    private CsEvalEngine _csEvalCompiledEngine = null!;

    // NCalc expressions (pre-parsed)
    private Expression _ncalc_SimpleArithmetic = null!;
    private Expression _ncalc_WithVariables = null!;
    private Expression _ncalc_Ternary = null!;

    [GlobalSetup]
    public void Setup()
    {
        // CsEval setup (tree-walking)
        _csEvalEngine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Disabled });
        _csEvalEngine.SetVariable("x", 10L);
        _csEvalEngine.SetVariable("y", 20L);
        _csEvalEngine.SetVariable("z", 30L);

        _csEval_SimpleArithmetic = _csEvalEngine.Parse("1 + 2 * 3");
        _csEval_WithVariables = _csEvalEngine.Parse("x + y * z");
        _csEval_Ternary = _csEvalEngine.Parse("x > 5 ? x * 2 : x + 10");

        // CsEval setup (compiled)
        _csEvalCompiledEngine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Eager });
        _csEvalCompiledEngine.SetVariable("x", 10L);
        _csEvalCompiledEngine.SetVariable("y", 20L);
        _csEvalCompiledEngine.SetVariable("z", 30L);

        _csEval_Compiled_SimpleArithmetic = _csEvalCompiledEngine.Parse("1 + 2 * 3");
        _csEval_Compiled_WithVariables = _csEvalCompiledEngine.Parse("x + y * z");
        _csEval_Compiled_Ternary = _csEvalCompiledEngine.Parse("x > 5 ? x * 2 : x + 10");

        // NCalc setup (pre-parsed)
        _ncalc_SimpleArithmetic = new Expression("1 + 2 * 3");
        _ncalc_WithVariables = new Expression("[x] + [y] * [z]");
        _ncalc_Ternary = new Expression("if([x] > 5, [x] * 2, [x] + 10)");
    }

    #region Simple Arithmetic (1 + 2 * 3)

    [Benchmark(Baseline = true)]
    public object CsEval_TreeWalk_SimpleArithmetic() => _csEvalEngine.Evaluate(_csEval_SimpleArithmetic)!;

    [Benchmark]
    public object CsEval_Compiled_SimpleArithmetic() => _csEvalCompiledEngine.Evaluate(_csEval_Compiled_SimpleArithmetic)!;

    [Benchmark]
    public object NCalc_SimpleArithmetic() => _ncalc_SimpleArithmetic.Evaluate()!;

    #endregion

    #region With Variables (x + y * z)

    [Benchmark]
    public object CsEval_TreeWalk_WithVariables() => _csEvalEngine.Evaluate(_csEval_WithVariables)!;

    [Benchmark]
    public object CsEval_Compiled_WithVariables() => _csEvalCompiledEngine.Evaluate(_csEval_Compiled_WithVariables)!;

    [Benchmark]
    public object NCalc_WithVariables()
    {
        _ncalc_WithVariables.Parameters["x"] = 10L;
        _ncalc_WithVariables.Parameters["y"] = 20L;
        _ncalc_WithVariables.Parameters["z"] = 30L;
        return _ncalc_WithVariables.Evaluate()!;
    }

    #endregion

    #region Ternary / Conditional

    [Benchmark]
    public object CsEval_TreeWalk_Ternary() => _csEvalEngine.Evaluate(_csEval_Ternary)!;

    [Benchmark]
    public object CsEval_Compiled_Ternary() => _csEvalCompiledEngine.Evaluate(_csEval_Compiled_Ternary)!;

    [Benchmark]
    public object NCalc_Ternary()
    {
        _ncalc_Ternary.Parameters["x"] = 10L;
        return _ncalc_Ternary.Evaluate()!;
    }

    #endregion

    #region Parse + Evaluate (cold path)

    [Benchmark]
    public object CsEval_ParseAndEvaluate_Simple()
    {
        var engine = new CsEvalEngine();
        return engine.Evaluate("1 + 2 * 3")!;
    }

    [Benchmark]
    public object NCalc_ParseAndEvaluate_Simple()
    {
        var expr = new Expression("1 + 2 * 3");
        return expr.Evaluate()!;
    }

    #endregion
}
