namespace Alder.Tracing;

public sealed record EvaluationTraceStep(
    string NodeKind,
    object? Value,
    string? Display);
