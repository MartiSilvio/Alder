namespace CsEval;

/// <summary>
/// Configures execution resource limits for expression evaluation.
/// Set on CsEvalOptions.Constraints. All properties default to null (unlimited).
/// Properties are mutable -- limits can be changed between evaluations.
/// </summary>
public sealed class ExecutionConstraints
{
    /// <summary>
    /// Maximum number of statements allowed per Evaluate() call.
    /// Each loop iteration, block statement, and top-level expression counts as one statement.
    /// Null means unlimited. When exceeded, throws CsEvalExecutionLimitException.
    /// </summary>
    public long? MaxStatements { get; set; }

    /// <summary>
    /// Maximum wall-clock time allowed per Evaluate() call.
    /// Uses Stopwatch for low-overhead monotonic timing, checked at statement boundaries.
    /// Null means unlimited. When exceeded, throws CsEvalExecutionLimitException.
    /// </summary>
    public TimeSpan? MaxTimeout { get; set; }
}
