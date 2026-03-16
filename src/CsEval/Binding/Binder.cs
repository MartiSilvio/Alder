using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Binding.Services;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Collections.Immutable;

namespace CsEval.Binding;

internal sealed class Binder
{
    public Binder()
    {
    }

    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        if (context is null) throw new ArgumentNullException(nameof(context));
        return expr switch
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
    }

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

        if ((diagnostic.Line is null || diagnostic.Column is null) &&
            ExprTokenLocator.TryGetPrimaryToken(expr, out var token))
        {
            diagnostic = diagnostic with
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (diagnostic.Code != null)
            return diagnostic;

        var wrapped = new CsEvalException(
            DiagnosticDescriptors.SemanticValidationFailed,
            diagnostic.Line,
            diagnostic.Column,
            diagnostic.SpanStart,
            diagnostic.SpanLength,
            ex.Message);
        return CsEvalDiagnostic.FromException(wrapped);
    }

    private static BoundLiteralExpr BindTypeReference(TypeReferenceExpr typeReference, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeReference.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, typeof(Type));
    }

    private static BoundLiteralExpr BindTypeof(TypeofExpr typeofExpr, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeofExpr.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, typeof(Type));
    }

    private static BoundLiteralExpr BindDefault(DefaultExpr defaultExpr, BindingContext context)
    {
        if (defaultExpr.TypeToken == null)
            return new BoundLiteralExpr(null, typeof(object));

        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(defaultExpr.TypeToken.Value.Lexeme);
        var value = TypeHelpers.GetDefaultValue(resolvedType);
        return new BoundLiteralExpr(value, resolvedType);
    }

    private BoundArrayLiteralExpr BindArrayLiteral(ArrayLiteralExpr arrayLiteral, BindingContext context)
    {
        var elements = arrayLiteral.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        return new BoundArrayLiteralExpr(elements, typeof(object));
    }

    private BoundSpreadExpr BindSpread(SpreadExpr spread, BindingContext context)
    {
        var expression = Bind(spread.Expression, context);
        return new BoundSpreadExpr(expression, typeof(object));
    }

    private BoundObjectLiteralExpr BindObjectLiteral(ObjectLiteralExpr objectLiteral, BindingContext context)
    {
        var properties = objectLiteral.Properties
            .Select(property =>
            {
                var (key, value) = property;
                if (key.Type == TokenType.DotDot && value is SpreadExpr spread)
                {
                    return new BoundObjectLiteralProperty(
                        PropertyName: null,
                        Value: Bind(spread.Expression, context),
                        IsSpread: true);
                }

                return new BoundObjectLiteralProperty(
                    PropertyName: key.Lexeme,
                    Value: Bind(value, context),
                    IsSpread: false);
            })
            .ToImmutableArray();

        return new BoundObjectLiteralExpr(properties, typeof(object));
    }

    private BoundSliceExpr BindSlice(SliceExpr slice, BindingContext context)
    {
        var target = Bind(slice.Target, context);
        var start = slice.Start != null ? Bind(slice.Start, context) : null;
        var end = slice.End != null ? Bind(slice.End, context) : null;
        var step = slice.Step != null ? Bind(slice.Step, context) : null;
        return new BoundSliceExpr(target, start, end, step, typeof(object));
    }

    private BoundObjectCreationExpr BindObjectCreation(ObjectCreationExpr objectCreation, BindingContext context)
    {
        var arguments = objectCreation.Arguments
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();
        var initializerEntries = objectCreation.Initializer != null
            ? [
                ..objectCreation.Initializer.Entries
                    .Select(entry => new BoundInitializerEntry(
                        entry.PropertyName,
                        Bind(entry.Value, context),
                        entry.IndexerKey != null ? Bind(entry.IndexerKey, context) : null))
            ]
            : ImmutableArray<BoundInitializerEntry>.Empty;
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(objectCreation.TypeName) ?? typeof(object);
        return new BoundObjectCreationExpr(objectCreation.TypeName, arguments, initializerEntries, resolvedType);
    }

    private BoundTypedArrayCreationExpr BindTypedArrayCreation(TypedArrayCreationExpr typedArrayCreation, BindingContext context)
    {
        var size = Bind(typedArrayCreation.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayCreation.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType)
            : typeof(Array);
        return new BoundTypedArrayCreationExpr(typedArrayCreation.ElementTypeName, size, arrayType);
    }

    private BoundTypedArrayLiteralExpr BindTypedArrayLiteral(TypedArrayLiteralExpr typedArrayLiteral, BindingContext context)
    {
        var elements = typedArrayLiteral.Elements.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayLiteral.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType)
            : typeof(Array);
        return new BoundTypedArrayLiteralExpr(typedArrayLiteral.ElementTypeName, elements, arrayType);
    }

    private BoundTupleExpr BindTuple(TupleExpr tupleExpr, BindingContext context)
    {
        var elements = tupleExpr.Elements
            .Select(element => Bind(element.Expression, context))
            .ToImmutableArray();
        var names = tupleExpr.Elements
            .Select(static element => element.Name)
            .ToImmutableArray();
        var tupleType = CreateTupleStaticType(elements.Select(static element => element.StaticType).ToArray());
        return new BoundTupleExpr(elements, names, tupleType);
    }

    private BoundMultiDimTypedArrayCreationExpr BindMultiDimTypedArrayCreation(
        MultiDimTypedArrayCreationExpr multiDimTypedArrayCreation,
        BindingContext context)
    {
        var sizes = multiDimTypedArrayCreation.Sizes
            .Select(size => Bind(size, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimTypedArrayCreation.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType, multiDimTypedArrayCreation.Sizes.Count)
            : typeof(Array);
        return new BoundMultiDimTypedArrayCreationExpr(multiDimTypedArrayCreation.ElementTypeName, sizes, arrayType);
    }

    private BoundMultiDimArrayInitExpr BindMultiDimArrayInit(
        MultiDimArrayInitExpr multiDimArrayInit,
        BindingContext context)
    {
        var explicitSizes = multiDimArrayInit.ExplicitSizes?
            .Select(size => Bind(size, context))
            .ToImmutableArray();
        var flatValues = multiDimArrayInit.FlatValues
            .Select(value => Bind(value, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimArrayInit.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType, multiDimArrayInit.Rank)
            : typeof(Array);
        return new BoundMultiDimArrayInitExpr(
            multiDimArrayInit.ElementTypeName,
            multiDimArrayInit.Rank,
            explicitSizes,
            flatValues,
            multiDimArrayInit.InferredDimensions,
            arrayType);
    }

    private BoundMultiDimIndexAccessExpr BindMultiDimIndexAccess(
        MultiDimIndexAccessExpr multiDimIndexAccess,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAccess.Object, context);
        var indices = multiDimIndexAccess.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        return new BoundMultiDimIndexAccessExpr(target, indices, multiDimIndexAccess.NullSafe, typeof(object));
    }

    private BoundMultiDimIndexAssignExpr BindMultiDimIndexAssign(
        MultiDimIndexAssignExpr multiDimIndexAssign,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAssign.Object, context);
        var indices = multiDimIndexAssign.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        var value = Bind(multiDimIndexAssign.Value, context);
        return new BoundMultiDimIndexAssignExpr(target, indices, value, value.StaticType);
    }

    private BoundDeconstructionExpr BindDeconstruction(DeconstructionExpr deconstruction, BindingContext context)
    {
        var valueExpression = Bind(deconstruction.ValueExpression, context);
        var variableNames = deconstruction.VariableNames.ToImmutableArray();
        return new BoundDeconstructionExpr(variableNames, valueExpression, valueExpression.StaticType);
    }

    private static Type CreateTupleStaticType(Type[] elementTypes)
    {
        if (elementTypes.Length == 0)
            return typeof(ValueTuple);

        if (elementTypes.Length <= 7)
        {
            var openType = elementTypes.Length switch
            {
                1 => typeof(ValueTuple<>),
                2 => typeof(ValueTuple<,>),
                3 => typeof(ValueTuple<,,>),
                4 => typeof(ValueTuple<,,,>),
                5 => typeof(ValueTuple<,,,,>),
                6 => typeof(ValueTuple<,,,,,>),
                7 => typeof(ValueTuple<,,,,,,>),
                _ => throw new InvalidOperationException("Unsupported tuple arity.")
            };
            return RuntimeGenericFactory.CloseGenericType(openType, elementTypes);
        }

        var headTypes = new Type[8];
        Array.Copy(elementTypes, 0, headTypes, 0, 7);
        var restTypes = new Type[elementTypes.Length - 7];
        Array.Copy(elementTypes, 7, restTypes, 0, restTypes.Length);
        headTypes[7] = CreateTupleStaticType(restTypes);

        return RuntimeGenericFactory.CloseGenericType(typeof(ValueTuple<,,,,,,,>), headTypes);
    }

    private BoundInterpolatedStringExpr BindInterpolatedString(InterpolatedStringExpr interpolatedString, BindingContext context)
    {
        var parts = interpolatedString.Parts
            .Select(part => part switch
            {
                TextPart text => (BoundInterpolatedPart)new BoundInterpolatedTextPart(text.Text),
                ExpressionPart expressionPart => new BoundInterpolatedExpressionPart(
                    Bind(expressionPart.Expression, context),
                    expressionPart.AlignmentSpecifier,
                    expressionPart.FormatSpecifier),
                _ => throw new BindingNotSupportedException(
                    $"Interpolated part '{part.GetType().Name}' is not supported")
            })
            .ToImmutableArray();

        return new BoundInterpolatedStringExpr(parts, typeof(string));
    }

    private BoundCheckedExpr BindCheckedExpr(CheckedExpr checkedExpr, BindingContext context)
    {
        var expression = Bind(checkedExpr.Expression, context);
        return new BoundCheckedExpr(expression, checkedExpr.IsChecked, expression.StaticType);
    }

    private BoundChainedComparisonExpr BindChainedComparison(ChainedComparisonExpr chainedComparison, BindingContext context)
    {
        var operands = chainedComparison.Operands
            .Select(operand => Bind(operand, context))
            .ToImmutableArray();
        var operators = chainedComparison.Operators
            .Select(@operator => @operator.Type)
            .ToImmutableArray();
        return new BoundChainedComparisonExpr(operands, operators, typeof(bool));
    }

    private static BoundExpr BindIdentifier(IdentifierExpr identifier, BindingContext context)
    {
        var name = identifier.Name.Lexeme;

        if (context.RuntimeContext.Functions.ContainsKey(name) ||
            context.RuntimeContext.Modules.ContainsKey(name))
        {
            // Runtime resolution gives functions/modules precedence over variables.
            // Keep static type as object so compiled binding does not incorrectly
            // assume variable numeric types for shadowed identifiers.
            return new BoundIdentifierExpr(name, typeof(object));
        }

        context.TryGetVariableType(name, out var staticType);
        if (staticType != typeof(object))
            return new BoundIdentifierExpr(name, staticType);

        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, typeof(Type));
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundCastExpr BindCast(CastExpr cast, BindingContext context)
    {
        var expression = Bind(cast.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(cast.TargetType.Lexeme);
        var sourceStaticType = cast.Expression is IdentifierExpr ? expression.StaticType : null;
        return new BoundCastExpr(expression, targetType, sourceStaticType, targetType);
    }

    private BoundIsPatternExpr BindIsPattern(IsPatternExpr isPattern, BindingContext context)
    {
        var expression = Bind(isPattern.Expression, context);
        return new BoundIsPatternExpr(expression, isPattern.Pattern, typeof(bool));
    }

    private BoundAsExpr BindAs(AsExpr asExpr, BindingContext context)
    {
        var expression = Bind(asExpr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(asExpr.TargetType.Lexeme);
        return new BoundAsExpr(expression, targetType, targetType);
    }

    private BoundBinaryExpr BindBinary(BinaryExpr binary, BindingContext context)
    {
        var left = Bind(binary.Left, context);
        var right = Bind(binary.Right, context);
        var resultType = InferBinaryResultType(binary.Op.Type, left.StaticType, right.StaticType);

        return new BoundBinaryExpr(binary.Op.Type, left, right, resultType);
    }

    private BoundUnaryExpr BindUnary(UnaryExpr unary, BindingContext context)
    {
        var operand = Bind(unary.Right, context);
        var resultType = unary.Op.Type == TokenType.Bang ? typeof(bool) : operand.StaticType;
        return new BoundUnaryExpr(unary.Op.Type, operand, resultType);
    }

    private BoundLogicalExpr BindLogical(LogicalExpr logical, BindingContext context)
    {
        var left = Bind(logical.Left, context);
        var right = Bind(logical.Right, context);
        return new BoundLogicalExpr(logical.Op.Type, left, right, typeof(bool));
    }

    private BoundNullCoalesceExpr BindNullCoalesce(NullCoalesceExpr nullCoalesce, BindingContext context)
    {
        var left = Bind(nullCoalesce.Left, context);
        var right = Bind(nullCoalesce.Right, context);
        return new BoundNullCoalesceExpr(
            left,
            right,
            GetCommonType(left.StaticType, right.StaticType));
    }

    private BoundConditionalExpr BindConditional(ConditionalExpr conditional, BindingContext context)
    {
        var condition = Bind(conditional.Condition, context);
        var thenBranch = Bind(conditional.ThenBranch, context);
        var elseBranch = Bind(conditional.ElseBranch, context);
        return new BoundConditionalExpr(
            condition,
            thenBranch,
            elseBranch,
            GetCommonType(thenBranch.StaticType, elseBranch.StaticType));
    }

    private BoundBlockExpr BindBlock(BlockExpr block, BindingContext context)
    {
        var blockScope = context.CreateChildScope();
        var statements = block.Statements
            .Select(statement => Bind(statement, blockScope))
            .ToImmutableArray();
        var returnExpr = block.ReturnExpr != null ? Bind(block.ReturnExpr, blockScope) : null;
        var staticType = returnExpr?.StaticType ?? typeof(object);
        return new BoundBlockExpr(statements, returnExpr, staticType);
    }

    private BoundIfStatementExpr BindIfStatement(IfStatementExpr ifStatement, BindingContext context)
    {
        var condition = Bind(ifStatement.Condition, context);
        var thenScope = context.CreateChildScope();
        var thenStatements = ifStatement.ThenStatements
            .Select(statement => Bind(statement, thenScope))
            .ToImmutableArray();

        ImmutableArray<BoundExpr> elseStatements = ImmutableArray<BoundExpr>.Empty;
        if (ifStatement.ElseStatements is { Count: > 0 } elseSource)
        {
            var elseScope = context.CreateChildScope();
            elseStatements = [
                ..elseSource
                    .Select(statement => Bind(statement, elseScope))
            ];
        }

        return new BoundIfStatementExpr(condition, thenStatements, elseStatements, typeof(object));
    }

    private BoundVariableDeclExpr BindVariableDecl(VariableDeclExpr variableDecl, BindingContext context)
    {
        var initializer = Bind(variableDecl.Initializer, context);
        var declaredType = variableDecl.DeclaredType != null
            ? context.RuntimeContext.TypeResolver.ResolveType(variableDecl.DeclaredType.Value.Lexeme)
            : null;
        var staticType = declaredType ?? initializer.StaticType;
        context.DeclareLocal(variableDecl.Name.Lexeme, staticType, variableDecl.IsConst);
        return new BoundVariableDeclExpr(
            variableDecl.Name.Lexeme,
            initializer,
            declaredType,
            staticType,
            IsConst: variableDecl.IsConst);
    }

    private BoundWhileExpr BindWhile(WhileStatementExpr whileStatement, BindingContext context)
    {
        var condition = Bind(whileStatement.Condition, context);
        var bodyScope = context.CreateChildScope();
        var body = whileStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundWhileExpr(condition, body, typeof(object));
    }

    private BoundForExpr BindFor(ForStatementExpr forStatement, BindingContext context)
    {
        var loopScope = context.CreateChildScope();
        var initializers = forStatement.Initializers
            .Select(initializer => Bind(initializer, loopScope))
            .ToImmutableArray();
        var condition = forStatement.Condition != null
            ? Bind(forStatement.Condition, loopScope)
            : null;
        var increments = forStatement.Increments
            .Select(increment => Bind(increment, loopScope))
            .ToImmutableArray();

        var bodyScope = loopScope.CreateChildScope();
        var body = forStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForExpr(initializers, condition, increments, body, typeof(object));
    }

    private BoundDoWhileExpr BindDoWhile(DoWhileStatementExpr doWhileStatement, BindingContext context)
    {
        var bodyScope = context.CreateChildScope();
        var body = doWhileStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        var condition = Bind(doWhileStatement.Condition, context);
        return new BoundDoWhileExpr(body, condition, typeof(object));
    }

    private BoundForEachExpr BindForEach(ForEachStatementExpr forEachStatement, BindingContext context)
    {
        var collection = Bind(forEachStatement.Collection, context);
        var elementType = InferElementType(collection.StaticType);
        var bodyScope = context.CreateChildScope();
        bodyScope.DeclareLocal(forEachStatement.VariableName.Lexeme, elementType);
        var body = forEachStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForEachExpr(forEachStatement.VariableName.Lexeme, collection, body, elementType, typeof(object));
    }

    private static Type InferElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType.GetGenericArguments()[0];

        return typeof(object);
    }

    private BoundUsingStatementExpr BindUsingStatement(UsingStatementExpr usingStatement, BindingContext context)
    {
        var resource = Bind(usingStatement.ResourceDeclaration, context);
        var body = Bind(usingStatement.Body, context.CreateChildScope());
        return new BoundUsingStatementExpr(resource, body, typeof(object));
    }

    private BoundLockStatementExpr BindLockStatement(LockStatementExpr lockStatement, BindingContext context)
    {
        var lockObject = Bind(lockStatement.LockObject, context);
        var body = Bind(lockStatement.Body, context.CreateChildScope());
        return new BoundLockStatementExpr(lockObject, body, typeof(object));
    }

    private BoundTryCatchFinallyExpr BindTryCatchFinally(TryCatchFinallyExpr tryCatchFinally, BindingContext context)
    {
        var tryScope = context.CreateChildScope();
        var tryBody = tryCatchFinally.TryBody
            .Select(statement => Bind(statement, tryScope))
            .ToImmutableArray();

        var catches = tryCatchFinally.CatchClauses
            .Select(catchClause =>
            {
                var catchScope = context.CreateChildScope();
                if (catchClause.VariableName != null)
                {
                    var exceptionType = catchClause.ExceptionTypeName != null
                        ? context.RuntimeContext.TypeResolver.TryResolveType(catchClause.ExceptionTypeName) ?? typeof(Exception)
                        : typeof(Exception);
                    catchScope.DeclareLocal(catchClause.VariableName.Value.Lexeme, exceptionType);
                }

                var whenGuard = catchClause.WhenGuard != null
                    ? Bind(catchClause.WhenGuard, catchScope)
                    : null;

                var body = catchClause.Body
                    .Select(statement => Bind(statement, catchScope))
                    .ToImmutableArray();

                return new BoundCatchClause(
                    catchClause.ExceptionTypeName,
                    catchClause.VariableName?.Lexeme,
                    whenGuard,
                    body);
            })
            .ToImmutableArray();

        ImmutableArray<BoundExpr> finallyBody = ImmutableArray<BoundExpr>.Empty;
        if (tryCatchFinally.FinallyBody != null)
        {
            var finallyScope = context.CreateChildScope();
            finallyBody = [
                ..tryCatchFinally.FinallyBody
                    .Select(statement => Bind(statement, finallyScope))
            ];
        }

        return new BoundTryCatchFinallyExpr(tryBody, catches, finallyBody, typeof(object));
    }

    private BoundThrowExpr BindThrowExpr(ThrowExpr throwExpr, BindingContext context)
    {
        var expression = Bind(throwExpr.Expression, context);
        return new BoundThrowExpr(expression, typeof(object));
    }

    private BoundSwitchStatementExpr BindSwitchStatement(SwitchStatementExpr switchStatement, BindingContext context)
    {
        var expression = Bind(switchStatement.Expression, context);
        var cases = switchStatement.Cases
            .Select(switchCase =>
            {
                var caseScope = context.CreateChildScope();
                var guard = switchCase.WhenGuard != null
                    ? Bind(switchCase.WhenGuard, caseScope)
                    : null;
                var statements = switchCase.Statements
                    .Select(statement => Bind(statement, caseScope))
                    .ToImmutableArray();
                return new BoundSwitchCase(switchCase.CasePattern, guard, statements);
            })
            .ToImmutableArray();
        return new BoundSwitchStatementExpr(expression, cases, typeof(object));
    }

    private BoundSwitchExpressionExpr BindSwitchExpression(SwitchExpressionExpr switchExpression, BindingContext context)
    {
        var expression = Bind(switchExpression.Expression, context);
        var arms = switchExpression.Arms
            .Select(arm =>
            {
                var armScope = context.CreateChildScope();
                var whenGuard = arm.WhenGuard != null ? Bind(arm.WhenGuard, armScope) : null;
                var value = Bind(arm.Value, armScope);
                return new BoundSwitchExpressionArm(arm.Pattern, whenGuard, value);
            })
            .ToImmutableArray();

        var staticType = typeof(object);
        if (arms.Length > 0)
        {
            staticType = arms[0].Value.StaticType;
            for (var i = 1; i < arms.Length; i++)
                staticType = GetCommonType(staticType, arms[i].Value.StaticType);
        }

        return new BoundSwitchExpressionExpr(expression, arms, staticType);
    }

    private BoundAssignExpr BindAssign(AssignExpr assign, BindingContext context)
    {
        EnsureVariableIsAssignable(assign.Name.Lexeme, context);
        var value = Bind(assign.Value, context);
        var staticType = context.TryGetVariableType(assign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        return new BoundAssignExpr(assign.Name.Lexeme, value, staticType);
    }

    private BoundNullCoalesceAssignExpr BindNullCoalesceAssign(NullCoalesceAssignExpr nullCoalesceAssign, BindingContext context)
    {
        EnsureVariableIsAssignable(nullCoalesceAssign.Name.Lexeme, context);
        var value = Bind(nullCoalesceAssign.Value, context);
        var staticType = context.TryGetVariableType(nullCoalesceAssign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        return new BoundNullCoalesceAssignExpr(nullCoalesceAssign.Name.Lexeme, value, staticType);
    }

    private BoundCompoundAssignExpr BindCompoundAssign(CompoundAssignExpr compoundAssign, BindingContext context)
    {
        EnsureVariableIsAssignable(compoundAssign.Name.Lexeme, context);
        var value = Bind(compoundAssign.Value, context);
        var staticType = context.TryGetVariableType(compoundAssign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        return new BoundCompoundAssignExpr(compoundAssign.Name.Lexeme, compoundAssign.Op.Type, value, staticType);
    }

    private BoundIncrementDecrementExpr BindIncrementDecrement(IncrementDecrementExpr incrementDecrement, BindingContext context)
    {
        EnsureVariableIsAssignable(incrementDecrement.Name.Lexeme, context);
        var staticType = context.TryGetVariableType(incrementDecrement.Name.Lexeme, out var variableType)
            ? variableType
            : typeof(object);
        return new BoundIncrementDecrementExpr(
            incrementDecrement.Name.Lexeme,
            incrementDecrement.Op.Type,
            incrementDecrement.IsPrefix,
            staticType);
    }

    private static void EnsureVariableIsAssignable(string variableName, BindingContext context)
    {
        if (context.IsReadOnlyLocal(variableName))
            throw new CsEvalException(DiagnosticDescriptors.AssignmentRequiresVariable);
    }

    private BoundMemberAssignExpr BindMemberAssign(MemberAssignExpr memberAssign, BindingContext context)
    {
        var target = Bind(memberAssign.Object, context);
        var value = Bind(memberAssign.Value, context);
        return new BoundMemberAssignExpr(target, memberAssign.Name.Lexeme, value, value.StaticType);
    }

    private BoundIndexAssignExpr BindIndexAssign(IndexAssignExpr indexAssign, BindingContext context)
    {
        var target = Bind(indexAssign.Object, context);
        var index = Bind(indexAssign.Index, context);
        var value = Bind(indexAssign.Value, context);
        return new BoundIndexAssignExpr(target, index, value, value.StaticType);
    }

    private BoundMemberCompoundAssignExpr BindMemberCompoundAssign(MemberCompoundAssignExpr memberCompoundAssign, BindingContext context)
    {
        var target = Bind(memberCompoundAssign.Object, context);
        var value = Bind(memberCompoundAssign.Value, context);
        return new BoundMemberCompoundAssignExpr(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            value,
            typeof(object));
    }

    private BoundIndexCompoundAssignExpr BindIndexCompoundAssign(IndexCompoundAssignExpr indexCompoundAssign, BindingContext context)
    {
        var target = Bind(indexCompoundAssign.Object, context);
        var index = Bind(indexCompoundAssign.Index, context);
        var value = Bind(indexCompoundAssign.Value, context);
        return new BoundIndexCompoundAssignExpr(target, index, indexCompoundAssign.Operator, value, typeof(object));
    }

    private BoundMemberNullCoalesceAssignExpr BindMemberNullCoalesceAssign(
        MemberNullCoalesceAssignExpr memberNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(memberNullCoalesceAssign.Object, context);
        var value = Bind(memberNullCoalesceAssign.Value, context);
        return new BoundMemberNullCoalesceAssignExpr(target, memberNullCoalesceAssign.MemberName, value, typeof(object));
    }

    private BoundIndexNullCoalesceAssignExpr BindIndexNullCoalesceAssign(
        IndexNullCoalesceAssignExpr indexNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(indexNullCoalesceAssign.Object, context);
        var index = Bind(indexNullCoalesceAssign.Index, context);
        var value = Bind(indexNullCoalesceAssign.Value, context);
        return new BoundIndexNullCoalesceAssignExpr(target, index, value, typeof(object));
    }

    private BoundMemberIncrementExpr BindMemberIncrement(MemberIncrementExpr memberIncrement, BindingContext context)
    {
        var target = Bind(memberIncrement.Object, context);
        return new BoundMemberIncrementExpr(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsPrefix,
            memberIncrement.IsIncrement,
            typeof(object));
    }

    private BoundIndexIncrementExpr BindIndexIncrement(IndexIncrementExpr indexIncrement, BindingContext context)
    {
        var target = Bind(indexIncrement.Object, context);
        var index = Bind(indexIncrement.Index, context);
        return new BoundIndexIncrementExpr(
            target,
            index,
            indexIncrement.IsPrefix,
            indexIncrement.IsIncrement,
            typeof(object));
    }

    private BoundReturnExpr BindReturn(ReturnExpr returnExpr, BindingContext context)
    {
        var value = returnExpr.Value != null ? Bind(returnExpr.Value, context) : null;
        return new BoundReturnExpr(value, value?.StaticType ?? typeof(object));
    }

    private static BoundLambdaExpr BindLambda(LambdaExpr lambda)
    {
        return new BoundLambdaExpr(
            [..lambda.Parameters.Select(static parameter => parameter.Name.Lexeme)],
            lambda.Body,
            typeof(LambdaValue));
    }

    private BoundExpr BindPipeline(PipelineExpr pipeline, BindingContext context)
    {
        if (pipeline.Right is IdentifierExpr rightIdentifier)
        {
            var call = new CallExpr(rightIdentifier, [pipeline.Left]);
            return BindCall(call, context);
        }

        if (pipeline.Right is CallExpr rightCall)
        {
            var args = new List<Expr>(rightCall.Arguments.Count + 1) { pipeline.Left };
            args.AddRange(rightCall.Arguments);
            var call = new CallExpr(rightCall.Callee, args, rightCall.TypeArguments);
            return BindCall(call, context);
        }

        return new BoundPipelineExpr(
            Bind(pipeline.Left, context),
            Bind(pipeline.Right, context),
            typeof(object));
    }

    private BoundIndexFromEndExpr BindIndexFromEnd(IndexFromEndExpr expr, BindingContext context)
    {
        var operand = Bind(expr.Operand, context);
        return new BoundIndexFromEndExpr(operand, typeof(Index));
    }

    private BoundRangeExpr BindRange(RangeExpr rangeExpr, BindingContext context)
    {
        var start = Bind(rangeExpr.Start, context);
        var end = Bind(rangeExpr.End, context);
        return new BoundRangeExpr(start, end, rangeExpr.ExclusiveEnd, typeof(IEnumerable<int>));
    }

    private BoundNamedArgumentExpr BindNamedArgument(NamedArgumentExpr namedArgument, BindingContext context)
    {
        var value = Bind(namedArgument.Value, context);
        return new BoundNamedArgumentExpr(namedArgument.Name.Lexeme, value, typeof(object));
    }

    private static BoundOutArgExpr BindOutArg(OutArgExpr outArg)
    {
        return new BoundOutArgExpr(outArg.VariableName, outArg.TypeName, outArg.IsDiscard, typeof(object));
    }

    private static Type InferBinaryResultType(TokenType op, Type leftType, Type rightType)
    {
        if (op is TokenType.EqualEqual or TokenType.BangEqual or TokenType.EqualEqualEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual or
            TokenType.In or TokenType.Like or TokenType.EqualTilde or TokenType.BangTilde)
        {
            return typeof(bool);
        }

        if (op == TokenType.LessEqualGreater)
            return typeof(int);

        if (op == TokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
            return typeof(string);

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(leftType, rightType, op);

        return typeof(object);
    }

    private static Type InferArithmeticResultType(Type leftType, Type rightType, TokenType op)
    {
        // ECMA-style numeric promotion for arithmetic result typing.
        // Use object only for ambiguous/invalid static mixes where runtime dispatch must decide.
        var normalizedLeft = NormalizeArithmeticType(leftType);
        var normalizedRight = NormalizeArithmeticType(rightType);

        if (op is TokenType.LessLess or TokenType.GreaterGreater or TokenType.GreaterGreaterGreater)
            return normalizedLeft;

        if (normalizedLeft == normalizedRight)
            return normalizedLeft;

        if (normalizedLeft == typeof(decimal) || normalizedRight == typeof(decimal))
            return typeof(decimal);

        if (normalizedLeft == typeof(double) || normalizedRight == typeof(double))
            return typeof(double);

        if (normalizedLeft == typeof(float) || normalizedRight == typeof(float))
            return typeof(float);

        if (normalizedLeft == typeof(ulong) || normalizedRight == typeof(ulong))
        {
            var other = normalizedLeft == typeof(ulong) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(object);
            return typeof(ulong);
        }

        if (normalizedLeft == typeof(long) || normalizedRight == typeof(long))
            return typeof(long);

        if (normalizedLeft == typeof(uint) || normalizedRight == typeof(uint))
        {
            var other = normalizedLeft == typeof(uint) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(long);
            return typeof(uint);
        }

        return typeof(int);
    }

    private static Type NormalizeArithmeticType(Type type)
    {
        if (type == typeof(char))
            return typeof(int);
        return type;
    }

    private static bool IsSignedIntegralType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(int) ||
               type == typeof(long);
    }

    private static Type GetCommonType(Type leftType, Type rightType)
    {
        if (leftType == rightType)
            return leftType;

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return NumericDispatch.GetResultType(leftType, rightType);

        if (leftType.IsAssignableFrom(rightType))
            return leftType;
        if (rightType.IsAssignableFrom(leftType))
            return rightType;

        if (leftType == typeof(string) || rightType == typeof(string))
            return typeof(string);

        return typeof(object);
    }

    private BoundMemberAccessExpr BindMemberAccess(MemberAccessExpr memberAccess, BindingContext context)
    {
        var target = Bind(memberAccess.Object, context);
        var (targetType, isStatic) = ResolveMemberTarget(target);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        BoundMemberPlan? plan;
        try
        {
            plan = memberBinder.BindMemberRead(targetType, memberAccess.Name.Lexeme, isStatic, context.IsCaseSensitive);
        }
        catch (CsEvalException)
        {
            plan = null;
        }

        var staticType = plan?.Member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        return new BoundMemberAccessExpr(target, memberAccess.Name.Lexeme, memberAccess.NullSafe, plan, staticType);
    }

    private BoundIndexAccessExpr BindIndexAccess(IndexAccessExpr indexAccess, BindingContext context)
    {
        var target = Bind(indexAccess.Object, context);
        var index = Bind(indexAccess.Index, context);

        BoundIndexPlan? plan = null;
        var staticType = typeof(object);

        try
        {
            var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
            plan = memberBinder.BindIndexRead(target.StaticType, index.StaticType);
            staticType = plan.ResultType;
        }
        catch (CsEvalException)
        {
            // Keep index access in the bound pipeline with dynamic runtime dispatch when
            // static index planning is not possible (e.g., object-typed targets).
        }

        return new BoundIndexAccessExpr(target, index, plan, indexAccess.NullSafe, staticType);
    }

    private BoundExpr BindCall(CallExpr call, BindingContext context)
    {
        if (TryBindStaticModuleCall(call, context, out var staticModuleCall))
            return staticModuleCall;

        var callee = BindCallCallee(call.Callee, context);
        var arguments = call.Arguments
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();
        var typeArguments = call.TypeArguments?.ToImmutableArray() ?? ImmutableArray<string>.Empty;

        if (callee is BoundMemberAccessExpr { Plan.IsMethodGroup: true } memberAccess &&
            arguments.All(static argument =>
                argument is not BoundLambdaExpr &&
                argument is not BoundNamedArgumentExpr &&
                argument is not BoundOutArgExpr))
        {
            var argumentTypes = arguments.Select(static argument => argument.StaticType).ToArray();
            var callBinder = new CallBinderService(context.RuntimeContext);

            try
            {
                var plan = memberAccess.Plan.IsStatic && memberAccess.Target is BoundLiteralExpr { Value: Type staticDeclaringType }
                    ? callBinder.BindStaticCall(staticDeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive)
                    : callBinder.BindInstanceCall(memberAccess.Plan.DeclaringType, memberAccess.MemberName, argumentTypes, context.IsCaseSensitive);

                return new BoundCallExpr(callee, arguments, plan, plan.SelectedMethod.ReturnType);
            }
            catch (CsEvalException)
            {
                return new BoundInvokeExpr(callee, arguments, typeArguments, typeof(object));
            }
        }

        return new BoundInvokeExpr(callee, arguments, typeArguments, typeof(object));
    }

    private bool TryBindStaticModuleCall(CallExpr call, BindingContext context, out BoundExpr boundCall)
    {
        boundCall = null!;
        if (call.Callee is not MemberAccessExpr { Object: IdentifierExpr moduleIdentifier } memberAccess)
            return false;

        var moduleName = moduleIdentifier.Name.Lexeme;
        if (context.RuntimeContext.Functions.ContainsKey(moduleName))
            return false;

        if (!context.RuntimeContext.Modules.TryGetValue(moduleName, out var moduleInfo))
            return false;

        if (moduleInfo.Instance != null ||
            !moduleInfo.Type.IsAbstract ||
            !moduleInfo.Type.IsSealed)
        {
            return false;
        }

        if (!moduleInfo.Members.TryGetValue(memberAccess.Name.Lexeme, out var moduleMember) ||
            moduleMember is not MethodInfo)
            return false;

        var arguments = call.Arguments
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();
        if (arguments.Any(static argument => argument is BoundLambdaExpr))
            return false;

        var argumentTypes = arguments.Select(static argument => argument.StaticType).ToArray();
        var callBinder = new CallBinderService(context.RuntimeContext);

        BoundCallPlan callPlan;
        try
        {
            callPlan = callBinder.BindStaticCall(
                moduleInfo.Type,
                memberAccess.Name.Lexeme,
                argumentTypes,
                context.IsCaseSensitive) with { IsModuleCall = true };
        }
        catch (CsEvalException)
        {
            return false;
        }

        var callee = new BoundMemberAccessExpr(
            new BoundLiteralExpr(moduleInfo.Type, typeof(Type)),
            memberAccess.Name.Lexeme,
            memberAccess.NullSafe,
            new BoundMemberPlan(
                moduleInfo.Type,
                memberAccess.Name.Lexeme,
                Member: null,
                IsMethodGroup: true,
                IsStatic: true),
            typeof(object));

        boundCall = new BoundCallExpr(callee, arguments, callPlan, callPlan.SelectedMethod.ReturnType);
        return true;
    }

    private BoundExpr BindCallCallee(Expr callee, BindingContext context)
    {
        if (callee is not MemberAccessExpr memberAccess)
            return Bind(callee, context);

        try
        {
            return BindMemberAccess(memberAccess, context);
        }
        catch (BindingNotSupportedException)
        {
            // Preserve call semantics for extension/dynamic method candidates by deferring
            // member resolution to invocation-time dispatch while staying in the bound pipeline.
            var target = Bind(memberAccess.Object, context);
            return new BoundMemberAccessExpr(
                target,
                memberAccess.Name.Lexeme,
                memberAccess.NullSafe,
                Plan: null,
                StaticType: typeof(object));
        }
    }

    private static (Type TargetType, bool IsStatic) ResolveMemberTarget(BoundExpr target)
    {
        if (target is BoundLiteralExpr { Value: Type staticTargetType })
            return (staticTargetType, true);

        return (target.StaticType, false);
    }
}
