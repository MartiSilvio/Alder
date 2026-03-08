using CsEval.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using CsEval.Runtime;

namespace CsEval.Test.Compilation;

[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CompiledHotPathRegressionTests(CompilationMode mode)
{
    private sealed class CapturingExpressionCompiler : IExpressionCompiler
    {
        public LambdaExpression? LastExpression { get; private set; }
        public List<LambdaExpression> CompiledExpressions { get; } = [];

        public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
            where TDelegate : Delegate
        {
            LastExpression = expression;
            CompiledExpressions.Add(expression);
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

    private sealed class MethodCallCollector : ExpressionVisitor
    {
        private readonly List<MethodInfo> _methods = [];
        public IReadOnlyList<MethodInfo> Methods => _methods;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            _methods.Add(node.Method);
            return base.VisitMethodCall(node);
        }
    }

    private sealed record OrderRow(int Quantity);

    private static LambdaExpression SelectRootLambda(CapturingExpressionCompiler compiler) =>
        compiler.CompiledExpressions.Last(e => e.Parameters.Count == 3);

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
        Assert.That(allocated, Is.LessThan(5_500_000L));
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

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new ParameterCollector();
        collector.Visit(compiled.Body);
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

    [Test]
    public void ObjectGraphChain_CompiledPath_DoesNotUseRuntimeMemberDispatch()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<List<OrderRow>>("orders", new List<OrderRow>
        {
            new(5),
            new(9)
        });

        var result = engine.Evaluate("orders[0].Quantity + orders[1].Quantity + orders.Count");
        Assert.That(result, Is.EqualTo(16));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var runtimeDispatchCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(MemberAccess) &&
                (m.Name == nameof(MemberAccess.GetMember) ||
                 m.Name == nameof(MemberAccess.GetIndex)))
            .ToList();

        Assert.That(runtimeDispatchCalls, Is.Empty);
    }

    [Test]
    public void ObjectGraphChain_CompiledPath_ElidesReflectionGuard_ForSealedSafeTypes()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<List<OrderRow>>("orders", new List<OrderRow>
        {
            new(5),
            new(9)
        });

        var result = engine.Evaluate("orders[0].Quantity + orders[1].Quantity + orders.Count");
        Assert.That(result, Is.EqualTo(16));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var reflectionGuardCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(TypeHelpers) &&
                m.Name == nameof(TypeHelpers.GuardReflectionLeakTyped))
            .ToList();

        Assert.That(reflectionGuardCalls, Is.Empty);
    }

    [Test]
    public void ObjectGraphChain_CompiledPath_UsesLiteralIndexFastPath_InStandardMode()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<List<OrderRow>>("orders", new List<OrderRow>
        {
            new(5),
            new(9)
        });

        var result = engine.Evaluate("orders[0].Quantity + orders[1].Quantity + orders.Count");
        Assert.That(result, Is.EqualTo(16));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var normalizeCalls = collector.Methods
            .Where(m => m.DeclaringType == typeof(MemberAccess) && m.Name == nameof(MemberAccess.NormalizeIndex))
            .ToList();

        Assert.That(normalizeCalls, Is.Empty);
    }

    [Test]
    public void StringChain_CompiledPath_DoesNotUseRuntimeMethodDispatch()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<string>("text", "alpha");
        var result = engine.Evaluate("text.Trim().ToUpper().Length");
        Assert.That(result, Is.EqualTo(5));

        var compiled = capturingCompiler.LastExpression;
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled!.Body);

        var runtimeDispatchCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(MemberAccess) && m.Name == nameof(MemberAccess.GetMember) ||
                m.DeclaringType == typeof(CsEval.Runtime.MethodInvoker) &&
                m.Name is nameof(CsEval.Runtime.MethodInvoker.InvokeCall) or nameof(CsEval.Runtime.MethodInvoker.InvokeMemberCall))
            .ToList();

        Assert.That(runtimeDispatchCalls, Is.Empty);
    }

    [Test]
    public void StringChain_CompiledPath_ElidesReflectionGuard_ForStringSegments()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            ExpressionCompiler = capturingCompiler
        });

        engine.SetVariable<string>("text", "alpha");
        var result = engine.Evaluate("text.Trim().ToUpper().Length");
        Assert.That(result, Is.EqualTo(5));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var reflectionGuardCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(TypeHelpers) &&
                m.Name == nameof(TypeHelpers.GuardReflectionLeakTyped))
            .ToList();

        Assert.That(reflectionGuardCalls, Is.Empty);
    }

    [Test]
    public void ObjectGraphAccess_CompiledPath_AvoidsRuntimeDispatchAllocations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<List<OrderRow>>("orders", new List<OrderRow>
        {
            new(5),
            new(9),
            new(13)
        });

        var expression = engine.Parse("orders[0].Quantity + orders[1].Quantity + orders.Count");

        for (var i = 0; i < 10_000; i++)
            _ = engine.Evaluate(expression);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 30_000; i++)
            _ = engine.Evaluate(expression);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.LessThan(5_500_000L));
    }

    [Test]
    public void StringChain_CompiledPath_AvoidsRuntimeDispatchAllocations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable<string>("text", "alpha");
        var expression = engine.Parse("text.Trim().ToUpper().Length");

        for (var i = 0; i < 10_000; i++)
            _ = engine.Evaluate(expression);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 30_000; i++)
            _ = engine.Evaluate(expression);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.LessThan(3_000_000L));
    }
}
