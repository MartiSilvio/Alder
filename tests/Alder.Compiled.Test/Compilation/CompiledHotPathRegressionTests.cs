using Alder.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Alder.Runtime;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compilation;

[TestFixture(CompilationMode.Compiled)]
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
        compiler.CompiledExpressions.Last(e => e.Parameters.Count == 4);

    [Test]
    public void TypeNameIdentifier_ResolvesInCompiledPath()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void LogicalOperator_WithNonBooleanOperand_ThrowsCs0019()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("1 && 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void LocalVariableTypeFlow_AssignmentThenArithmetic_RemainsValid()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("{ var x = 1L; x = 2L; return x + 1; }");
        Assert.That(Convert.ToInt64(result), Is.EqualTo(3));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassFunctionShadowing()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Functions.Register("x", _ => 123));
        engine.SetVariable<int>("x", 5);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void TypedIdentifierFastPath_DoesNotBypassModuleShadowing()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("Math", 5);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Math + 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void StaticModuleCallFastPath_DoesNotBypassFunctionShadowing()
    {
        var engine = TestEngineFactory.Create(mode, o => o.Functions.Register("Math", _ => 123));

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("Math.Abs(-5)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1061));
    }

    [Test]
    public void TypedIdentifierFastPath_RespectsCaseInsensitiveLookup()
    {
        var engine = TestEngineFactory.Create(mode, o => o.IsCaseSensitive = false);
        engine.SetVariable<int>("Value", 41);

        var result = engine.Evaluate("value + 1");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CompiledHotPath_NoCancellationInvoker_SeesUpdatedVariables()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("x", 1);
        var expression = engine.Parse("x + 1");

        Assert.That(engine.Evaluate(expression), Is.EqualTo(2));

        engine.SetVariable<int>("x", 10);
        Assert.That(engine.Evaluate(expression), Is.EqualTo(11));
    }

    [Test]
    public void CompiledHotPath_NoCancellationInvoker_SwitchesAcrossExpressionsSafely()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("x", 2);
        engine.SetVariable<int>("y", 3);

        var first = engine.Parse("x + 1");
        var second = engine.Parse("y + 10");

        Assert.That(engine.Evaluate(first), Is.EqualTo(3));
        Assert.That(engine.Evaluate(second), Is.EqualTo(13));
        Assert.That(engine.Evaluate(first), Is.EqualTo(3));

        engine.SetVariable<int>("x", 7);
        engine.SetVariable<int>("y", 9);

        Assert.That(engine.Evaluate(first), Is.EqualTo(8));
        Assert.That(engine.Evaluate(second), Is.EqualTo(19));
    }

    [Test]
    public void MathMix_CompiledPath_AvoidsRuntimeCallDispatchAllocations()
    {
        var engine = TestEngineFactory.Create(mode);
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

        Assert.That(allocated, Is.LessThan(5_500_000L));
    }

    [Test]
    public void MathMix_CompiledPath_DoesNotUseRuntimeMethodDispatch()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<int>("x", 11);
        engine.SetVariable<int>("y", 7);
        engine.SetVariable<int>("z", 9);

        var result = engine.Evaluate("Math.Abs(x - y) + Math.Max(y, z)");
        Assert.That(result, Is.EqualTo(13));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var runtimeDispatchCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(Alder.Runtime.MethodInvoker) &&
                m.Name is nameof(Alder.Runtime.MethodInvoker.InvokeCall) or nameof(Alder.Runtime.MethodInvoker.InvokeMemberCall))
            .ToList();

        Assert.That(runtimeDispatchCalls, Is.Empty);
    }

    [Test]
    public void SmallBranching_CompiledPath_DoesNotUseRuntimeNumericOperators()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<int>("x", 11);
        engine.SetVariable<int>("y", 7);

        var result = engine.Evaluate(
            "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? " +
            "((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)");
        Assert.That(Convert.ToDouble(result), Is.EqualTo(11.666666666666666).Within(1e-12));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var runtimeNumericOperatorCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(global::Alder.Runtime.Operators) &&
                m.Name is nameof(global::Alder.Runtime.Operators.Add)
                    or nameof(global::Alder.Runtime.Operators.Subtract)
                    or nameof(global::Alder.Runtime.Operators.Multiply)
                    or nameof(global::Alder.Runtime.Operators.Divide))
            .ToList();

        Assert.That(runtimeNumericOperatorCalls, Is.Empty);

        var numericPromotionCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(NumericDispatch) &&
                m.Name == nameof(NumericDispatch.PromoteToType))
            .ToList();

        Assert.That(numericPromotionCalls, Is.Empty);

        var requireBooleanCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(TypeHelpers) &&
                m.Name == nameof(TypeHelpers.RequireBoolean))
            .ToList();

        Assert.That(requireBooleanCalls, Is.Empty);
    }

    [Test]
    public void PureReadOnlyExpression_UsesLazyTypedIdentifierCacheSlots()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<int>("x", 10);
        engine.SetVariable<int>("y", 3);
        engine.SetVariable<int>("z", 7);
        engine.SetVariable<int>("value", 42);

        var expressionText =
            "(((x + 1) > y && (z * 2) != value) || ((x + 2) > y && (z * 3) != value)) && (x == x)";
        var expression = engine.Parse(expressionText);
        _ = engine.Evaluate(expression);

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        Assert.That(engine.GetCompiledInfo(expression)!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void LazyTypedIdentifierCache_PreservesLogicalShortCircuit()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("true || (missingVariable > 0)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LazyTypedIdentifierCache_DoesNotCrossSideEffectingCalls()
    {
        AlderEngine engine = null!;
        engine = TestEngineFactory.Create(mode, o => o.Functions.Register("bump", _ =>
        {
            var current = engine.Evaluate<int>("x");
            engine.SetVariable("x", current + 1);
            return 0;
        }));
        engine.SetVariable<int>("x", 1);

        var result = engine.Evaluate<int>("x + bump() + x");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ObjectGraphChain_CompiledPath_DoesNotUseRuntimeMemberDispatch()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<List<OrderRow>>("orders", [
            new(5),
            new(9)
        ]);

        var result = engine.Evaluate("orders[0].Quantity + orders[1].Quantity + orders.Count");
        Assert.That(result, Is.EqualTo(16));

        var compiled = SelectRootLambda(capturingCompiler);
        Assert.That(compiled, Is.Not.Null);

        var collector = new MethodCallCollector();
        collector.Visit(compiled.Body);

        var runtimeDispatchCalls = collector.Methods
            .Where(m =>
                m.DeclaringType == typeof(MemberAccess) &&
                m.Name is nameof(MemberAccess.GetMember) or nameof(MemberAccess.GetIndex))
            .ToList();

        Assert.That(runtimeDispatchCalls, Is.Empty);
    }

    [Test]
    public void ObjectGraphChain_CompiledPath_ElidesReflectionGuard_ForSealedSafeTypes()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<List<OrderRow>>("orders", [
            new(5),
            new(9)
        ]);

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
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

        engine.SetVariable<List<OrderRow>>("orders", [
            new(5),
            new(9)
        ]);

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
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

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
                m.DeclaringType == typeof(Alder.Runtime.MethodInvoker) &&
                m.Name is nameof(Alder.Runtime.MethodInvoker.InvokeCall) or nameof(Alder.Runtime.MethodInvoker.InvokeMemberCall))
            .ToList();

        Assert.That(runtimeDispatchCalls, Is.Empty);
    }

    [Test]
    public void StringChain_CompiledPath_ElidesReflectionGuard_ForStringSegments()
    {
        var capturingCompiler = new CapturingExpressionCompiler();
        var engine = TestEngineFactory.Create(mode, o => o.ExpressionCompiler = capturingCompiler);

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
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<List<OrderRow>>("orders", [
            new(5),
            new(9),
            new(13)
        ]);

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

        Assert.That(allocated, Is.LessThan(8_000_000L));
    }

    [Test]
    public void StringChain_CompiledPath_AvoidsRuntimeDispatchAllocations()
    {
        var engine = TestEngineFactory.Create(mode);
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

        Assert.That(allocated, Is.LessThan(8_000_000L));
    }
}
