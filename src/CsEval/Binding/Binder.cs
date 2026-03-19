using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Text;

namespace CsEval.Binding;

internal sealed partial class Binder
{
    private readonly SourceText? _sourceText;

    public Binder()
    {
    }

    public Binder(SourceText sourceText)
    {
        _sourceText = sourceText;
    }

    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));
        try
        {
            var bound = BindCore(expr, context);
            return bound with { Span = expr.Span };
        }
        catch (CsEvalException ex) when (ex.Span.IsEmpty && !expr.Span.IsEmpty)
        {
            ex.Span = expr.Span;
            if (_sourceText != null)
            {
                var pos = _sourceText.GetLinePosition(expr.Span.Start);
                ex.Line = pos.Line + 1;
                ex.Column = pos.Character + 1;
            }
            throw;
        }
    }

    private BoundExpr BindCore(Expr expr, BindingContext context) => expr switch
    {
                LiteralExpr literal => BoundLiteralExpr.FromValue(literal.Value),
                IdentifierExpr identifier => BindIdentifier(identifier, context),
                TypeReferenceExpr typeReference => BindTypeReference(typeReference, context),
                IsPatternExpr isPattern => BindIsPattern(isPattern, context),
                NameofExpr nameofExpr => new BoundLiteralExpr(nameofExpr.Name, typeof(string)),
                TypeofExpr typeofExpr => BindTypeof(typeofExpr, context),
                DefaultExpr defaultExpr => BindDefault(defaultExpr, context),
                SizeofExpr sizeofExpr => new BoundLiteralExpr(TypeHelpers.GetSizeOf(sizeofExpr.TypeName), typeof(int)),
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
                BreakExpr => new BoundBreakExpr(typeof(object)),
                ContinueExpr => new BoundContinueExpr(typeof(object)),
                GotoExpr gotoExpr => new BoundGotoExpr(gotoExpr.Label, typeof(object)),
                GotoCaseExpr gotoCaseExpr => new BoundGotoCaseExpr(Bind(gotoCaseExpr.Value, context), typeof(object)),
                GotoDefaultExpr => new BoundGotoDefaultExpr(typeof(object)),
                LabelExpr labelExpr => new BoundLabelExpr(labelExpr.Name, typeof(object)),
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
                ThrowStatementExpr => new BoundThrowStatementExpr(typeof(object)),
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

    public IReadOnlyList<CsEvalDiagnostic> CollectDiagnostics(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var diagnostics = new List<CsEvalDiagnostic>();
        if (expr is BlockExpr block)
        {
            var blockScope = context.CreateChildScope();
            foreach (var statement in block.Statements)
                TryBindForDiagnostics(statement, blockScope, diagnostics);
            if (block.ReturnExpr != null)
                TryBindForDiagnostics(block.ReturnExpr, blockScope, diagnostics);
        }
        else
        {
            TryBindForDiagnostics(expr, context, diagnostics);
        }

        return diagnostics;
    }

    private void TryBindForDiagnostics(
        Expr expr,
        BindingContext context,
        List<CsEvalDiagnostic> diagnostics)
    {
        try
        {
            _ = Bind(expr, context);
        }
        catch (Exception ex)
        {
            diagnostics.Add(NormalizeDiagnostic(ex, expr));
        }
    }

    private static CsEvalDiagnostic NormalizeDiagnostic(Exception ex, Expr expr)
    {
        var diagnostic = CsEvalDiagnostic.FromException(ex);

        if (diagnostic.Span.IsEmpty && !expr.Span.IsEmpty)
            diagnostic = diagnostic with { Span = expr.Span };

        if (diagnostic.Code != null)
            return diagnostic;

        var wrapped = new CsEvalException(DiagnosticDescriptors.SemanticValidationFailed, ex.Message)
        {
            Span = diagnostic.Span
        };
        return CsEvalDiagnostic.FromException(wrapped);
    }
}
