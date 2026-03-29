using Alder.Binding;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed class BoundEvaluator
{
    private readonly EvaluationContext _evalCtx;

    public BoundEvaluator(
        AlderContext context,
        AlderConfig config,
        ExecutionConstraintState? constraintState = null,
        EvaluationTracer? tracer = null,
        SourceText? sourceText = null,
        CancellationToken cancellationToken = default)
    {
        _evalCtx = new EvaluationContext(context, config, constraintState, cancellationToken, new Stack<Exception>());
        _evalCtx.Tracer = tracer;
        _evalCtx.SourceText = sourceText;
    }

    public object? Evaluate(BoundExpr expr) => _evalCtx.Evaluate(expr);
}
