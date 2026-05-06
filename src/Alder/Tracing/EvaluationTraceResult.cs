namespace Alder.Tracing;

/// <summary>
/// Evaluation result paired with a step-by-step trace tree.
/// </summary>
public sealed record EvaluationTraceResult(
    object? Result,
    TraceNode Tree,
    Exception? Error);
