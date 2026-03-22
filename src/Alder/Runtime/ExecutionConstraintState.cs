using System.Diagnostics;

namespace Alder.Runtime;

internal sealed class ExecutionConstraintState
{
    public long StatementCount;
    public long LoopIterationCount;
    public Stopwatch? Timer;

    public void Reset(ExecutionConstraints? constraints)
    {
        StatementCount = 0;
        LoopIterationCount = 0;
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
