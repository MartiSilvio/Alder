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
    private Func<object?> _compiledFunc = null!;
    private CompiledExpressionFastDelegate _compiledFastDelegate = null!;
    private CompiledExpressionDelegate _compiledStandardDelegate = null!;
    private CsEvalContext _compiledContext = null!;
    private CsEvalOptions _compiledOptions = null!;
    private IDynamicExpression _fleeExpression = null!;

    private const string ExpressionText = "1 + 2 * 3 - 4 / 2";

    [GlobalSetup]
    public void Setup()
    {
        SetupEngines(_globals);
        _compiledExpression = CompiledEngine.ParseAndCompile(ExpressionText);
        _compiledFunc = CompiledEngine.CompileToFunc<object?>(ExpressionText);
        var compiledInfo = _compiledExpression.GetCompiledInfo()!;
        _compiledFastDelegate = compiledInfo.FastDelegate!;
        _compiledStandardDelegate = compiledInfo.Delegate!;
        _compiledContext = CompiledEngine.GetContextForCompiled();
        _compiledOptions = compiledInfo.FastDelegateOptions ?? CsEvalOptions.Default;

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
    public object CsEval_CompiledFunc() => _compiledFunc()!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectFastDelegate() => _compiledFastDelegate(_compiledContext)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object CsEval_DirectStandardDelegate() => _compiledStandardDelegate(_compiledContext, _compiledOptions, default)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Flee() => _fleeExpression.Evaluate()!;
}
