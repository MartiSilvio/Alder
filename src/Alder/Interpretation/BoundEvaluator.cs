using System.Threading.Tasks;
using Alder.Binding;
using Alder.Runtime;
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
            EnrichException(ex);
            throw;
        }
    }

    public async ValueTask<object?> EvaluateAsync(BoundExpr expr)
    {
        try
        {
            return await _evalCtx.EvaluateAsync(expr);
        }
        catch (AlderException ex) when (ex.Span.IsEmpty)
        {
            EnrichException(ex);
            throw;
        }
    }

    private void EnrichException(AlderException ex)
    {
        var faulted = _evalCtx.LastEvaluatedExpr;
        if (faulted is { Span.IsEmpty: false })
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
    }
}
