namespace CsEval.Benchmarks;

public sealed record ComparableScenario(
    string Name,
    string CsEvalExpression,
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
    string CsEvalExpression,
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
    string CsEvalExpression,
    string RoslynExpression,
    string NCalcExpression,
    string DynamicExpressoExpression,
    string FleeExpression)
{
    public override string ToString() => Name;
}

public sealed record LinqScenario(
    string Name,
    string CsEvalExpression,
    string RoslynExpression,
    Func<BenchmarkGlobalData, object?> NativeEvaluator)
{
    public override string ToString() => Name;
}
