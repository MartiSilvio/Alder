using System.Runtime.CompilerServices;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Text;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private readonly SourceText? _sourceText;
    private List<AlderDiagnostic>? _diagnostics;
    internal readonly BinderContext _binderCtx;

    public Binder()
    {
        _binderCtx = new BinderContext(BindWithContext);
    }

    public Binder(SourceText sourceText)
    {
        _sourceText = sourceText;
        _binderCtx = new BinderContext(BindWithContext);
    }

    internal IReadOnlyList<AlderDiagnostic> GetAccumulatedDiagnostics()
        => _diagnostics ?? (IReadOnlyList<AlderDiagnostic>)[];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        var bound = Dispatch(expr, context, _binderCtx);
        if (bound.Span.IsEmpty) bound.Span = expr.Span;
        return bound;
    }

    private BoundExpr BindWithContext(Expr expr, BindingContext context, BinderContext binderCtx)
    {
        var bound = Dispatch(expr, context, binderCtx);
        if (bound.Span.IsEmpty) bound.Span = expr.Span;
        return bound;
    }

    public BoundExpr BindRecovering(Expr expr, BindingContext context)
    {
        try
        {
            return Bind(expr, context);
        }
        catch (AlderException ex)
        {
            var diagnostic = NormalizeDiagnostic(ex, expr);
            _diagnostics ??= new List<AlderDiagnostic>();
            _diagnostics.Add(diagnostic);
            return new BoundLiteralExpr(null, BoundType.Unknown)
            {
                HasErrors = true,
                Diagnostic = diagnostic,
                Span = expr.Span
            };
        }
    }

    public IReadOnlyList<AlderDiagnostic> CollectDiagnostics(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));

        try
        {
            BindRecovering(expr, context);
        }
        catch (Exception ex)
        {
            _diagnostics ??= new List<AlderDiagnostic>();
            _diagnostics.Add(NormalizeDiagnostic(ex, expr));
        }

        return GetAccumulatedDiagnostics();
    }

    private AlderDiagnostic NormalizeDiagnostic(Exception ex, Expr expr)
    {
        var diagnostic = AlderDiagnostic.FromException(ex);

        if (diagnostic.Span.IsEmpty && !expr.Span.IsEmpty)
        {
            int? line = null, column = null;
            if (_sourceText != null)
            {
                var pos = _sourceText.GetLinePosition(expr.Span.Start);
                line = pos.Line + 1;
                column = pos.Character + 1;
            }
            diagnostic = diagnostic with { Span = expr.Span, Line = line, Column = column };
        }

        if (diagnostic.Code != null)
            return diagnostic;

        return new AlderDiagnostic(
            DiagnosticSeverity.Error,
            $"{DiagnosticDescriptors.SemanticValidationFailed.Code.ToDiagnosticId()}: {DiagnosticDescriptors.SemanticValidationFailed.FormatMessage(ex.Message)}",
            DiagnosticDescriptors.SemanticValidationFailed.Code,
            diagnostic.Span,
            diagnostic.Line,
            diagnostic.Column);
    }
}
