using System.Diagnostics;

namespace CsEval.Runtime;

/// <summary>
/// Mutable state tracking execution constraints during a single Evaluate() call.
/// Created/reset at evaluation start, shared across nested evaluations via CsEvalContext.
/// </summary>
internal sealed class ExecutionConstraintState
{
    public long StatementCount;
    public Stopwatch? Timer;

    public void Reset(ExecutionConstraints? constraints)
    {
        StatementCount = 0;
        if (constraints?.MaxTimeout is { } timeout && timeout > TimeSpan.Zero)
        {
            Timer = Stopwatch.StartNew();
        }
        else
        {
            Timer = null;
        }
    }
}
