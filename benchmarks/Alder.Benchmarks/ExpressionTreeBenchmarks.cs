using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Alder-specific expression-tree capability benchmark. This class is not a
/// cross-library head-to-head comparison; it measures the cost and execution
/// profile of Alder's typed expression-tree APIs against native compiled delegates.
///
/// Two benchmark categories:
///
///   Generation — cost of producing the expression tree from a string.
///     ParseAsExpression (tree only) vs CompileExpression (tree + Compile()).
///
///   CompiledExecution — warm-path cost of invoking a delegate compiled from
///     an expression tree vs engine.Evaluate.
/// </summary>
[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExpressionTreeBenchmarks : BenchmarkBase
{
    private BenchmarkData _data = null!;
    private AlderEngine _engine = null!;

    // Pre-compiled expression tree delegates
    private Func<int, bool> _compiledSimplePredicate = null!;
    private Func<int, int, bool> _compiledComplexPredicate = null!;
    private Func<string, bool> _compiledStringPredicate = null!;

    // Pre-compiled engine.Evaluate paths
    private AlderExpression _evalSimple = null!;
    private AlderExpression _evalComplex = null!;
    private AlderExpression _evalString = null!;

    private const string SimplePredicate = "x => x > 5";
    private const string ComplexPredicate = "(x, y) => x * x + y * y > 100 && x > 0";
    private const string StringPredicate = """s => s.Length > 3 && s.StartsWith("a")""";

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
        _engine = new AlderEngine(new AlderOptions().UseCompiler());
        BenchmarkBase.ApplyVariables(_engine, _data);

        // Compile expression tree delegates
        _compiledSimplePredicate = _engine.CompileExpression<Func<int, bool>>(SimplePredicate);
        _compiledComplexPredicate = _engine.CompileExpression<Func<int, int, bool>>(ComplexPredicate);
        _compiledStringPredicate = _engine.CompileExpression<Func<string, bool>>(StringPredicate);

        // Parse for engine.Evaluate comparison (equivalent inline expressions)
        _evalSimple = _engine.Parse("x > 5");
        _evalComplex = _engine.Parse("x * x + y * y > 100 && x > 0");
        _evalString = _engine.Parse("""text.Length > 3 && text.StartsWith("a")""");

        var simpleDelegate = _compiledSimplePredicate(_data.X);
        var simpleEngine = _engine.Evaluate(_evalSimple);
        if (!BenchmarkParityVerifier.AreEquivalent(simpleDelegate, simpleEngine))
            throw new InvalidOperationException(
                $"Simple predicate parity failure: delegate={simpleDelegate}, engine={simpleEngine}");

        var complexDelegate = _compiledComplexPredicate(_data.X, _data.Y);
        var complexEngine = _engine.Evaluate(_evalComplex);
        if (!BenchmarkParityVerifier.AreEquivalent(complexDelegate, complexEngine))
            throw new InvalidOperationException(
                $"Complex predicate parity failure: delegate={complexDelegate}, engine={complexEngine}");

        var stringDelegate = _compiledStringPredicate(_data.Text);
        var stringEngine = _engine.Evaluate(_evalString);
        if (!BenchmarkParityVerifier.AreEquivalent(stringDelegate, stringEngine))
            throw new InvalidOperationException(
                $"String predicate parity failure: delegate={stringDelegate}, engine={stringEngine}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine?.Dispose();
    }

    #region Generation — how fast can we produce expression trees?

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Capability/ExpressionTrees/Generation")]
    public Expression<Func<int, bool>> ParseAsExpression_Simple()
        => _engine.ParseAsExpression<Func<int, bool>>(SimplePredicate);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Generation")]
    public Expression<Func<int, int, bool>> ParseAsExpression_Complex()
        => _engine.ParseAsExpression<Func<int, int, bool>>(ComplexPredicate);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Generation")]
    public Expression<Func<string, bool>> ParseAsExpression_String()
        => _engine.ParseAsExpression<Func<string, bool>>(StringPredicate);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Generation")]
    public Func<int, bool> CompileExpression_Simple()
        => _engine.CompileExpression<Func<int, bool>>(SimplePredicate);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Generation")]
    public Func<int, int, bool> CompileExpression_Complex()
        => _engine.CompileExpression<Func<int, int, bool>>(ComplexPredicate);

    #endregion

    #region Compiled execution — expression tree delegate vs engine.Evaluate

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public bool ExprTree_Simple() => _compiledSimplePredicate(_data.X);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public object Engine_Simple() => _engine.Evaluate(_evalSimple)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public bool ExprTree_Complex() => _compiledComplexPredicate(_data.X, _data.Y);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public object Engine_Complex() => _engine.Evaluate(_evalComplex)!;

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public bool ExprTree_String() => _compiledStringPredicate(_data.Text);

    [Benchmark]
    [BenchmarkCategory("Capability/ExpressionTrees/Execution")]
    public object Engine_String() => _engine.Evaluate(_evalString)!;

    #endregion
}
