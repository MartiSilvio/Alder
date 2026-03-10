using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;
using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

namespace CsEval.Interpretation;

internal sealed class BoundEvaluator
{
    private CsEvalContext _context;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly Stack<Exception> _caughtExceptions = new();
    private int _breakContextDepth;
    private int _loopDepth;
    private bool _isChecked;

    public BoundEvaluator(
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken = default)
    {
        _context = context;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public object? Evaluate(BoundExpr expr)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        return expr switch
        {
            BoundLiteralExpr literal => literal.Value,
            BoundIdentifierExpr identifier => IdentifierRuntime.ResolveIdentifier(identifier.Name, _context, _options),
            BoundCastExpr cast => EvaluateCast(cast),
            BoundAsExpr asExpr => EvaluateAs(asExpr),
            BoundIsPatternExpr isPattern => EvaluateIsPattern(isPattern),
            BoundArrayLiteralExpr arrayLiteral => EvaluateArrayLiteral(arrayLiteral),
            BoundObjectLiteralExpr objectLiteral => EvaluateObjectLiteral(objectLiteral),
            BoundSpreadExpr spread => EvaluateSpread(spread),
            BoundSliceExpr slice => EvaluateSlice(slice),
            BoundObjectCreationExpr objectCreation => EvaluateObjectCreation(objectCreation),
            BoundTypedArrayCreationExpr typedArrayCreation => EvaluateTypedArrayCreation(typedArrayCreation),
            BoundTypedArrayLiteralExpr typedArrayLiteral => EvaluateTypedArrayLiteral(typedArrayLiteral),
            BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation => EvaluateMultiDimTypedArrayCreation(multiDimTypedArrayCreation),
            BoundTupleExpr tuple => EvaluateTuple(tuple),
            BoundDeconstructionExpr deconstruction => EvaluateDeconstruction(deconstruction),
            BoundInterpolatedStringExpr interpolatedString => EvaluateInterpolatedString(interpolatedString),
            BoundUnaryExpr unary => EvaluateUnary(unary),
            BoundBinaryExpr binary => EvaluateBinary(binary),
            BoundLogicalExpr logical => EvaluateLogical(logical),
            BoundNullCoalesceExpr nullCoalesce => EvaluateNullCoalesce(nullCoalesce),
            BoundConditionalExpr conditional => EvaluateConditional(conditional),
            BoundBlockExpr block => EvaluateBlock(block),
            BoundIfStatementExpr ifStatement => EvaluateIfStatement(ifStatement),
            BoundWhileExpr whileExpr => EvaluateWhile(whileExpr),
            BoundForExpr forExpr => EvaluateFor(forExpr),
            BoundDoWhileExpr doWhileExpr => EvaluateDoWhile(doWhileExpr),
            BoundForEachExpr forEachExpr => EvaluateForEach(forEachExpr),
            BoundUsingStatementExpr usingStatement => EvaluateUsingStatement(usingStatement),
            BoundLockStatementExpr lockStatement => EvaluateLockStatement(lockStatement),
            BoundSwitchStatementExpr switchStatement => EvaluateSwitch(switchStatement),
            BoundSwitchExpressionExpr switchExpression => EvaluateSwitchExpression(switchExpression),
            BoundCheckedExpr checkedExpr => EvaluateChecked(checkedExpr),
            BoundChainedComparisonExpr chainedComparison => EvaluateChainedComparison(chainedComparison),
            BoundBreakExpr => EvaluateBreak(),
            BoundContinueExpr => EvaluateContinue(),
            BoundVariableDeclExpr variableDecl => EvaluateVariableDecl(variableDecl),
            BoundAssignExpr assign => EvaluateAssign(assign),
            BoundNullCoalesceAssignExpr nullCoalesceAssign => EvaluateNullCoalesceAssign(nullCoalesceAssign),
            BoundCompoundAssignExpr compoundAssign => EvaluateCompoundAssign(compoundAssign),
            BoundIncrementDecrementExpr incrementDecrement => EvaluateIncrementDecrement(incrementDecrement),
            BoundMemberAssignExpr memberAssign => EvaluateMemberAssign(memberAssign),
            BoundIndexAssignExpr indexAssign => EvaluateIndexAssign(indexAssign),
            BoundMemberCompoundAssignExpr memberCompoundAssign => EvaluateMemberCompoundAssign(memberCompoundAssign),
            BoundIndexCompoundAssignExpr indexCompoundAssign => EvaluateIndexCompoundAssign(indexCompoundAssign),
            BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign => EvaluateMemberNullCoalesceAssign(memberNullCoalesceAssign),
            BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign => EvaluateIndexNullCoalesceAssign(indexNullCoalesceAssign),
            BoundMemberIncrementExpr memberIncrement => EvaluateMemberIncrement(memberIncrement),
            BoundIndexIncrementExpr indexIncrement => EvaluateIndexIncrement(indexIncrement),
            BoundThrowExpr throwExpr => EvaluateThrow(throwExpr),
            BoundTryCatchFinallyExpr tryCatchFinally => EvaluateTryCatchFinally(tryCatchFinally),
            BoundThrowStatementExpr => EvaluateThrowStatement(),
            BoundReturnExpr returnExpr => EvaluateReturn(returnExpr),
            BoundMemberAccessExpr memberAccess => EvaluateMemberAccess(memberAccess),
            BoundIndexAccessExpr indexAccess => EvaluateIndexAccess(indexAccess),
            BoundMultiDimIndexAccessExpr multiDimIndexAccess => EvaluateMultiDimIndexAccess(multiDimIndexAccess),
            BoundMultiDimIndexAssignExpr multiDimIndexAssign => EvaluateMultiDimIndexAssign(multiDimIndexAssign),
            BoundNamedArgumentExpr namedArgument => EvaluateNamedArgument(namedArgument),
            BoundOutArgExpr outArg => EvaluateOutArg(outArg),
            BoundCallExpr call => EvaluateCall(call),
            BoundInvokeExpr invoke => EvaluateInvoke(invoke),
            BoundLambdaExpr lambda => EvaluateLambda(lambda),
            BoundPipelineExpr pipeline => EvaluatePipeline(pipeline),
            BoundRangeExpr range => EvaluateRange(range),
            _ => throw new BindingNotSupportedException(
                $"Bound execution for node '{expr.GetType().Name}' is not implemented")
        };
    }

