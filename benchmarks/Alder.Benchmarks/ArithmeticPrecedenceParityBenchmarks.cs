using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Compiled;
using Alder.Runtime;
using Flee.PublicTypes;

namespace Alder.Benchmarks;

[Config(typeof(BenchmarkSuiteConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ArithmeticPrecedenceParityBenchmarks : BenchmarkBase
{
    private readonly BenchmarkGlobalData _globals = BenchmarkGlobalData.CreateDefault();
    private AlderExpression _compiledExpression = null!;
    private AlderExpression _compiledFecExpression = null!;
    private Func<object?> _compiledFunc = null!;
    private Func<object?> _compiledFecFunc = null!;
    private CompiledExpressionDelegate _compiledStandardDelegate = null!;
    private CompiledExpressionDelegate _compiledFecStandardDelegate = null!;
    private AlderContext _compiledContext = null!;
    private AlderContext _compiledFecContext = null!;
    private AlderConfig _compiledOptions = null!;
    private AlderConfig _compiledFecOptions = null!;
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
        _compiledStandardDelegate = compiledInfo.Delegate!;
        _compiledContext = CompiledEngine.GetContextForCompiled();
        _compiledOptions = CompiledEngine.GetCompiledFeatureAccess().Config;
        var compiledFecInfo = _compiledFecExpression.GetCompiledInfo()!;
        _compiledFecStandardDelegate = compiledFecInfo.Delegate!;
        _compiledFecContext = CompiledFecEngine.GetContextForCompiled();
        _compiledFecOptions = CompiledFecEngine.GetCompiledFeatureAccess().Config;

        var fleeContext = CreateFleeContext(_globals);
        _fleeExpression = fleeContext.CompileDynamic(ExpressionText);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object NativeDelegate_Baseline() => 5;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_Compiled() => CompiledEngine.Evaluate(_compiledExpression)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_CompiledFec() => CompiledFecEngine.Evaluate(_compiledFecExpression)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_CompiledFunc() => _compiledFunc()!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_CompiledFecFunc() => _compiledFecFunc()!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_DirectStandardDelegate() => _compiledStandardDelegate(_compiledContext, _compiledOptions, new ExecutionConstraintState(), default)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Alder_DirectFecStandardDelegate() => _compiledFecStandardDelegate(_compiledFecContext, _compiledFecOptions, new ExecutionConstraintState(), default)!;

    [Benchmark]
    [BenchmarkCategory("Arithmetic/Precedence")]
    public object Flee() => _fleeExpression.Evaluate()!;
}
