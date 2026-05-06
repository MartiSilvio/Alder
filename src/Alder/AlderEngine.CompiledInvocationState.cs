using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{
    internal CompiledInvocationStateLease CreateCompiledInvocationState(
        int expectedTypeVersion,
        CancellationToken cancellationToken)
    {
        var constraintState = RentExecutionConstraintState();

        try
        {
            return new CompiledInvocationStateLease(
                CreateCompiledInvocationContext(expectedTypeVersion, cancellationToken),
                constraintState);
        }
        catch
        {
            ReturnExecutionConstraintState(constraintState);
            throw;
        }
    }

    internal readonly struct CompiledInvocationStateLease : IDisposable
    {
        internal CompiledInvocationStateLease(
            AlderContext executionContext,
            ExecutionConstraintState constraintState)
        {
            ExecutionContext = executionContext;
            ConstraintState = constraintState;
        }

        internal AlderContext ExecutionContext { get; }

        internal ExecutionConstraintState ConstraintState { get; }

        public void Dispose()
        {
            ReturnExecutionConstraintState(ConstraintState);
        }
    }
}
