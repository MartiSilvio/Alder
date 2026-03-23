namespace Alder.Tracing;

/// <summary>
/// The result of an evaluated expression with its step-by-step trace tree, returned by <see cref="AlderEngine.EvaluateWithTrace(string, IDictionary{string, object?}?, CancellationToken)"/>.
/// </summary>
/// <param name="Result">The evaluation result, or <c>null</c> if evaluation failed.</param>
/// <param name="Tree">The root of the evaluation trace tree.</param>
/// <param name="Error">The exception that caused evaluation to fail, or <c>null</c> if successful.</param>
public sealed record EvaluationTraceResult(
    object? Result,
    TraceNode Tree,
    Exception? Error);
