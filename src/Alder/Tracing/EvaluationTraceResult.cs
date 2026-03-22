namespace Alder.Tracing;

public sealed record EvaluationTraceResult(
    object? Result,
    TraceNode Tree,
    Exception? Error);
