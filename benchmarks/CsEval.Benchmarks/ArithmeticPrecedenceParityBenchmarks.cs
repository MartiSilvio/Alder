using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CsEval.Compiled;
using CsEval.Runtime;
using Flee.PublicTypes;

namespace CsEval.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ArithmeticPrecedenceParityBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private CsEvalExpression _compiledExpression = null!;
    private CsEvalExpression _compiledFecExpression = null!;
    private Func<object?> _compiledFunc = null!;
    private Func<object?> _compiledFecFunc = null!;
    private CompiledExpressionFastDelegate _compiledFastDelegate = null!;
    private CompiledExpressionFastDelegate _compiledFecFastDelegate = null!;
    private CompiledExpressionDelegate _compiledStandardDelegate = null!;
    private CompiledExpressionDelegate _compiledFecStandardDelegate = null!;
    private CsEvalContext _compiledContext = null!;
    private CsEvalContext _compiledFecContext = null!;
    private CsEvalOptions _compiledOptions = null!;
    private CsEvalOptions _compiledFecOptions = null!;
    private IDynamicExpression _fleeExpression = null!;

    private const string ExpressionText = "1 + 2 * 3 - 4 / 2";

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _compiledExpression = CompiledEngine.ParseAndCompile(ExpressionText);
        _compiledFecExpression = CompiledFecEngine.ParseAndCompile(ExpressionText);
        _compiledFunc = CompiledEngine.CompileToFunc<object?>(ExpressionText);
        _compiledFecFunc = CompiledFecEngine.CompileToFunc<object?>(ExpressionText);
        var compiledInfo = _compiledExpression.GetCompiledInfo()!;
        _compiledFastDelegate = compiledInfo.FastDelegate!;
        _compiledStandardDelegate = compiledInfo.Delegate!;
        _compiledContext = CompiledEngine.GetContextForCompiled();
        _compiledOptions = compiledInfo.FastDelegateOptions ?? CsEvalOptions.Default;
        var compiledFecInfo = _compiledFecExpression.GetCompiledInfo()!;
        _compiledFecFastDelegate = compiledFecInfo.FastDelegate!;
        _compiledFecStandardDelegate = compiledFecInfo.Delegate!;
        _compiledFecContext = CompiledFecEngine.GetContextForCompiled();
        _compiledFecOptions = compiledFecInfo.FastDelegateOptions ?? CsEvalOptions.Default;

        var fleeContext = CreateFleeContext(_globals);
        _fleeExpression = fleeContext.CompileDynamic(ExpressionText);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object NativeDelegate_Baseline() => 5;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_CompiledFunc() => _compiledFunc()!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_CompiledFecFunc() => _compiledFecFunc()!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectFastDelegate() => _compiledFastDelegate(_compiledContext)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectFecFastDelegate() => _compiledFecFastDelegate(_compiledFecContext)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectStandardDelegate() => _compiledStandardDelegate(_compiledContext, _compiledOptions, default)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectFecStandardDelegate() => _compiledFecStandardDelegate(_compiledFecContext, _compiledFecOptions, default)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Flee() => _fleeExpression.Evaluate()!;
}
