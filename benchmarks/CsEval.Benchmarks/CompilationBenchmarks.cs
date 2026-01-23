using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CsEval.Benchmarks;

/// <summary>
/// Benchmarks comparing compiled vs tree-walking evaluation.
/// Demonstrates the performance benefit of expression compilation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CompilationBenchmarks
{
    private CsEvalEngine _treeWalkEngine = null!;
    private CsEvalEngine _compiledEngine = null!;

    private CsEvalExpression _simpleArithmetic_TreeWalk = null!;
    private CsEvalExpression _simpleArithmetic_Compiled = null!;
    private CsEvalExpression _withVariables_TreeWalk = null!;
    private CsEvalExpression _withVariables_Compiled = null!;
    private CsEvalExpression _ternary_TreeWalk = null!;
    private CsEvalExpression _ternary_Compiled = null!;
    private CsEvalExpression _propertyAccess_TreeWalk = null!;
    private CsEvalExpression _propertyAccess_Compiled = null!;
    private CsEvalExpression _comparison_TreeWalk = null!;
    private CsEvalExpression _comparison_Compiled = null!;
    private CsEvalExpression _nullCoalesce_TreeWalk = null!;
    private CsEvalExpression _nullCoalesce_Compiled = null!;

    public class TestPerson
    {
        public string Name { get; set; } = "Alice";
        public int Age { get; set; } = 30;
    }

    [GlobalSetup]
    public void Setup()
    {
        // Tree-walk engine (Disabled mode)
        _treeWalkEngine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Disabled });
        _treeWalkEngine.SetVariable("x", 10L);
        _treeWalkEngine.SetVariable("y", 20L);
        _treeWalkEngine.SetVariable("z", 30L);
        _treeWalkEngine.SetVariable("a", null);
        _treeWalkEngine.SetVariable("b", "fallback");
        _treeWalkEngine.SetVariable("person", new TestPerson());

        // Compiled engine (Eager mode)
        _compiledEngine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Eager });
        _compiledEngine.SetVariable("x", 10L);
        _compiledEngine.SetVariable("y", 20L);
        _compiledEngine.SetVariable("z", 30L);
        _compiledEngine.SetVariable("a", null);
        _compiledEngine.SetVariable("b", "fallback");
        _compiledEngine.SetVariable("person", new TestPerson());

        // Parse expressions
        const string simpleArithmetic = "1 + 2 * 3";
        const string withVariables = "x + y * z";
        const string ternary = "x > 5 ? x * 2 : x + 10";
        const string propertyAccess = "person.Name";
        const string comparison = "x < y && y < z";
        const string nullCoalesce = "a ?? b";

        // Tree-walk expressions
        _simpleArithmetic_TreeWalk = _treeWalkEngine.Parse(simpleArithmetic);
        _withVariables_TreeWalk = _treeWalkEngine.Parse(withVariables);
        _ternary_TreeWalk = _treeWalkEngine.Parse(ternary);
        _propertyAccess_TreeWalk = _treeWalkEngine.Parse(propertyAccess);
        _comparison_TreeWalk = _treeWalkEngine.Parse(comparison);
        _nullCoalesce_TreeWalk = _treeWalkEngine.Parse(nullCoalesce);

        // Compiled expressions (Eager mode compiles on Parse)
        _simpleArithmetic_Compiled = _compiledEngine.Parse(simpleArithmetic);
        _withVariables_Compiled = _compiledEngine.Parse(withVariables);
        _ternary_Compiled = _compiledEngine.Parse(ternary);
        _propertyAccess_Compiled = _compiledEngine.Parse(propertyAccess);
        _comparison_Compiled = _compiledEngine.Parse(comparison);
        _nullCoalesce_Compiled = _compiledEngine.Parse(nullCoalesce);
    }

    #region Simple Arithmetic (1 + 2 * 3)

    [Benchmark]
    public object Evaluate_TreeWalk_SimpleArithmetic() => _treeWalkEngine.Evaluate(_simpleArithmetic_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_SimpleArithmetic() => _compiledEngine.Evaluate(_simpleArithmetic_Compiled)!;

    #endregion

    #region With Variables (x + y * z)

    [Benchmark]
    public object Evaluate_TreeWalk_WithVariables() => _treeWalkEngine.Evaluate(_withVariables_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_WithVariables() => _compiledEngine.Evaluate(_withVariables_Compiled)!;

    #endregion

    #region Ternary (x > 5 ? x * 2 : x + 10)

    [Benchmark]
    public object Evaluate_TreeWalk_Ternary() => _treeWalkEngine.Evaluate(_ternary_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_Ternary() => _compiledEngine.Evaluate(_ternary_Compiled)!;

    #endregion

    #region Property Access (person.Name)

    [Benchmark]
    public object Evaluate_TreeWalk_PropertyAccess() => _treeWalkEngine.Evaluate(_propertyAccess_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_PropertyAccess() => _compiledEngine.Evaluate(_propertyAccess_Compiled)!;

    #endregion

    #region Comparison (x < y && y < z)

    [Benchmark]
    public object Evaluate_TreeWalk_Comparison() => _treeWalkEngine.Evaluate(_comparison_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_Comparison() => _compiledEngine.Evaluate(_comparison_Compiled)!;

    #endregion

    #region Null Coalesce (a ?? b)

    [Benchmark]
    public object Evaluate_TreeWalk_NullCoalesce() => _treeWalkEngine.Evaluate(_nullCoalesce_TreeWalk)!;

    [Benchmark]
    public object Evaluate_Compiled_NullCoalesce() => _compiledEngine.Evaluate(_nullCoalesce_Compiled)!;

    #endregion

    #region Compile Time (measures compilation overhead)

    [Benchmark]
    public void CompileTime_SimpleArithmetic()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("1 + 2 * 3");
        expr.Compile();
    }

    [Benchmark]
    public void CompileTime_WithVariables()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 10L);
        engine.SetVariable("y", 20L);
        engine.SetVariable("z", 30L);
        var expr = engine.Parse("x + y * z");
        expr.Compile();
    }

    [Benchmark]
    public void CompileTime_Ternary()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 10L);
        var expr = engine.Parse("x > 5 ? x * 2 : x + 10");
        expr.Compile();
    }

    #endregion
}
