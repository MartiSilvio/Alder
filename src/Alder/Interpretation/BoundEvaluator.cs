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

    public object? Evaluate(BoundExpr expr)
    {
        try
        {
            return _evalCtx.Evaluate(expr);
        }
        catch (AlderException ex) when (ex.Span.IsEmpty)
        {
            var faulted = _evalCtx.LastEvaluatedExpr;
            if (faulted != null && !faulted.Span.IsEmpty)
            {
                int? line = null, column = null;
                if (_evalCtx.SourceText != null)
                {
                    var pos = _evalCtx.SourceText.GetLinePosition(faulted.Span.Start);
                    line = pos.Line + 1;
                    column = pos.Character + 1;
                }
                ex.EnrichDiagnosticsWithPosition(faulted.Span, line, column);
            }
            throw;
        }
    }
}
