namespace Alder.Benchmarks;

public sealed record ComparableScenario(
    string Name,
    string AlderExpression,
    string RoslynExpression,
    string NCalcExpression,
    string DynamicExpressoExpression,
    string FleeExpression,
    Func<BenchmarkGlobalData, object?> NativeEvaluator)
{
    public override string ToString() => Name;
}

public sealed record AdvancedScenario(
    string Name,
    string AlderExpression,
    string RoslynExpression,
    string DynamicExpressoExpression,
    string FleeExpression)
{
    public override string ToString() => Name;
}

public sealed record ExtendedParityScenario(
    string Name,
    string ExtendedExpression,
    string StandardExpression)
{
    public override string ToString() => Name;
}

public sealed record CompilationScenario(
    string Name,
    string AlderExpression,
    string RoslynExpression,
    string NCalcExpression,
    string DynamicExpressoExpression,
    string FleeExpression)
{
    public override string ToString() => Name;
}

public sealed record LinqScenario(
    string Name,
    string AlderExpression,
    string RoslynExpression,
    Func<BenchmarkGlobalData, object?> NativeEvaluator)
{
    public override string ToString() => Name;
}

public sealed record DynamicLinqScenario(
    string Name,
    Func<BenchmarkGlobalData, object?> NativeEvaluator,
    Func<BenchmarkGlobalData, object?> AlderEvaluator,
    Func<BenchmarkGlobalData, object?> DynamicLinqCoreEvaluator)
{
    public override string ToString() => Name;
}
