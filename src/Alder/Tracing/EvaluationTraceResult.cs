namespace Alder.Tracing;

public sealed record EvaluationTraceResult(
    object? Result,
    IReadOnlyList<EvaluationTraceStep> Steps);
