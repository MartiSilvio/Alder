using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Text;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private readonly SourceText? _sourceText;
    private readonly bool _recovering;
    private List<AlderDiagnostic>? _diagnostics;

    public Binder()
    {
    }

    public Binder(SourceText sourceText, bool recovering = false)
    {
        _sourceText = sourceText;
        _recovering = recovering;
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
            if (bound.Span.IsEmpty) bound.Span = expr.Span;
            return bound;
        }

        try
        {
            var bound = BindCore(expr, context);
            if (bound.Span.IsEmpty) bound.Span = expr.Span;
            return bound;
        }
        catch (AlderException ex)
        {
            var diagnostic = NormalizeDiagnostic(ex, expr);
            _diagnostics ??= new List<AlderDiagnostic>();
            _diagnostics.Add(diagnostic);
            return new BoundLiteralExpr(null, new BoundType(typeof(object)))
            {
                HasErrors = true,
                Diagnostic = diagnostic,
                Span = expr.Span
            };
        }
    }

    private BoundExpr BindCore(Expr expr, BindingContext context) => expr switch
    {
                LiteralExpr literal => BoundLiteralExpr.FromValue(literal.Value),
                IdentifierExpr identifier => BindIdentifier(identifier, context),
                TypeReferenceExpr typeReference => BindTypeReference(typeReference, context),
                IsPatternExpr isPattern => BindIsPattern(isPattern, context),
                NameofExpr nameofExpr => new BoundLiteralExpr(nameofExpr.Name, new BoundType(typeof(string))),
                TypeofExpr typeofExpr => BindTypeof(typeofExpr, context),
                DefaultExpr defaultExpr => BindDefault(defaultExpr, context),
                SizeofExpr sizeofExpr => new BoundLiteralExpr(TypeHelpers.GetSizeOf(sizeofExpr.TypeName), new BoundType(typeof(int))),
                ArrayLiteralExpr arrayLiteral => BindArrayLiteral(arrayLiteral, context),
                ObjectLiteralExpr objectLiteral => BindObjectLiteral(objectLiteral, context),
                SpreadExpr spread => BindSpread(spread, context),
                SliceExpr slice => BindSlice(slice, context),
                ObjectCreationExpr objectCreation => BindObjectCreation(objectCreation, context),
                TypedArrayCreationExpr typedArrayCreation => BindTypedArrayCreation(typedArrayCreation, context),
                TypedArrayLiteralExpr typedArrayLiteral => BindTypedArrayLiteral(typedArrayLiteral, context),
                MultiDimTypedArrayCreationExpr multiDimTypedArrayCreation => BindMultiDimTypedArrayCreation(multiDimTypedArrayCreation, context),
                MultiDimArrayInitExpr multiDimArrayInit => BindMultiDimArrayInit(multiDimArrayInit, context),
                MultiDimIndexAccessExpr multiDimIndexAccess => BindMultiDimIndexAccess(multiDimIndexAccess, context),
                MultiDimIndexAssignExpr multiDimIndexAssign => BindMultiDimIndexAssign(multiDimIndexAssign, context),
                DeconstructionExpr deconstruction => BindDeconstruction(deconstruction, context),
                TupleExpr tupleExpr => BindTuple(tupleExpr, context),
                InterpolatedStringExpr interpolatedString => BindInterpolatedString(interpolatedString, context),
                CastExpr cast => BindCast(cast, context),
                AsExpr asExpr => BindAs(asExpr, context),
                UnaryExpr unary => BindUnary(unary, context),
                BinaryExpr binary => BindBinary(binary, context),
                LogicalExpr logical => BindLogical(logical, context),
                NullCoalesceExpr nullCoalesce => BindNullCoalesce(nullCoalesce, context),
                ConditionalExpr conditional => BindConditional(conditional, context),
                BlockExpr block => BindBlock(block, context),
                IfStatementExpr ifStatement => BindIfStatement(ifStatement, context),
                WhileStatementExpr whileStatement => BindWhile(whileStatement, context),
                ForStatementExpr forStatement => BindFor(forStatement, context),
                DoWhileStatementExpr doWhileStatement => BindDoWhile(doWhileStatement, context),
                ForEachStatementExpr forEachStatement => BindForEach(forEachStatement, context),
                UsingStatementExpr usingStatement => BindUsingStatement(usingStatement, context),
                LockStatementExpr lockStatement => BindLockStatement(lockStatement, context),
                SwitchStatementExpr switchStatement => BindSwitchStatement(switchStatement, context),
                SwitchExpressionExpr switchExpression => BindSwitchExpression(switchExpression, context),
                TryCatchFinallyExpr tryCatchFinally => BindTryCatchFinally(tryCatchFinally, context),
                CheckedExpr checkedExpr => BindCheckedExpr(checkedExpr, context),
                ChainedComparisonExpr chainedComparison => BindChainedComparison(chainedComparison, context),
                BreakExpr => new BoundBreakExpr(new BoundType(typeof(object))),
                ContinueExpr => new BoundContinueExpr(new BoundType(typeof(object))),
                GotoExpr gotoExpr => new BoundGotoExpr(gotoExpr.Label, new BoundType(typeof(object))),
                GotoCaseExpr gotoCaseExpr => new BoundGotoCaseExpr(Bind(gotoCaseExpr.Value, context), new BoundType(typeof(object))),
                GotoDefaultExpr => new BoundGotoDefaultExpr(new BoundType(typeof(object))),
                LabelExpr labelExpr => new BoundLabelExpr(labelExpr.Name, new BoundType(typeof(object))),
                VariableDeclExpr variableDecl => BindVariableDecl(variableDecl, context),
                AssignExpr assign => BindAssign(assign, context),
                NullCoalesceAssignExpr nullCoalesceAssign => BindNullCoalesceAssign(nullCoalesceAssign, context),
                CompoundAssignExpr compoundAssign => BindCompoundAssign(compoundAssign, context),
                IncrementDecrementExpr incrementDecrement => BindIncrementDecrement(incrementDecrement, context),
                MemberAssignExpr memberAssign => BindMemberAssign(memberAssign, context),
                IndexAssignExpr indexAssign => BindIndexAssign(indexAssign, context),
                MemberCompoundAssignExpr memberCompoundAssign => BindMemberCompoundAssign(memberCompoundAssign, context),
                IndexCompoundAssignExpr indexCompoundAssign => BindIndexCompoundAssign(indexCompoundAssign, context),
                MemberNullCoalesceAssignExpr memberNullCoalesceAssign => BindMemberNullCoalesceAssign(memberNullCoalesceAssign, context),
                IndexNullCoalesceAssignExpr indexNullCoalesceAssign => BindIndexNullCoalesceAssign(indexNullCoalesceAssign, context),
                MemberIncrementExpr memberIncrement => BindMemberIncrement(memberIncrement, context),
                IndexIncrementExpr indexIncrement => BindIndexIncrement(indexIncrement, context),
                NewExpr newExpr => Bind(newExpr.Initializer, context),
                ThrowExpr throwExpr => BindThrowExpr(throwExpr, context),
                ThrowStatementExpr => new BoundThrowStatementExpr(new BoundType(typeof(object))),
                ReturnExpr returnExpr => BindReturn(returnExpr, context),
                MemberAccessExpr memberAccess => BindMemberAccess(memberAccess, context),
                IndexAccessExpr indexAccess => BindIndexAccess(indexAccess, context),
                LambdaExpr lambda => BindLambda(lambda),
                PipelineExpr pipeline => BindPipeline(pipeline, context),
                RangeExpr rangeExpr => BindRange(rangeExpr, context),
                IndexFromEndExpr indexFromEnd => BindIndexFromEnd(indexFromEnd, context),
                NamedArgumentExpr namedArgument => BindNamedArgument(namedArgument, context),
                OutArgExpr outArg => BindOutArg(outArg),
                CallExpr call => BindCall(call, context),
                _ => throw new BindingNotSupportedException(
                    $"Binding for expression type '{expr.GetType().Name}' is not implemented")
    };

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