    private object? EvaluateCast(BoundCastExpr cast)
    {
        var value = Evaluate(cast.Expression);
        return TypeHelpers.ExplicitCast(value, cast.TargetType, cast.SourceStaticType, _isChecked);
    }

    private object? EvaluateAs(BoundAsExpr asExpr)
    {
        var value = Evaluate(asExpr.Expression);
        return TypeHelpers.TryAs(value, asExpr.TargetType);
    }

    private object? EvaluateIsPattern(BoundIsPatternExpr isPattern)
    {
        var value = Evaluate(isPattern.Expression);
        return MatchPattern(value, isPattern.Pattern);
    }

    private object? EvaluateArrayLiteral(BoundArrayLiteralExpr arrayLiteral)
    {
        var result = new List<object?>(arrayLiteral.Elements.Length);
        foreach (var element in arrayLiteral.Elements)
        {
            if (element is BoundSpreadExpr spread)
            {
                var spreadValue = Evaluate(spread.Expression);
                SpreadHelpers.SpreadIntoList(result, spreadValue);
            }
            else
            {
                result.Add(Evaluate(element));
            }
        }

        return SpreadHelpers.CreateTypedArray(result);
    }

    private static object? EvaluateSpread(BoundSpreadExpr _)
    {
        throw new CsEvalException("Spread operator can only be used in array or object literals");
    }

