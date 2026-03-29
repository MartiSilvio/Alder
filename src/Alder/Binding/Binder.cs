using Alder.Binding.Binders;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Text;

namespace Alder.Binding;

internal sealed class Binder
{
    private readonly SourceText? _sourceText;
    private readonly bool _recovering;
    private List<AlderDiagnostic>? _diagnostics;
    private readonly BinderContext _binderCtx;

    public Binder()
    {
        _binderCtx = new BinderContext(Bind);
        RegisterBinders();
    }

    public Binder(SourceText sourceText, bool recovering = false)
    {
        _sourceText = sourceText;
        _recovering = recovering;
        _binderCtx = new BinderContext(Bind);
        RegisterBinders();
    }

    private void RegisterBinders()
    {
        _binderCtx.Register(new LiteralBinder());
        _binderCtx.Register(new IdentifierBinder());
        _binderCtx.Register(new TypeReferenceBinder());
        _binderCtx.Register(new IsPatternBinder());
        _binderCtx.Register(new NameofBinder());
        _binderCtx.Register(new TypeofBinder());
        _binderCtx.Register(new DefaultBinder());
        _binderCtx.Register(new SizeofBinder());
        _binderCtx.Register(new CollectionExprBinder());
        _binderCtx.Register(new ImplicitArrayCreationBinder());
        _binderCtx.Register(new ObjectLiteralBinder());
        _binderCtx.Register(new SpreadBinder());
        _binderCtx.Register(new SliceBinder());
        _binderCtx.Register(new ObjectCreationBinder());
        _binderCtx.Register(new TypedArrayCreationBinder());
        _binderCtx.Register(new TypedArrayLiteralBinder());
        _binderCtx.Register(new MultiDimTypedArrayCreationBinder());
        _binderCtx.Register(new MultiDimArrayInitBinder());
        _binderCtx.Register(new MultiDimIndexAccessBinder());
        _binderCtx.Register(new MultiDimIndexAssignBinder());
        _binderCtx.Register(new DeconstructionBinder());
        _binderCtx.Register(new TupleBinder());
        _binderCtx.Register(new InterpolatedStringBinder());
        _binderCtx.Register(new IndexFromEndBinder());
        _binderCtx.Register(new RangeBinder());
        _binderCtx.Register(new CastBinder());
        _binderCtx.Register(new AsBinder());
        _binderCtx.Register(new UnaryBinder());
        _binderCtx.Register(new BinaryBinder());
        _binderCtx.Register(new LogicalBinder());
        _binderCtx.Register(new NullCoalesceBinder());
        _binderCtx.Register(new ConditionalBinder());
        _binderCtx.Register(new CheckedBinder());
        _binderCtx.Register(new ChainedComparisonBinder());
        _binderCtx.Register(new BlockBinder());
        _binderCtx.Register(new IfBinder());
        _binderCtx.Register(new WhileBinder());
        _binderCtx.Register(new ForBinder());
        _binderCtx.Register(new DoWhileBinder());
        _binderCtx.Register(new ForEachBinder());
        _binderCtx.Register(new UsingBinder());
        _binderCtx.Register(new LockBinder());
        _binderCtx.Register(new TryCatchFinallyBinder());
        _binderCtx.Register(new ThrowBinder());
        _binderCtx.Register(new ThrowStatementBinder());
        _binderCtx.Register(new SwitchStatementBinder());
        _binderCtx.Register(new SwitchExpressionBinder());
        _binderCtx.Register(new ReturnBinder());
        _binderCtx.Register(new BreakBinder());
        _binderCtx.Register(new ContinueBinder());
        _binderCtx.Register(new GotoBinder());
        _binderCtx.Register(new GotoCaseBinder());
        _binderCtx.Register(new GotoDefaultBinder());
        _binderCtx.Register(new LabelBinder());
        _binderCtx.Register(new NewExprBinder());
        _binderCtx.Register(new VariableDeclBinder());
        _binderCtx.Register(new AssignBinder());
        _binderCtx.Register(new NullCoalesceAssignBinder());
        _binderCtx.Register(new CompoundAssignBinder());
        _binderCtx.Register(new IncrementDecrementBinder());
        _binderCtx.Register(new MemberAssignBinder());
        _binderCtx.Register(new IndexAssignBinder());
        _binderCtx.Register(new MemberCompoundAssignBinder());
        _binderCtx.Register(new IndexCompoundAssignBinder());
        _binderCtx.Register(new MemberNullCoalesceAssignBinder());
        _binderCtx.Register(new IndexNullCoalesceAssignBinder());
        _binderCtx.Register(new MemberIncrementBinder());
        _binderCtx.Register(new IndexIncrementBinder());
        _binderCtx.Register(new LambdaBinder());
        _binderCtx.Register(new PipelineBinder());
        _binderCtx.Register(new NamedArgumentBinder());
        _binderCtx.Register(new OutArgBinder());
        _binderCtx.Register(new MemberAccessBinder());
        _binderCtx.Register(new IndexAccessBinder());
        _binderCtx.Register(new CallBinder());
    }

    internal IReadOnlyList<AlderDiagnostic> GetAccumulatedDiagnostics()
        => _diagnostics ?? (IReadOnlyList<AlderDiagnostic>)Array.Empty<AlderDiagnostic>();

    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (!_recovering)
        {
            var bound = BindCore(expr, context);
            return bound.Span.IsEmpty ? bound with { Span = expr.Span } : bound;
        }

        try
        {
            var bound = BindCore(expr, context);
            return bound.Span.IsEmpty ? bound with { Span = expr.Span } : bound;
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

    private BoundExpr BindCore(Expr expr, BindingContext context)
    {
        if (_binderCtx.TryBind(expr, context, out var result))
            return result;

        throw new BindingNotSupportedException(
            $"Binding for expression type '{expr.GetType().Name}' is not implemented");
    }

    public IReadOnlyList<AlderDiagnostic> CollectDiagnostics(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (!_recovering) throw new InvalidOperationException("CollectDiagnostics requires a binder created with recovering: true");

        try
        {
            Bind(expr, context);
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
