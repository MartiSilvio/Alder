using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{
    private EvaluationStateLease CreateEvaluationState(
        IDictionary<string, object?>? variables,
        CancellationToken cancellationToken)
    {
        return CreateEvaluationState(CreateEvaluationBindingContext(variables), cancellationToken);
    }

    private EvaluationStateLease CreateEvaluationState(
        (string Name, object? Value, Type Type)[] variables,
        CancellationToken cancellationToken)
    {
        return CreateEvaluationState(CreateEvaluationBindingContext(variables), cancellationToken);
    }

    private EvaluationStateLease CreateEvaluationState(
        AlderContext bindingContext,
        CancellationToken cancellationToken)
    {
        var constraintState = RentExecutionConstraintState();

        try
        {
            return new EvaluationStateLease(
                bindingContext,
                CreateExecutionContext(bindingContext, cancellationToken),
                constraintState);
        }
        catch
        {
            ReturnExecutionConstraintState(constraintState);
            throw;
        }
    }

    private static AlderContext CreateExecutionContext(AlderContext context, CancellationToken cancellationToken)
    {
        var executionContext = context.CreateChild();
        executionContext.ActiveCancellationToken = cancellationToken;
        return executionContext;
    }

    private readonly struct EvaluationStateLease : IDisposable
    {
        internal EvaluationStateLease(
            AlderContext bindingContext,
            AlderContext executionContext,
            ExecutionConstraintState constraintState)
        {
            BindingContext = bindingContext;
            ExecutionContext = executionContext;
            ConstraintState = constraintState;
        }

        internal AlderContext BindingContext { get; }

        internal AlderContext ExecutionContext { get; }

        internal ExecutionConstraintState ConstraintState { get; }

        public void Dispose()
        {
            ReturnExecutionConstraintState(ConstraintState);
        }
    }
}