    private object? EvaluateObjectLiteral(BoundObjectLiteralExpr objectLiteral)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var property in objectLiteral.Properties)
        {
            if (property.IsSpread)
            {
                var spreadValue = Evaluate(property.Value);
                SpreadHelpers.SpreadIntoDict(result, spreadValue, _context);
                continue;
            }

            result[property.PropertyName!] = Evaluate(property.Value);
        }

        return result;
    }

    private object? EvaluateSlice(BoundSliceExpr slice)
    {
        var target = Evaluate(slice.Target);
        var start = slice.Start != null ? Evaluate(slice.Start) : null;
        var end = slice.End != null ? Evaluate(slice.End) : null;
        var step = slice.Step != null ? Evaluate(slice.Step) : null;
        return MemberAccess.GetSlice(target, start, end, step, _options);
    }

    private object? EvaluateObjectCreation(BoundObjectCreationExpr objectCreation)
    {
        var args = new object?[objectCreation.Arguments.Length];
        for (var i = 0; i < objectCreation.Arguments.Length; i++)
            args[i] = Evaluate(objectCreation.Arguments[i]);

        var type = _context.TypeResolver.ResolveType(objectCreation.TypeName);
        var result = ConstructionRuntime.InvokeConstructor(type, args);

        foreach (var entry in objectCreation.InitializerEntries)
        {
            var value = Evaluate(entry.Value);
            if (entry.PropertyName != null)
            {
                MemberAccess.SetMember(result!, entry.PropertyName, value, _options, _context);
            }
            else
            {
                var addMethod = result!.GetType().GetMethod("Add");
                if (addMethod == null)
                    throw new CsEvalException(
                        $"Type '{result.GetType().Name}' does not have an 'Add' method for collection initializer");
                addMethod.Invoke(result, [value]);
            }
        }

        return result;
    }

    private object? EvaluateTypedArrayCreation(BoundTypedArrayCreationExpr typedArrayCreation)
    {
        var sizeValue = Evaluate(typedArrayCreation.Size);
        var size = Convert.ToInt32(sizeValue);
        var elementType = _context.TypeResolver.ResolveType(typedArrayCreation.ElementTypeName);
        return Array.CreateInstance(elementType, size);
    }

    private object? EvaluateTypedArrayLiteral(BoundTypedArrayLiteralExpr typedArrayLiteral)
    {
        var elementType = _context.TypeResolver.ResolveType(typedArrayLiteral.ElementTypeName);
        var array = Array.CreateInstance(elementType, typedArrayLiteral.Elements.Length);
        for (var i = 0; i < typedArrayLiteral.Elements.Length; i++)
        {
            var value = Evaluate(typedArrayLiteral.Elements[i]);
            array.SetValue(value, i);
        }

        return array;
    }

    private object? EvaluateTuple(BoundTupleExpr tuple)
    {
        var values = new object?[tuple.Elements.Length];
        for (var i = 0; i < tuple.Elements.Length; i++)
            values[i] = Evaluate(tuple.Elements[i]);
        return ConstructionRuntime.CreateTuple(values);
    }

    private object? EvaluateMultiDimTypedArrayCreation(BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation)
    {
        var sizes = new int[multiDimTypedArrayCreation.Sizes.Length];
        for (var i = 0; i < multiDimTypedArrayCreation.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(Evaluate(multiDimTypedArrayCreation.Sizes[i]));
        var elementType = _context.TypeResolver.ResolveType(multiDimTypedArrayCreation.ElementTypeName);
        return Array.CreateInstance(elementType, sizes);
    }

    private object? EvaluateDeconstruction(BoundDeconstructionExpr deconstruction)
    {
        var value = Evaluate(deconstruction.ValueExpression);

        if (value is System.Runtime.CompilerServices.ITuple tuple)
        {
            if (tuple.Length != deconstruction.VariableNames.Length)
            {
                throw new CsEvalException(
                    $"Deconstruction requires {deconstruction.VariableNames.Length} values but tuple has {tuple.Length} elements");
            }

            for (var i = 0; i < deconstruction.VariableNames.Length; i++)
            {
                var elementValue = tuple[i];
                var elementType = elementValue?.GetType() ?? typeof(object);
                _context.DefineNew(deconstruction.VariableNames[i], elementValue, elementType);
            }

            return value;
        }

        if (value != null)
        {
            var deconstructed = ConstructionRuntime.TryDeconstruct(value, deconstruction.VariableNames.Length);
            if (deconstructed != null)
            {
                for (var i = 0; i < deconstruction.VariableNames.Length; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    _context.DefineNew(deconstruction.VariableNames[i], elementValue, elementType);
                }

                return value;
            }
        }

        throw new CsEvalException($"Cannot deconstruct type '{value?.GetType().Name ?? "null"}'");
    }

    private object? EvaluateInterpolatedString(BoundInterpolatedStringExpr interpolatedString)
    {
        var sb = new StringBuilder();
        foreach (var part in interpolatedString.Parts)
        {
            switch (part)
            {
                case BoundInterpolatedTextPart text:
                    sb.Append(text.Text);
                    break;
                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var value = Evaluate(expressionPart.Expression);
                    if (expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null)
                    {
                        var format = "{0";
                        if (expressionPart.AlignmentSpecifier != null)
                            format += "," + expressionPart.AlignmentSpecifier;
                        if (expressionPart.FormatSpecifier != null)
                            format += ":" + expressionPart.FormatSpecifier;
                        format += "}";
                        sb.Append(string.Format(format, value));
                    }
                    else
                    {
                        sb.Append(value?.ToString() ?? string.Empty);
                    }

                    break;
                }
                default:
                    throw new BindingNotSupportedException(
                        $"Bound interpolated part '{part.GetType().Name}' is not implemented");
            }
        }

        return sb.ToString();
    }

    private object? EvaluateUnary(BoundUnaryExpr unary)
    {
        var operand = Evaluate(unary.Operand);
        return unary.Operator switch
        {
            TokenType.Minus => Operators.Negate(operand, _isChecked),
            TokenType.Plus => Operators.UnaryPlus(operand),
            TokenType.Bang => Operators.LogicalNot(operand),
            TokenType.Tilde => Operators.BitwiseNot(operand),
            _ => throw new BindingNotSupportedException(
                $"Bound unary operator '{unary.Operator}' is not implemented")
        };
    }

    private object? EvaluateBinary(BoundBinaryExpr binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        (left, right) = NumericPromotionRuntime.ApplyConstantNumericPromotion(
            left,
            binary.Left is BoundLiteralExpr,
            right,
            binary.Right is BoundLiteralExpr);

        return binary.Operator switch
        {
            TokenType.Plus => Operators.Add(left, right, _options, _context, _isChecked),
            TokenType.Minus => Operators.Subtract(left, right, _isChecked),
            TokenType.Star => Operators.Multiply(left, right, _options, _isChecked),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.EqualEqualEqual => Operators.Equals(left, right),
            TokenType.BangEqualEqual => Operators.NotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, _options),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, _options),
            TokenType.Greater => Operators.GreaterThan(left, right, _options),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, _options),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            TokenType.GreaterGreaterGreater => Operators.UnsignedRightShift(left, right),
            TokenType.StarStar => Operators.Power(left, right),
            TokenType.In => Operators.InOperator(left, right),
            TokenType.Like => Operators.Like(left, right),
            TokenType.EqualTilde => Operators.RegexMatch(left, right),
            TokenType.BangTilde => Operators.RegexNotMatch(left, right),
            TokenType.LessEqualGreater => Operators.Spaceship(left, right),
            _ => throw new BindingNotSupportedException(
                $"Bound binary operator '{binary.Operator}' is not implemented")
        };
    }

    private object? EvaluateLogical(BoundLogicalExpr logical)
    {
        var left = Evaluate(logical.Left);
        var opLexeme = TokenLexemes.GetCanonical(logical.Operator);
        if (left is not bool leftBool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                TypeNameFormatter.Of(left),
                GetLogicalExpressionTypeName(logical.Right));
        }

        if (logical.Operator == TokenType.PipePipe)
        {
            if (leftBool)
                return true;
        }
        else if (logical.Operator == TokenType.AmpAmp)
        {
            if (!leftBool)
                return false;
        }
        else
        {
            throw new BindingNotSupportedException(
                $"Bound logical operator '{logical.Operator}' is not implemented");
        }

        var right = Evaluate(logical.Right);
        if (right is not bool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        }

        return (bool)right;
    }

    private object? EvaluateNullCoalesce(BoundNullCoalesceExpr nullCoalesce)
    {
        var left = Evaluate(nullCoalesce.Left);
        return left ?? Evaluate(nullCoalesce.Right);
    }

    private object? EvaluateConditional(BoundConditionalExpr conditional)
    {
        var condition = Evaluate(conditional.Condition);
        var result = TypeHelpers.RequireBoolean(condition)
            ? Evaluate(conditional.ThenBranch)
            : Evaluate(conditional.ElseBranch);

        var thenType = conditional.ThenBranch.StaticType;
        var elseType = conditional.ElseBranch.StaticType;
        if (result != null &&
            thenType != typeof(object) &&
            elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) &&
            TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var resultType = NumericDispatch.GetResultType(thenType, elseType);
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }

    private object? EvaluateBlock(BoundBlockExpr block)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var statement in block.Statements)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                var result = Evaluate(statement);
                if (result is ControlFlowSignal signal)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return)
                        return signal.Value;
                    return result;
                }
            }

            return block.ReturnExpr != null ? Evaluate(block.ReturnExpr) : null;
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? EvaluateVariableDecl(BoundVariableDeclExpr variableDecl)
    {
        var value = Evaluate(variableDecl.Initializer);
        return AssignmentRuntime.DefineVariable(variableDecl.Name, value, variableDecl.DeclaredType, _context);
    }

    private object? EvaluateIfStatement(BoundIfStatementExpr ifStatement)
    {
        var condition = Evaluate(ifStatement.Condition);
        if (TypeHelpers.RequireBoolean(condition))
        {
            return EvaluateBranch(ifStatement.ThenStatements);
        }

        if (!ifStatement.ElseStatements.IsDefaultOrEmpty)
        {
            return EvaluateBranch(ifStatement.ElseStatements);
        }

        return null;
    }

    private object? EvaluateBranch(IEnumerable<BoundExpr> statements)
    {
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var statement in statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = Evaluate(statement);
                if (result is ControlFlowSignal)
                    return result;
            }

            return null;
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? EvaluateAssign(BoundAssignExpr assign)
    {
        var value = Evaluate(assign.Value);
        ExecutionRuntime.CheckAllowAssignment(_options, $"{assign.Name} = ...");
        value = AssignmentRuntime.ValidateVariableAssignment(assign.Name, value, _context);
        _context.Set(assign.Name, value);
        return value;
    }

    private object? EvaluateNullCoalesceAssign(BoundNullCoalesceAssignExpr nullCoalesceAssign)
    {
        var name = nullCoalesceAssign.Name;
        ExecutionRuntime.CheckNullCoalesceAssignAllowed(name, _context);

        var currentValue = _context.Get(name);
        if (currentValue != null)
            return currentValue;

        ExecutionRuntime.CheckAllowAssignment(_options, $"{name} ??= ...");

        var newValue = Evaluate(nullCoalesceAssign.Value);
        _context.Set(name, newValue);
        return newValue;
    }

    private object? EvaluateCompoundAssign(BoundCompoundAssignExpr compoundAssign)
    {
        var rightValue = Evaluate(compoundAssign.Value);
        return AssignmentRuntime.ApplyCompoundAssign(
            compoundAssign.Name,
            compoundAssign.Operator,
            rightValue,
            _context,
            _options,
            _isChecked);
    }

    private object? EvaluateIncrementDecrement(BoundIncrementDecrementExpr incrementDecrement)
    {
        return AssignmentRuntime.ApplyIncrementDecrement(
            incrementDecrement.Name,
            incrementDecrement.Operator == TokenType.PlusPlus,
            incrementDecrement.IsPrefix,
            _context,
            _options,
            _isChecked);
    }

    private object? EvaluateMemberAssign(BoundMemberAssignExpr memberAssign)
    {
        var target = Evaluate(memberAssign.Target);
        var value = Evaluate(memberAssign.Value);
        return AssignmentRuntime.ApplyMemberAssign(target, memberAssign.MemberName, value, _options, _context);
    }

    private object? EvaluateIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        var target = Evaluate(indexAssign.Target);
        var index = Evaluate(indexAssign.Index);
        var value = Evaluate(indexAssign.Value);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, _options);
    }

    private object? EvaluateMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
        var target = Evaluate(memberCompoundAssign.Target);
        var rightValue = Evaluate(memberCompoundAssign.Value);
        return AssignmentRuntime.ApplyMemberCompoundAssign(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            rightValue,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
        var target = Evaluate(indexCompoundAssign.Target);
        var index = Evaluate(indexCompoundAssign.Index);
        var rightValue = Evaluate(indexCompoundAssign.Value);
        return AssignmentRuntime.ApplyIndexCompoundAssign(
            target,
            index,
            indexCompoundAssign.Operator,
            rightValue,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign)
    {
        var target = Evaluate(memberNullCoalesceAssign.Target);
        var newValue = Evaluate(memberNullCoalesceAssign.Value);
        return AssignmentRuntime.ApplyMemberNullCoalesceAssign(
            target,
            memberNullCoalesceAssign.MemberName,
            newValue,
            _options,
            _context);
    }

    private object? EvaluateIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign)
    {
        var target = Evaluate(indexNullCoalesceAssign.Target);
        var index = Evaluate(indexNullCoalesceAssign.Index);
        var newValue = Evaluate(indexNullCoalesceAssign.Value);
        return AssignmentRuntime.ApplyIndexNullCoalesceAssign(target, index, newValue, _options);
    }

    private object? EvaluateMemberIncrement(BoundMemberIncrementExpr memberIncrement)
    {
        var target = Evaluate(memberIncrement.Target);
        return AssignmentRuntime.ApplyMemberIncrement(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsIncrement,
            memberIncrement.IsPrefix,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateIndexIncrement(BoundIndexIncrementExpr indexIncrement)
    {
        var target = Evaluate(indexIncrement.Target);
        var index = Evaluate(indexIncrement.Index);
        return AssignmentRuntime.ApplyIndexIncrement(
            target,
            index,
            indexIncrement.IsIncrement,
            indexIncrement.IsPrefix,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateThrow(BoundThrowExpr throwExpr)
    {
        var result = Evaluate(throwExpr.Expression);
        var exception = ExecutionRuntime.ValidateThrowOperand(result);
        throw exception;
    }

    private object? EvaluateTryCatchFinally(BoundTryCatchFinallyExpr tryCatchFinally)
    {
        object? result = null;
        Exception? unhandledException = null;
        ControlFlowSignal? pendingSignal = null;

        try
        {
            foreach (var statement in tryCatchFinally.TryBody)
            {
                result = Evaluate(statement);
                if (result is ControlFlowSignal signal)
                {
                    pendingSignal = signal;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            var (handled, catchResult, catchSignal) = TryMatchCatchClause(tryCatchFinally.CatchClauses, ex);
            if (handled)
            {
                result = catchResult;
                pendingSignal = catchSignal;
            }
            else
            {
                unhandledException = ex;
            }
        }
        finally
        {
            foreach (var statement in tryCatchFinally.FinallyBody)
            {
                Evaluate(statement);
            }
        }

        if (unhandledException != null)
            ExceptionDispatchInfo.Capture(unhandledException).Throw();

        if (pendingSignal != null)
            return pendingSignal;

        return result;
    }

    private (bool Handled, object? Result, ControlFlowSignal? Signal) TryMatchCatchClause(
        IReadOnlyList<BoundCatchClause> catchClauses,
        Exception ex)
    {
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.ExceptionTypeName != null)
            {
                var catchType = _context.TypeResolver.ResolveType(catchClause.ExceptionTypeName);
                if (!catchType.IsInstanceOfType(ex))
                    continue;
            }

            var previousContext = _context;
            _context = _context.CreateChild();
            try
            {
                if (catchClause.VariableName != null)
                    _context.DefineNew(catchClause.VariableName, ex, ex.GetType());

                if (catchClause.WhenGuard != null)
                {
                    bool guardMatched;
                    try
                    {
                        var guardResult = Evaluate(catchClause.WhenGuard);
                        guardMatched = TypeHelpers.RequireBoolean(guardResult);
                    }
                    catch
                    {
                        guardMatched = false;
                    }

                    if (!guardMatched)
                        continue;
                }

                _caughtExceptions.Push(ex);
                try
                {
                    object? result = null;
                    ControlFlowSignal? signal = null;
                    foreach (var statement in catchClause.Body)
                    {
                        result = Evaluate(statement);
                        if (result is ControlFlowSignal controlFlowSignal)
                        {
                            signal = controlFlowSignal;
                            break;
                        }
                    }

                    return (true, result, signal);
                }
                finally
                {
                    _caughtExceptions.Pop();
                }
            }
            finally
            {
                _context = previousContext;
            }
        }

        return (false, null, null);
    }

    private object? EvaluateThrowStatement()
    {
        if (_caughtExceptions.Count == 0)
            throw new CsEvalException(DiagnosticDescriptors.ThrowOutsideCatch);

        ExceptionDispatchInfo.Capture(_caughtExceptions.Peek()).Throw();
        return null;
    }

    private object? EvaluateReturn(BoundReturnExpr returnExpr)
    {
        var value = returnExpr.Value != null ? Evaluate(returnExpr.Value) : null;
        return ControlFlowSignal.Return(value);
    }

    private object? EvaluateWhile(BoundWhileExpr whileExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var iterationContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            while (TypeHelpers.RequireBoolean(Evaluate(whileExpr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                iterationContext.ClearScope();

                var previousContext = _context;
                _context = iterationContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(whileExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    private object? EvaluateFor(BoundForExpr forExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var loopContext = _context;
        _context = _context.CreateChild();
        var bodyContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var initializer in forExpr.Initializers)
            {
                Evaluate(initializer);
            }

            while (forExpr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(forExpr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                bodyContext.ClearScope();

                var previousContext = _context;
                _context = bodyContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(forExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return) return signal;
                }

                foreach (var increment in forExpr.Increments)
                {
                    Evaluate(increment);
                }
            }
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
            _context = loopContext;
        }

        return null;
    }

    private object? EvaluateDoWhile(BoundDoWhileExpr doWhileExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var iterationContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                iterationContext.ClearScope();

                var previousContext = _context;
                _context = iterationContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(doWhileExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            } while (TypeHelpers.RequireBoolean(Evaluate(doWhileExpr.Condition)));

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    private object? EvaluateForEach(BoundForEachExpr forEachExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var collection = Evaluate(forEachExpr.Collection);

        if (collection is not IEnumerable enumerable)
        {
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));
        }

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var item in enumerable)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var previousContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    _context.DefineNew(forEachExpr.VariableName, item, typeof(object));
                    signal = ExecuteStatementBlock(forEachExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    private object? EvaluateUsingStatement(BoundUsingStatementExpr usingStatement)
    {
        var resource = Evaluate(usingStatement.Resource);
        try
        {
            return Evaluate(usingStatement.Body);
        }
        finally
        {
            if (resource is IDisposable disposable)
                disposable.Dispose();
            else if (resource is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private object? EvaluateLockStatement(BoundLockStatementExpr lockStatement)
    {
        var lockObject = Evaluate(lockStatement.LockObject);
        if (lockObject == null)
            throw new CsEvalException("lock statement requires a non-null reference");

        lock (lockObject)
        {
            return Evaluate(lockStatement.Body);
        }
    }

    private object? EvaluateSwitch(BoundSwitchStatementExpr switchStatement)
    {
        var switchValue = Evaluate(switchStatement.Expression);
        var matched = false;
        var defaultCaseIndex = -1;

        _breakContextDepth++;
        try
        {
            for (var i = 0; i < switchStatement.Cases.Length; i++)
            {
                var switchCase = switchStatement.Cases[i];
                if (switchCase.CasePattern == null)
                {
                    defaultCaseIndex = i;
                    continue;
                }

                if (matched)
                    continue;

                var previousContext = _context;
                _context = _context.CreateChild();
                try
                {
                    if (!TypeHelpers.RequireBoolean(MatchPattern(switchValue, switchCase.CasePattern)))
                        continue;

                    if (switchCase.WhenGuard != null)
                    {
                        var guardResult = Evaluate(switchCase.WhenGuard);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    matched = true;
                    var signal = ExecuteSwitchCaseStatements(switchStatement.Cases, i);
                    if (signal != null)
                        return signal.SignalKind == ControlFlowSignal.Kind.Break ? null : signal;
                }
                finally
                {
                    _context = previousContext;
                }
            }

            if (!matched && defaultCaseIndex >= 0)
            {
                var signal = ExecuteSwitchCaseStatements(switchStatement.Cases, defaultCaseIndex);
                if (signal != null && signal.SignalKind != ControlFlowSignal.Kind.Break)
                    return signal;
            }

            return null;
        }
        finally
        {
            _breakContextDepth--;
        }
    }

    private object? EvaluateSwitchExpression(BoundSwitchExpressionExpr switchExpression)
    {
        var value = Evaluate(switchExpression.Expression);

        foreach (var arm in switchExpression.Arms)
        {
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                if (!TypeHelpers.RequireBoolean(MatchPattern(value, arm.Pattern)))
                    continue;

                if (arm.WhenGuard != null)
                {
                    var guardResult = Evaluate(arm.WhenGuard);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                return Evaluate(arm.Value);
            }
            finally
            {
                _context = previousContext;
            }
        }

        throw new System.Runtime.CompilerServices.SwitchExpressionException(value);
    }

    private object? EvaluateBreak()
    {
        if (_breakContextDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Break;
    }

    private object? EvaluateContinue()
    {
        if (_loopDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Continue;
    }

    private ControlFlowSignal? ExecuteStatementBlock(IEnumerable<BoundExpr> statements)
    {
        foreach (var statement in statements)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var result = Evaluate(statement);
            if (result is ControlFlowSignal signal)
                return signal;
        }

        return null;
    }

    private ControlFlowSignal? ExecuteSwitchCaseStatements(IReadOnlyList<BoundSwitchCase> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            foreach (var statement in switchCase.Statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = Evaluate(statement);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new CsEvalException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    private static string GetLogicalExpressionTypeName(BoundExpr expr)
    {
        if (expr is BoundLiteralExpr { Value: null })
            return TypeNameFormatter.Null;

        if (expr is BoundLiteralExpr { Value: { } value })
            return value.GetType().Name;

        return expr.StaticType == typeof(object)
            ? "unknown"
            : expr.StaticType.Name;
    }

    private object? EvaluateMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        var target = Evaluate(memberAccess.Target);
        if (memberAccess.NullSafe && target == null)
            return null;
        return MemberAccess.GetMember(
            target,
            memberAccess.MemberName,
            _options,
            nullSafe: memberAccess.NullSafe,
            _context);
    }

    private object? EvaluateIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        if (indexAccess.NullSafe && target == null)
            return null;

        if (target == null)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(indexAccess.Index);
        return MemberAccess.GetIndex(target, index, _options);
    }

    private object? EvaluateMultiDimIndexAccess(BoundMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = Evaluate(multiDimIndexAccess.Target);
        if (multiDimIndexAccess.NullSafe && target == null)
            return null;

        var indices = new int[multiDimIndexAccess.Indices.Length];
        for (var i = 0; i < multiDimIndexAccess.Indices.Length; i++)
            indices[i] = Convert.ToInt32(Evaluate(multiDimIndexAccess.Indices[i]));

        if (target is Array array)
            return array.GetValue(indices);

        if (target != null && multiDimIndexAccess.Indices.Length > 1)
        {
            var hasMatchingIndexer = target.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(property => property.Name == "Item" && property.GetIndexParameters().Length == multiDimIndexAccess.Indices.Length);
            if (hasMatchingIndexer)
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, target.GetType().Name);
        }

        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateMultiDimIndexAssign(BoundMultiDimIndexAssignExpr multiDimIndexAssign)
    {
        var target = Evaluate(multiDimIndexAssign.Target);
        var indices = new int[multiDimIndexAssign.Indices.Length];
        for (var i = 0; i < multiDimIndexAssign.Indices.Length; i++)
            indices[i] = Convert.ToInt32(Evaluate(multiDimIndexAssign.Indices[i]));
        var value = Evaluate(multiDimIndexAssign.Value);

        if (target is Array array)
        {
            array.SetValue(value, indices);
            return value;
        }

        if (target != null && multiDimIndexAssign.Indices.Length > 1)
        {
            var hasMatchingIndexer = target.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(property => property.Name == "Item" && property.GetIndexParameters().Length == multiDimIndexAssign.Indices.Length);
            if (hasMatchingIndexer)
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, target.GetType().Name);
        }

        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(target));
    }

    private object? EvaluateNamedArgument(BoundNamedArgumentExpr namedArgument)
    {
        return new NamedArg(namedArgument.Name, Evaluate(namedArgument.Value));
    }

    private static object? EvaluateOutArg(BoundOutArgExpr outArg)
    {
        return new OutArgMarker(outArg.VariableName, outArg.TypeName, outArg.IsDiscard);
    }

    private object? EvaluateCall(BoundCallExpr call)
    {
        var args = new object?[call.Arguments.Length];
        var outBindings = CollectOutBindings(call.Arguments);
        for (var i = 0; i < call.Arguments.Length; i++)
        {
            args[i] = Evaluate(call.Arguments[i]);
        }

        if (call.Callee is BoundMemberAccessExpr memberAccess && memberAccess.Plan != null)
        {
            var target = memberAccess.Plan.IsStatic ? null : Evaluate(memberAccess.Target);
            if (memberAccess.NullSafe && target == null)
                return null;

            if (!call.Plan.IsModuleCall)
            {
                ExecutionRuntime.EnsureMethodCallsAllowed(
                    _options,
                    call.Plan.SelectedMethod.Name,
                    call.Plan.IsStaticCall ? call.Plan.SelectedMethod.DeclaringType : null);
            }

            var result = CsEval.Runtime.MethodInvoker.InvokeMethodWithArgs(
                call.Plan.SelectedMethod,
                target,
                args,
                _cancellationToken);
            if (result.Success)
            {
                if (outBindings.Length > 0)
                    IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
                return result.Value;
            }
        }

        var callee = Evaluate(call.Callee);
        var invokeResult = CsEval.Runtime.MethodInvoker.InvokeCall(callee, args, _context, _options, _cancellationToken);
        if (outBindings.Length > 0)
            IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
        return invokeResult;
    }

    private object? EvaluateInvoke(BoundInvokeExpr invoke)
    {
        var args = new object?[invoke.Arguments.Length];
        var outBindings = CollectOutBindings(invoke.Arguments);
        for (var i = 0; i < invoke.Arguments.Length; i++)
        {
            args[i] = Evaluate(invoke.Arguments[i]);
        }

        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name,
                args,
                _context,
                _options,
                _cancellationToken,
                typeArguments);
            if (outBindings.Length > 0)
                IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessExpr memberAccess)
        {
            var target = Evaluate(memberAccess.Target);
            var result = CsEval.Runtime.MethodInvoker.InvokeMemberCall(
                target,
                memberAccess.MemberName,
                args,
                memberAccess.NullSafe,
                _context,
                _options,
                _cancellationToken,
                typeArguments);
            if (outBindings.Length > 0)
                IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
            return result;
        }

        var callee = Evaluate(invoke.Callee);
        var invokeResult = CsEval.Runtime.MethodInvoker.InvokeCall(
            callee,
            args,
            _context,
            _options,
            _cancellationToken,
            typeArguments);
        if (outBindings.Length > 0)
            IdentifierRuntime.DefineOutVariables(args, outBindings, _context);
        return invokeResult;
    }

    private static OutVariableBinding[] CollectOutBindings(IReadOnlyList<BoundExpr> arguments)
    {
        List<OutVariableBinding>? bindings = null;
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return bindings?.ToArray() ?? [];
    }

    private object? EvaluateLambda(BoundLambdaExpr lambda)
    {
        return new LambdaValue(lambda.Parameters.ToList(), lambda.Body, _context, _options);
    }

    private object? EvaluatePipeline(BoundPipelineExpr pipeline)
    {
        var left = Evaluate(pipeline.Left);

        if (pipeline.Right is BoundIdentifierExpr rightIdentifier)
        {
            return IdentifierRuntime.InvokePipelineIdentifier(
                left,
                rightIdentifier.Name,
                _context,
                _options,
                _cancellationToken);
        }

        var right = Evaluate(pipeline.Right);
        return PipelineOperator.InvokePipeline(left, right, _context, _options, _cancellationToken);
    }

    private object? EvaluateRange(BoundRangeExpr range)
    {
        var startValue = Evaluate(range.Start);
        var endValue = Evaluate(range.End);
        var start = Convert.ToInt32(startValue);
        var end = Convert.ToInt32(endValue);
        return RangeHelpers.GenerateRange(start, end, range.ExclusiveEnd);
    }

    private object? EvaluateChecked(BoundCheckedExpr checkedExpr)
    {
        var previous = _isChecked;
        _isChecked = checkedExpr.IsChecked;
        try
        {
            return Evaluate(checkedExpr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    private object? EvaluateChainedComparison(BoundChainedComparisonExpr chainedComparison)
    {
        var previousValue = Evaluate(chainedComparison.Operands[0]);

        for (var i = 0; i < chainedComparison.Operators.Length; i++)
        {
            var nextValue = Evaluate(chainedComparison.Operands[i + 1]);
            if (!ChainedComparisonHelper.PerformComparison(
                    previousValue,
                    nextValue,
                    chainedComparison.Operators[i],
                    _options))
            {
                return false;
            }

            previousValue = nextValue;
        }

        return true;
    }

    private object? MatchPattern(object? value, Pattern pattern)
        => PatternRuntime.MatchPattern(value, pattern, _context, _options, _cancellationToken);
}
