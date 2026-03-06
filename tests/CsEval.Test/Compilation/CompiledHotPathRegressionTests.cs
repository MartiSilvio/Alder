using CsEval.Diagnostics;
using System.Linq.Expressions;

namespace CsEval.Test.Compilation;

[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CompiledHotPathRegressionTests(CompilationMode mode)
{
    private sealed class CapturingExpressionCompiler : IExpressionCompiler
    {
        public LambdaExpression? LastExpression { get; private set; }

        public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
            where TDelegate : Delegate
        {
            LastExpression = expression;
            return expression.Compile();
        }
    }

    private sealed class ParameterCollector : ExpressionVisitor
    {
        private readonly List<ParameterExpression> _parameters = [];
        public IReadOnlyList<ParameterExpression> Parameters => _parameters;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _parameters.Add(node);
            return base.VisitParameter(node);
        }
    }

    [Test]
    public void TypeNameIdentifier_ResolvesInCompiledPath()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void LogicalOperator_WithNonBooleanOperand_ThrowsCs0019()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("1 && 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void LocalVariableTypeFlow_AssignmentThenArithmetic_RemainsValid()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 1; x = 2L; return x + 1; }");
        Assert.That(Convert.ToInt64(result), Is.EqualTo(3));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassFunctionShadowing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("x", 5);
        engine.RegisterFunction("x", _ => 123);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("x + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassModuleShadowing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("Math", 5);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("Math + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void StaticModuleCallFastPath_DoesNotBypassFunctionShadowing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterFunction("Math", _ => 123);

        Assert.Throws<CsEvalException>(() => engine.Evaluate("Math.Abs(-5)"));
    }

    [Test]
    public void TypedIdentifierFastPath_RespectsCaseInsensitiveLookup()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            IsCaseSensitive = false
        });
        engine.SetVariable<int>("Value", 41);

        var result = engine.Evaluate("value + 1");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void MathMix_CompiledPath_AvoidsRuntimeCallDispatchAllocations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("x", 11);
        engine.SetVariable<int>("y", 7);
        engine.SetVariable<int>("z", 9);

        var expression = engine.Parse("Math.Abs(x - y) + Math.Max(y, z)");

        for (var i = 0; i < 10_000; i++)
            _ = engine.Evaluate(expression);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 50_000; i++)
            _ = engine.Evaluate(expression);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // 50k boxed int results account for ~1.2 MB. Keep a conservative ceiling that still
        // detects runtime member-call fallback allocations (multiple args arrays per call).
        Assert.That(allocated, Is.LessThan(4_000_000L));
    }

    [Test]
    public void PureReadOnlyExpression_UsesLazyTypedIdentifierCacheSlots()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<int>("x", 10);
        engine.SetVariable<int>("y", 3);
        engine.SetVariable<int>("z", 7);
        engine.SetVariable<int>("value", 42);

        var expression =
            "(((x + 1) > y && (z * 2) != value) || ((x + 2) > y && (z * 3) != value)) && (x == x)";
        _ = engine.Evaluate(expression);

        var compiled = capturingCompiler.LastExpression;
        Assert.That(compiled, Is.Not.Null);

        var collector = new ParameterCollector();
        collector.Visit(compiled!.Body);
        var idCacheSlots = collector.Parameters
            .Count(p => p.Name?.StartsWith("idCacheValue_", StringComparison.Ordinal) == true);

        Assert.That(idCacheSlots, Is.GreaterThan(0));
    }

    [Test]
    public void LazyTypedIdentifierCache_PreservesLogicalShortCircuit()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true || (missingVariable > 0)");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void LazyTypedIdentifierCache_DoesNotCrossSideEffectingCalls()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<int>("x", 1);
        engine.RegisterFunction("bump", _ =>
        {
            var current = engine.Evaluate<int>("x");
            engine.SetVariable("x", current + 1);
            return 0;
        });

        var result = engine.Evaluate<int>("x + bump() + x");
        Assert.That(result, Is.EqualTo(3));
    }
}
