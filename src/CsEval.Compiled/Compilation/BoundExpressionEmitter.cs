using System.Collections.Immutable;
using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;
using static CsEval.Compiled.Compilation.BoundRuntimeMethodCache;

namespace CsEval.Compiled.Compilation;

/// <summary>
/// Emits expression trees from core bound nodes.
/// This provides a shared semantic entrypoint for compiled mode while unsupported nodes
/// can still fall back to the existing AST compiler pipeline.
/// </summary>
internal sealed class BoundExpressionEmitter
{
    private readonly ParameterExpression _contextParam;
    private readonly ParameterExpression _optionsParam;
    private readonly ParameterExpression _ctParam;
    private bool _isChecked;
    private int _loopDepth;
    private int _switchDepth;
    private int _catchDepth;
    private Dictionary<string, HoistedIdentifier>? _hoistedIdentifiers;
    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression optionsParam,
        ParameterExpression ctParam)
    {
        _contextParam = contextParam;
        _optionsParam = optionsParam;
        _ctParam = ctParam;
    }

    public LinqExpression EmitRoot(BoundExpr expr)
    {
        var hoists = BuildIdentifierHoistPlan(expr);
        if (hoists.Count == 0)
            return Emit(expr);

        _hoistedIdentifiers = hoists;
        try
        {
            var variables = hoists.Values.Select(h => h.Variable).ToArray();
            var body = Emit(expr);
            var statements = new List<LinqExpression>(hoists.Count + 1);
            foreach (var (name, hoisted) in hoists)
            {
                statements.Add(
                    LinqExpression.Assign(
                        hoisted.Variable,
                        LinqExpression.Call(
                            _contextParam,
                            GetVariableTypedMethodFor(hoisted.Type),
                            LinqExpression.Constant(name))));
            }

            statements.Add(body);
            return LinqExpression.Block(body.Type, variables, statements);
        }
        finally
        {
            _hoistedIdentifiers = null;
        }
    }

    public LinqExpression Emit(BoundExpr expr)
    {
        return expr switch
        {
            BoundLiteralExpr literal => EmitLiteral(literal),
            BoundIdentifierExpr identifier => EmitIdentifier(identifier),
            BoundCastExpr cast => EmitCast(cast),
            BoundAsExpr asExpr => EmitAs(asExpr),
            BoundIsPatternExpr isPattern => EmitIsPattern(isPattern),
            BoundUnaryExpr unary => EmitUnary(unary),
            BoundBinaryExpr binary => EmitBinary(binary),
            BoundLogicalExpr logical => EmitLogical(logical),
            BoundNullCoalesceExpr nullCoalesce => EmitNullCoalesce(nullCoalesce),
            BoundConditionalExpr conditional => EmitConditional(conditional),
            BoundBlockExpr block => EmitBlock(block),
            BoundIfStatementExpr ifStatement => EmitIfStatement(ifStatement),
            BoundWhileExpr whileExpr => EmitWhile(whileExpr),
            BoundForExpr forExpr => EmitFor(forExpr),
            BoundDoWhileExpr doWhileExpr => EmitDoWhile(doWhileExpr),
            BoundForEachExpr forEachExpr => EmitForEach(forEachExpr),
            BoundUsingStatementExpr usingStatement => EmitUsingStatement(usingStatement),
            BoundLockStatementExpr lockStatement => EmitLockStatement(lockStatement),
            BoundTryCatchFinallyExpr tryCatchFinally => EmitTryCatchFinally(tryCatchFinally),
            BoundBreakExpr breakExpr => EmitBreak(breakExpr),
            BoundContinueExpr continueExpr => EmitContinue(continueExpr),
            BoundThrowStatementExpr throwStatementExpr => EmitThrowStatement(throwStatementExpr),
            BoundReturnExpr returnExpr => EmitReturn(returnExpr),
            BoundSwitchStatementExpr switchStatement => EmitSwitchStatement(switchStatement),
            BoundSwitchExpressionExpr switchExpression => EmitSwitchExpression(switchExpression),
            BoundCheckedExpr checkedExpr => EmitChecked(checkedExpr),
            BoundChainedComparisonExpr chainedComparison => EmitChainedComparison(chainedComparison),
            BoundRangeExpr range => EmitRange(range),
            BoundVariableDeclExpr variableDecl => EmitVariableDecl(variableDecl),
            BoundAssignExpr assign => EmitAssign(assign),
            BoundNullCoalesceAssignExpr nullCoalesceAssign => EmitNullCoalesceAssign(nullCoalesceAssign),
            BoundCompoundAssignExpr compoundAssign => EmitCompoundAssign(compoundAssign),
            BoundIncrementDecrementExpr incrementDecrement => EmitIncrementDecrement(incrementDecrement),
            BoundMemberAssignExpr memberAssign => EmitMemberAssign(memberAssign),
            BoundIndexAssignExpr indexAssign => EmitIndexAssign(indexAssign),
            BoundMemberCompoundAssignExpr memberCompoundAssign => EmitMemberCompoundAssign(memberCompoundAssign),
            BoundIndexCompoundAssignExpr indexCompoundAssign => EmitIndexCompoundAssign(indexCompoundAssign),
            BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign => EmitMemberNullCoalesceAssign(memberNullCoalesceAssign),
            BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign => EmitIndexNullCoalesceAssign(indexNullCoalesceAssign),
            BoundMemberIncrementExpr memberIncrement => EmitMemberIncrement(memberIncrement),
            BoundIndexIncrementExpr indexIncrement => EmitIndexIncrement(indexIncrement),
            BoundMemberAccessExpr memberAccess => EmitMemberAccess(memberAccess),
            BoundIndexAccessExpr indexAccess => EmitIndexAccess(indexAccess),
            BoundObjectCreationExpr objectCreation => EmitObjectCreation(objectCreation),
            BoundTypedArrayCreationExpr typedArrayCreation => EmitTypedArrayCreation(typedArrayCreation),
            BoundTypedArrayLiteralExpr typedArrayLiteral => EmitTypedArrayLiteral(typedArrayLiteral),
            BoundTupleExpr tuple => EmitTuple(tuple),
            BoundDeconstructionExpr deconstruction => EmitDeconstruction(deconstruction),
            BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation => EmitMultiDimTypedArrayCreation(multiDimTypedArrayCreation),
            BoundMultiDimIndexAccessExpr multiDimIndexAccess => EmitMultiDimIndexAccess(multiDimIndexAccess),
            BoundMultiDimIndexAssignExpr multiDimIndexAssign => EmitMultiDimIndexAssign(multiDimIndexAssign),
            BoundThrowExpr throwExpr => EmitThrow(throwExpr),
            BoundSliceExpr slice => EmitSlice(slice),
            BoundCallExpr call => EmitCall(call),
            BoundInvokeExpr invoke => EmitInvoke(invoke),
            BoundLambdaExpr lambda => EmitLambda(lambda),
            BoundPipelineExpr pipeline => EmitPipeline(pipeline),
            BoundArrayLiteralExpr arrayLiteral => EmitArrayLiteral(arrayLiteral),
            BoundObjectLiteralExpr objectLiteral => EmitObjectLiteral(objectLiteral),
            BoundInterpolatedStringExpr interpolatedString => EmitInterpolatedString(interpolatedString),
            BoundNamedArgumentExpr namedArgument => EmitNamedArgument(namedArgument),
            BoundOutArgExpr outArg => EmitOutArg(outArg),
            BoundSpreadExpr => EmitInvalidSpread(),
            _ => throw new BindingNotSupportedException(
                $"Bound compiled emission not implemented for '{expr.GetType().Name}'")
        };
    }

    private static Dictionary<string, HoistedIdentifier> BuildIdentifierHoistPlan(BoundExpr root)
    {
        var usage = new Dictionary<string, (Type Type, int Count)>(StringComparer.Ordinal);
        if (!CanHoistIdentifiers(root, usage))
            return new Dictionary<string, HoistedIdentifier>(StringComparer.Ordinal);

        var hoists = new Dictionary<string, HoistedIdentifier>(StringComparer.Ordinal);
        foreach (var (name, entry) in usage)
        {
            if (entry.Count <= 1)
                continue;

            hoists[name] = new HoistedIdentifier(
                entry.Type,
                LinqExpression.Variable(entry.Type, $"cached_{name.Replace('.', '_')}"));
        }

        return hoists;
    }

    private static bool CanHoistIdentifiers(BoundExpr expr, Dictionary<string, (Type Type, int Count)> usage)
    {
        switch (expr)
        {
            case BoundLiteralExpr:
                return true;

            case BoundIdentifierExpr identifier:
                if (identifier.StaticType == typeof(object))
                    return false;

                if (usage.TryGetValue(identifier.Name, out var entry))
                    usage[identifier.Name] = (entry.Type, entry.Count + 1);
                else
                    usage[identifier.Name] = (identifier.StaticType, 1);
                return true;

            case BoundBinaryExpr binary:
                return CanHoistIdentifiers(binary.Left, usage) &&
                       CanHoistIdentifiers(binary.Right, usage);

            case BoundLogicalExpr logical:
                return CanHoistIdentifiers(logical.Left, usage) &&
                       CanHoistIdentifiers(logical.Right, usage);

            case BoundUnaryExpr unary:
                return CanHoistIdentifiers(unary.Operand, usage);

            case BoundCastExpr cast:
                return CanHoistIdentifiers(cast.Expression, usage);

            case BoundAsExpr asExpr:
                return CanHoistIdentifiers(asExpr.Expression, usage);

            case BoundIsPatternExpr isPattern:
                return CanHoistIdentifiers(isPattern.Expression, usage);

            case BoundCheckedExpr checkedExpr:
                return CanHoistIdentifiers(checkedExpr.Expression, usage);

            case BoundNullCoalesceExpr nullCoalesce:
                return CanHoistIdentifiers(nullCoalesce.Left, usage) &&
                       CanHoistIdentifiers(nullCoalesce.Right, usage);

            case BoundConditionalExpr conditional:
                return CanHoistIdentifiers(conditional.Condition, usage) &&
                       CanHoistIdentifiers(conditional.ThenBranch, usage) &&
                       CanHoistIdentifiers(conditional.ElseBranch, usage);

            default:
                return false;
        }
    }

    private sealed record HoistedIdentifier(Type Type, ParameterExpression Variable);

    private static LinqExpression EmitLiteral(BoundLiteralExpr literal)
    {
        if (literal.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(literal.Value, literal.Value.GetType());
    }

    private LinqExpression EmitIdentifier(BoundIdentifierExpr identifier)
    {
        if (_hoistedIdentifiers != null &&
            _hoistedIdentifiers.TryGetValue(identifier.Name, out var hoisted))
        {
            return hoisted.Variable;
        }

        if (identifier.StaticType != typeof(object))
        {
            return LinqExpression.Call(
                _contextParam,
                GetVariableTypedMethodFor(identifier.StaticType),
                LinqExpression.Constant(identifier.Name));
        }

        return LinqExpression.Call(
            ResolveIdentifierMethod,
            LinqExpression.Constant(identifier.Name),
            _contextParam,
            _optionsParam);
    }

    private LinqExpression EmitCast(BoundCastExpr cast)
    {
        return LinqExpression.Call(
            ExplicitCastMethod,
            BoundEmitterSupport.AsObject(Emit(cast.Expression)),
            LinqExpression.Constant(cast.TargetType, typeof(Type)),
            cast.SourceStaticType == null
                ? LinqExpression.Constant(null, typeof(Type))
                : LinqExpression.Constant(cast.SourceStaticType, typeof(Type)),
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitAs(BoundAsExpr asExpr)
    {
        return LinqExpression.Call(
            TryAsMethod,
            BoundEmitterSupport.AsObject(Emit(asExpr.Expression)),
            LinqExpression.Constant(asExpr.TargetType, typeof(Type)));
    }

    private LinqExpression EmitIsPattern(BoundIsPatternExpr isPattern)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(
                MatchPatternMethod,
                BoundEmitterSupport.AsObject(Emit(isPattern.Expression)),
                LinqExpression.Constant(isPattern.Pattern, typeof(Pattern)),
                _contextParam,
                _optionsParam,
                _ctParam),
            typeof(object));
    }

    private LinqExpression EmitUnary(BoundUnaryExpr unary)
    {
        var operand = BoundEmitterSupport.AsObject(Emit(unary.Operand));
        return unary.Operator switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand, LinqExpression.Constant(_isChecked)),
            TokenType.Plus => LinqExpression.Call(UnaryPlusMethod, operand),
            TokenType.Bang => LinqExpression.Call(LogicalNotMethod, operand),
            TokenType.Tilde => LinqExpression.Call(BitwiseNotMethod, operand),
            _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{unary.Operator}'")
        };
    }

    private LinqExpression EmitBinary(BoundBinaryExpr binary)
    {
        if (TryEmitPrimitiveBinaryFastPath(binary, out var direct))
            return direct;

        if (ShouldApplyConstantPromotion(binary))
            return EmitBinaryWithConstantPromotion(binary);

        return EmitBinaryCore(binary.Operator, BoundEmitterSupport.AsObject(Emit(binary.Left)), BoundEmitterSupport.AsObject(Emit(binary.Right)));
    }

    private LinqExpression EmitBinaryWithConstantPromotion(BoundBinaryExpr binary)
    {
        var leftVar = LinqExpression.Variable(typeof(object), "binaryLeft");
        var rightVar = LinqExpression.Variable(typeof(object), "binaryRight");
        var promotedVar = LinqExpression.Variable(typeof(ValueTuple<object?, object?>), "binaryPromoted");

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar, promotedVar],
            LinqExpression.Assign(leftVar, BoundEmitterSupport.AsObject(Emit(binary.Left))),
            LinqExpression.Assign(rightVar, BoundEmitterSupport.AsObject(Emit(binary.Right))),
            LinqExpression.Assign(
                promotedVar,
                LinqExpression.Call(
                    ApplyConstantNumericPromotionMethod,
                    leftVar,
                    LinqExpression.Constant(binary.Left is BoundLiteralExpr),
                    rightVar,
                    LinqExpression.Constant(binary.Right is BoundLiteralExpr))),
            LinqExpression.Assign(leftVar, LinqExpression.Field(promotedVar, "Item1")),
            LinqExpression.Assign(rightVar, LinqExpression.Field(promotedVar, "Item2")),
            EmitBinaryCore(binary.Operator, leftVar, rightVar));
    }

    private static bool ShouldApplyConstantPromotion(BoundBinaryExpr binary)
    {
        if (binary.Operator is not (TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or TokenType.Percent))
            return false;

        return binary.Left is BoundLiteralExpr || binary.Right is BoundLiteralExpr;
    }

    private LinqExpression EmitBinaryCore(TokenType op, LinqExpression left, LinqExpression right)
    {
        left = BoundEmitterSupport.AsObject(left);
        right = BoundEmitterSupport.AsObject(right);

        return op switch
        {
            TokenType.Plus => LinqExpression.Call(AddMethod, left, right, _optionsParam, _contextParam, LinqExpression.Constant(_isChecked)),
            TokenType.Minus => LinqExpression.Call(SubtractMethod, left, right, LinqExpression.Constant(_isChecked)),
            TokenType.Star => LinqExpression.Call(MultiplyMethod, left, right, _optionsParam, LinqExpression.Constant(_isChecked)),
            TokenType.Slash => LinqExpression.Call(DivideMethod, left, right),
            TokenType.Percent => LinqExpression.Call(ModuloMethod, left, right),
            TokenType.EqualEqual => LinqExpression.Call(EqualsMethod, left, right),
            TokenType.BangEqual => LinqExpression.Call(NotEqualsMethod, left, right),
            TokenType.EqualEqualEqual => LinqExpression.Call(EqualsMethod, left, right),
            TokenType.BangEqualEqual => LinqExpression.Call(NotEqualsMethod, left, right),
            TokenType.Less => LinqExpression.Call(LessThanMethod, left, right, _optionsParam),
            TokenType.LessEqual => LinqExpression.Call(LessThanOrEqualMethod, left, right, _optionsParam),
            TokenType.Greater => LinqExpression.Call(GreaterThanMethod, left, right, _optionsParam),
            TokenType.GreaterEqual => LinqExpression.Call(GreaterThanOrEqualMethod, left, right, _optionsParam),
            TokenType.Amp => LinqExpression.Call(BitwiseAndMethod, left, right),
            TokenType.Pipe => LinqExpression.Call(BitwiseOrMethod, left, right),
            TokenType.Caret => LinqExpression.Call(BitwiseXorMethod, left, right),
            TokenType.LessLess => LinqExpression.Call(LeftShiftMethod, left, right),
            TokenType.GreaterGreater => LinqExpression.Call(RightShiftMethod, left, right),
            TokenType.GreaterGreaterGreater => LinqExpression.Call(UnsignedRightShiftMethod, left, right),
            TokenType.StarStar => LinqExpression.Call(PowerMethod, left, right),
            TokenType.In => LinqExpression.Call(InOperatorMethod, left, right),
            TokenType.Like => LinqExpression.Call(LikeMethod, left, right),
            TokenType.EqualTilde => LinqExpression.Call(RegexMatchMethod, left, right),
            TokenType.BangTilde => LinqExpression.Call(RegexNotMatchMethod, left, right),
            TokenType.LessEqualGreater => LinqExpression.Call(SpaceshipMethod, left, right),
            _ => throw new BindingNotSupportedException($"Unsupported bound binary operator '{op}'")
        };
    }

    private bool TryEmitPrimitiveBinaryFastPath(BoundBinaryExpr binary, out LinqExpression direct)
    {
        direct = null!;
        if (!TryGetNumericFastPathType(binary, out var promotedType))
            return false;

        var left = BoundEmitterSupport.EnsureTypedExpression(Emit(binary.Left), binary.Left.StaticType);
        var right = BoundEmitterSupport.EnsureTypedExpression(Emit(binary.Right), binary.Right.StaticType);
        if (left.Type != promotedType)
            left = LinqExpression.Convert(left, promotedType);
        if (right.Type != promotedType)
            right = LinqExpression.Convert(right, promotedType);

        LinqExpression? typed = binary.Operator switch
        {
            TokenType.Plus => _isChecked ? LinqExpression.AddChecked(left, right) : LinqExpression.Add(left, right),
            TokenType.Minus => _isChecked ? LinqExpression.SubtractChecked(left, right) : LinqExpression.Subtract(left, right),
            TokenType.Star => _isChecked ? LinqExpression.MultiplyChecked(left, right) : LinqExpression.Multiply(left, right),
            TokenType.Slash => LinqExpression.Divide(left, right),
            TokenType.Percent => LinqExpression.Modulo(left, right),
            TokenType.EqualEqual or TokenType.EqualEqualEqual => LinqExpression.Equal(left, right),
            TokenType.BangEqual or TokenType.BangEqualEqual => LinqExpression.NotEqual(left, right),
            TokenType.Less => LinqExpression.LessThan(left, right),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(left, right),
            TokenType.Greater => LinqExpression.GreaterThan(left, right),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(left, right),
            _ => null
        };

        if (typed == null)
            return false;

        direct = typed;
        return true;
    }

    private static bool TryGetNumericFastPathType(BoundBinaryExpr binary, out Type promotedType)
    {
        promotedType = null!;
        if (!IsFastPathNumericOperator(binary.Operator))
            return false;

        var leftType = binary.Left.StaticType;
        var rightType = binary.Right.StaticType;
        if (!IsPrimitiveBinaryFastPathType(leftType) || !IsPrimitiveBinaryFastPathType(rightType))
            return false;

        if ((leftType == typeof(decimal) && (rightType == typeof(float) || rightType == typeof(double))) ||
            (rightType == typeof(decimal) && (leftType == typeof(float) || leftType == typeof(double))))
        {
            return false;
        }

        if (TryGetConstantPromotionType(binary, leftType, rightType, out promotedType))
            return true;

        promotedType = NumericDispatch.GetResultType(leftType, rightType);
        return IsPrimitiveBinaryFastPathType(promotedType);
    }

    private static bool IsFastPathNumericOperator(TokenType op)
    {
        return op is TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.EqualEqualEqual or TokenType.BangEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual;
    }

    private static bool TryGetConstantPromotionType(BoundBinaryExpr binary, Type leftType, Type rightType, out Type promotedType)
    {
        promotedType = null!;
        var leftLiteral = binary.Left as BoundLiteralExpr;
        var rightLiteral = binary.Right as BoundLiteralExpr;

        if (leftType == typeof(uint) && rightLiteral?.Value is int rightInt && rightInt >= 0)
        {
            promotedType = typeof(uint);
            return true;
        }

        if (rightType == typeof(uint) && leftLiteral?.Value is int leftInt && leftInt >= 0)
        {
            promotedType = typeof(uint);
            return true;
        }

        if (leftType == typeof(ulong) && rightLiteral?.Value is int rightIntForUlong && rightIntForUlong >= 0)
        {
            promotedType = typeof(ulong);
            return true;
        }

        if (rightType == typeof(ulong) && leftLiteral?.Value is int leftIntForUlong && leftIntForUlong >= 0)
        {
            promotedType = typeof(ulong);
            return true;
        }

        if (leftType == typeof(ulong) && rightLiteral?.Value is long rightLongForUlong && rightLongForUlong >= 0)
        {
            promotedType = typeof(ulong);
            return true;
        }

        if (rightType == typeof(ulong) && leftLiteral?.Value is long leftLongForUlong && leftLongForUlong >= 0)
        {
            promotedType = typeof(ulong);
            return true;
        }

        return false;
    }

    private static bool IsPrimitiveBinaryFastPathType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(byte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(char) ||
               type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private LinqExpression EmitLogical(BoundLogicalExpr logical)
    {
        var leftCandidate = Emit(logical.Left);
        var rightCandidate = Emit(logical.Right);
        if (leftCandidate.Type == typeof(bool) && rightCandidate.Type == typeof(bool))
        {
            return logical.Operator switch
            {
                TokenType.PipePipe => LinqExpression.OrElse(leftCandidate, rightCandidate),
                TokenType.AmpAmp => LinqExpression.AndAlso(leftCandidate, rightCandidate),
                _ => throw new BindingNotSupportedException($"Unsupported bound logical operator '{logical.Operator}'")
            };
        }

        var opLexeme = TokenLexemes.GetCanonical(logical.Operator);
        var leftBool = LinqExpression.Call(
            RequireBooleanForLogicalOperatorMethod,
            BoundEmitterSupport.AsObject(leftCandidate),
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(BoundEmitterSupport.GetBoundTypeName(logical.Right)));
        var rightBoolAsObject = LinqExpression.Convert(
            LinqExpression.Call(
                RequireBooleanForLogicalOperatorMethod,
                BoundEmitterSupport.AsObject(rightCandidate),
                LinqExpression.Constant(opLexeme),
                LinqExpression.Constant(BoundEmitterSupport.GetBoundTypeName(logical.Left))),
            typeof(object));

        return logical.Operator switch
        {
            TokenType.PipePipe => LinqExpression.Condition(
                leftBool,
                LinqExpression.Constant(true, typeof(object)),
                rightBoolAsObject),
            TokenType.AmpAmp => LinqExpression.Condition(
                leftBool,
                rightBoolAsObject,
                LinqExpression.Constant(false, typeof(object))),
            _ => throw new BindingNotSupportedException($"Unsupported bound logical operator '{logical.Operator}'")
        };
    }

    private LinqExpression EmitNullCoalesce(BoundNullCoalesceExpr nullCoalesce)
    {
        var leftVar = LinqExpression.Variable(typeof(object), "coalesceLeft");
        return LinqExpression.Block(
            typeof(object),
            [leftVar],
            LinqExpression.Assign(leftVar, BoundEmitterSupport.AsObject(Emit(nullCoalesce.Left))),
            LinqExpression.Condition(
                LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(object))),
                BoundEmitterSupport.AsObject(Emit(nullCoalesce.Right)),
                leftVar));
    }

    private LinqExpression EmitConditional(BoundConditionalExpr conditional)
    {
        var conditionCandidate = Emit(conditional.Condition);
        var condition = conditionCandidate.Type == typeof(bool)
            ? conditionCandidate
            : LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(conditionCandidate));

        var thenCandidate = Emit(conditional.ThenBranch);
        var elseCandidate = Emit(conditional.ElseBranch);

        if (TryEmitTypedArithmeticConditional(conditional, condition, thenCandidate, elseCandidate, out var typed))
            return typed;

        return LinqExpression.Condition(
            condition,
            BoundEmitterSupport.AsObject(thenCandidate),
            BoundEmitterSupport.AsObject(elseCandidate));
    }

    private static bool TryEmitTypedArithmeticConditional(
        BoundConditionalExpr conditional,
        LinqExpression condition,
        LinqExpression thenCandidate,
        LinqExpression elseCandidate,
        out LinqExpression typed)
    {
        typed = null!;
        var thenType = conditional.ThenBranch.StaticType;
        var elseType = conditional.ElseBranch.StaticType;
        var resultType = conditional.StaticType;

        if (!TypeHelpers.IsArithmetic(thenType) ||
            !TypeHelpers.IsArithmetic(elseType) ||
            !TypeHelpers.IsArithmetic(resultType) ||
            !IsPrimitiveBinaryFastPathType(resultType))
        {
            return false;
        }

        if ((resultType == typeof(decimal) &&
             (thenType == typeof(float) || thenType == typeof(double) ||
              elseType == typeof(float) || elseType == typeof(double))))
        {
            return false;
        }

        try
        {
            var thenTyped = BoundEmitterSupport.EnsureTypedExpression(thenCandidate, thenType);
            var elseTyped = BoundEmitterSupport.EnsureTypedExpression(elseCandidate, elseType);

            if (thenTyped.Type != resultType)
                thenTyped = LinqExpression.Convert(thenTyped, resultType);
            if (elseTyped.Type != resultType)
                elseTyped = LinqExpression.Convert(elseTyped, resultType);

            typed = LinqExpression.Condition(condition, thenTyped, elseTyped);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private LinqExpression EmitBlock(BoundBlockExpr block)
    {
        var statements = block.Statements;
        var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "blockSignal");
        var doneLabel = LinqExpression.Label("blockDone");

        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(body, statements, resultVar, signalVar, doneLabel, unwrapReturnSignal: true);
        if (block.ReturnExpr != null)
            body.Add(LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(Emit(block.ReturnExpr))));
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private LinqExpression EmitIfStatement(BoundIfStatementExpr ifStatement)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "ifResult");
        var condition = LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(ifStatement.Condition)));
        var thenBody = EmitScopedStatements(ifStatement.ThenStatements);
        var elseBody = ifStatement.ElseStatements.IsDefaultOrEmpty
            ? LinqExpression.Constant(null, typeof(object))
            : EmitScopedStatements(ifStatement.ElseStatements);

        return LinqExpression.Block(
            typeof(object),
            [resultVar],
            LinqExpression.Assign(
                resultVar,
                LinqExpression.Condition(condition, BoundEmitterSupport.AsObject(thenBody), BoundEmitterSupport.AsObject(elseBody))),
            resultVar);
    }

    private LinqExpression EmitWhile(BoundWhileExpr whileExpr)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "whileBreak");
        var loopContinueLabel = LinqExpression.Label("whileContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "whileResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "whileSignal");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(whileExpr.Condition)))),
                LinqExpression.Break(loopBreakLabel, resultVar))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            EmitLoopIterationBody(body, whileExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: true);
            body.Add(LinqExpression.Label(loopContinueLabel));

            return LinqExpression.Block(
                typeof(object),
                [resultVar, signalVar],
                LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitFor(BoundForExpr forExpr)
    {
        var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), "forPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "forResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "forSignal");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "forBreak");
        var loopContinueLabel = LinqExpression.Label("forContinue");

        var prologue = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            for (var i = 0; i < forExpr.Initializers.Length; i++)
                prologue.Add(BoundEmitterSupport.AsObject(Emit(forExpr.Initializers[i])));

            var body = new List<LinqExpression>();
            if (forExpr.Condition != null)
            {
                body.Add(LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(forExpr.Condition)))),
                    LinqExpression.Break(loopBreakLabel, resultVar)));
            }

            EmitLoopIterationBody(body, forExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            for (var i = 0; i < forExpr.Increments.Length; i++)
                body.Add(BoundEmitterSupport.AsObject(Emit(forExpr.Increments[i])));

            return LinqExpression.Block(
                typeof(object),
                [previousContextVar, resultVar, signalVar],
                LinqExpression.TryFinally(
                    LinqExpression.Block(
                        prologue.Append(
                            LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel))),
                    LinqExpression.Assign(_contextParam, previousContextVar)),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitDoWhile(BoundDoWhileExpr doWhileExpr)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "doBreak");
        var loopContinueLabel = LinqExpression.Label("doContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "doResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "doSignal");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            EmitLoopIterationBody(body, doWhileExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(doWhileExpr.Condition)))),
                LinqExpression.Break(loopBreakLabel, resultVar)));

            return LinqExpression.Block(
                typeof(object),
                [resultVar, signalVar],
                LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitForEach(BoundForEachExpr forEachExpr)
    {
        var enumerableVar = LinqExpression.Variable(typeof(object), "foreachCollection");
        var enumeratorVar = LinqExpression.Variable(typeof(IEnumerator), "foreachEnumerator");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "foreachSignal");
        var currentVar = LinqExpression.Variable(typeof(object), "foreachCurrent");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "foreachBreak");
        var loopContinueLabel = LinqExpression.Label("foreachContinue");

        List<LinqExpression> loopBody;
        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            var iterationBody = EmitForeachIteration(forEachExpr.VariableName, currentVar, forEachExpr.Body);
            loopBody = new List<LinqExpression>
            {
                LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    LinqExpression.Call(_contextParam, GetConstraintStateProperty),
                    LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.Constraints)),
                    _ctParam),
                LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(enumeratorVar, MoveNextMethod)),
                    LinqExpression.Break(loopBreakLabel, resultVar)),
                LinqExpression.Assign(currentVar, LinqExpression.Convert(LinqExpression.Call(enumeratorVar, GetCurrentMethod), typeof(object))),
                LinqExpression.Assign(resultVar, iterationBody),
                BuildLoopSignalDispatch(resultVar, signalVar, loopBreakLabel, loopContinueLabel),
                LinqExpression.Label(loopContinueLabel)
            };
        }
        finally
        {
            _loopDepth = previousDepth;
        }

        var disposableVar = LinqExpression.Variable(typeof(IDisposable), "foreachDisposable");
        return LinqExpression.Block(
            typeof(object),
            [enumerableVar, enumeratorVar, resultVar, signalVar, currentVar, disposableVar],
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(enumerableVar, BoundEmitterSupport.AsObject(Emit(forEachExpr.Collection))),
            LinqExpression.Assign(enumeratorVar, LinqExpression.Call(GetEnumeratorMethod, enumerableVar)),
            LinqExpression.Loop(LinqExpression.Block(loopBody), loopBreakLabel),
            LinqExpression.Assign(disposableVar, LinqExpression.TypeAs(enumeratorVar, typeof(IDisposable))),
            LinqExpression.IfThen(
                LinqExpression.NotEqual(disposableVar, LinqExpression.Constant(null, typeof(IDisposable))),
                LinqExpression.Call(disposableVar, DisposeMethod)),
            resultVar);
    }

    private LinqExpression EmitUsingStatement(BoundUsingStatementExpr usingStatement)
    {
        var resourceVar = LinqExpression.Variable(typeof(object), "usingResource");
        var resultVar = LinqExpression.Variable(typeof(object), "usingResult");

        return LinqExpression.Block(
            typeof(object),
            [resourceVar, resultVar],
            LinqExpression.Assign(resourceVar, BoundEmitterSupport.AsObject(Emit(usingStatement.Resource))),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(Emit(usingStatement.Body))),
                LinqExpression.Call(DisposeResourceMethod, resourceVar)),
            resultVar);
    }

    private LinqExpression EmitLockStatement(BoundLockStatementExpr lockStatement)
    {
        var lockObjVar = LinqExpression.Variable(typeof(object), "lockObj");
        var resultVar = LinqExpression.Variable(typeof(object), "lockResult");

        return LinqExpression.Block(
            typeof(object),
            [lockObjVar, resultVar],
            LinqExpression.Assign(
                lockObjVar,
                LinqExpression.Call(ValidateLockObjectMethod, BoundEmitterSupport.AsObject(Emit(lockStatement.LockObject)))),
            LinqExpression.Call(MonitorEnterMethod, lockObjVar),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(Emit(lockStatement.Body))),
                LinqExpression.Call(MonitorExitMethod, lockObjVar)),
            resultVar);
    }

    private LinqExpression EmitBreak(BoundBreakExpr _)
    {
        if (_loopDepth > 0 || _switchDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowBreakField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }

    private LinqExpression EmitContinue(BoundContinueExpr _)
    {
        if (_loopDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowContinueField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }

    private LinqExpression EmitReturn(BoundReturnExpr returnExpr)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(
                ControlFlowReturnMethod,
                returnExpr.Value == null
                    ? LinqExpression.Constant(null, typeof(object))
                    : BoundEmitterSupport.AsObject(Emit(returnExpr.Value))),
            typeof(object));
    }

    private LinqExpression EmitThrowStatement(BoundThrowStatementExpr _)
    {
        if (_catchDepth == 0)
        {
            return LinqExpression.Throw(
                LinqExpression.Constant(new CsEvalException(DiagnosticDescriptors.ThrowOutsideCatch)),
                typeof(object));
        }

        return LinqExpression.Rethrow(typeof(object));
    }

    private LinqExpression EmitTryCatchFinally(BoundTryCatchFinallyExpr tryCatchFinally)
    {
        var tryBody = EmitStatementSequence(tryCatchFinally.TryBody);
        var catchBlocks = new List<CatchBlock>(tryCatchFinally.CatchClauses.Length);

        for (var i = 0; i < tryCatchFinally.CatchClauses.Length; i++)
        {
            var catchClause = tryCatchFinally.CatchClauses[i];
            var exParam = LinqExpression.Parameter(typeof(Exception), $"catchEx{i}");
            var catchBody = EmitCatchClauseBody(catchClause, exParam);
            var filter = BuildCatchFilter(catchClause, exParam);
            catchBlocks.Add(LinqExpression.MakeCatchBlock(typeof(Exception), exParam, catchBody, filter));
        }

        LinqExpression? finallyBody = null;
        if (!tryCatchFinally.FinallyBody.IsDefaultOrEmpty)
        {
            var statements = new List<LinqExpression>(tryCatchFinally.FinallyBody.Length);
            for (var i = 0; i < tryCatchFinally.FinallyBody.Length; i++)
                statements.Add(BoundEmitterSupport.AsObject(Emit(tryCatchFinally.FinallyBody[i])));
            finallyBody = LinqExpression.Block(statements);
        }

        if (catchBlocks.Count > 0 && finallyBody != null)
            return LinqExpression.TryCatchFinally(tryBody, finallyBody, catchBlocks.ToArray());
        if (catchBlocks.Count > 0)
            return LinqExpression.TryCatch(tryBody, catchBlocks.ToArray());
        if (finallyBody != null)
            return LinqExpression.TryFinally(tryBody, finallyBody);

        return tryBody;
    }

    private LinqExpression EmitCatchClauseBody(BoundCatchClause catchClause, ParameterExpression exParam)
    {
        var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), "catchPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "catchResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "catchSignal");
        var doneLabel = LinqExpression.Label("catchDone");
        var bodyStatements = new List<LinqExpression>();

        var previousDepth = _catchDepth;
        _catchDepth = previousDepth + 1;
        try
        {
            bodyStatements.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
            EmitStatementListBody(
                bodyStatements,
                catchClause.Body,
                resultVar,
                signalVar,
                doneLabel,
                unwrapReturnSignal: false);
            bodyStatements.Add(LinqExpression.Label(doneLabel));
        }
        finally
        {
            _catchDepth = previousDepth;
        }

        var scopedStatements = new List<LinqExpression>
        {
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod))
        };

        if (catchClause.VariableName != null)
        {
            scopedStatements.Add(
                LinqExpression.Call(
                    _contextParam,
                    ContextDefineNewMethod,
                    LinqExpression.Constant(catchClause.VariableName),
                    LinqExpression.Convert(exParam, typeof(object)),
                    LinqExpression.Call(exParam, typeof(object).GetMethod(nameof(GetType))!),
                    LinqExpression.Constant(false)));
        }

        scopedStatements.Add(
            LinqExpression.TryFinally(
                LinqExpression.Block(bodyStatements),
                LinqExpression.Assign(_contextParam, previousContextVar)));
        scopedStatements.Add(resultVar);

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            scopedStatements);
    }

    private LinqExpression? BuildCatchFilter(BoundCatchClause catchClause, ParameterExpression exParam)
    {
        LinqExpression? typeFilter = null;
        if (catchClause.ExceptionTypeName != null)
        {
            var resolvedType = ResolveTypeByName(catchClause.ExceptionTypeName);
            typeFilter = LinqExpression.Call(
                typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsType), [typeof(object), typeof(Type)])!,
                LinqExpression.Convert(exParam, typeof(object)),
                resolvedType);
        }

        LinqExpression? whenFilter = null;
        if (catchClause.WhenGuard != null)
        {
            whenFilter = LinqExpression.Call(
                EvaluateCatchWhenGuardMethod,
                LinqExpression.Constant(catchClause.WhenGuard, typeof(BoundExpr)),
                LinqExpression.Constant(catchClause.VariableName, typeof(string)),
                LinqExpression.Convert(exParam, typeof(object)),
                _contextParam,
                _optionsParam,
                _ctParam);
        }

        if (typeFilter == null)
            return whenFilter;
        if (whenFilter == null)
            return typeFilter;
        return LinqExpression.AndAlso(typeFilter, whenFilter);
    }

    private LinqExpression EmitSwitchExpression(BoundSwitchExpressionExpr switchExpression)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var resultVar = LinqExpression.Variable(typeof(object), "switchExprResult");
        var doneLabel = LinqExpression.Label("switchExprDone");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, BoundEmitterSupport.AsObject(Emit(switchExpression.Expression)))
        };

        for (var i = 0; i < switchExpression.Arms.Length; i++)
        {
            var arm = switchExpression.Arms[i];
            var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), $"switchArmPrevCtx{i}");
            var armCondition = (LinqExpression)LinqExpression.Call(
                MatchPatternMethod,
                valueVar,
                LinqExpression.Constant(arm.Pattern, typeof(Pattern)),
                _contextParam,
                _optionsParam,
                _ctParam);

            if (arm.WhenGuard != null)
            {
                armCondition = LinqExpression.AndAlso(
                    armCondition,
                    LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(arm.WhenGuard))));
            }

            statements.Add(
                LinqExpression.Block(
                    typeof(void),
                    [previousContextVar],
                    LinqExpression.Assign(previousContextVar, _contextParam),
                    LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
                    LinqExpression.TryFinally(
                        LinqExpression.IfThen(
                            armCondition,
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(Emit(arm.Value))),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Assign(_contextParam, previousContextVar))));
        }

        statements.Add(
            LinqExpression.Throw(
                LinqExpression.New(SwitchExpressionExceptionCtor, valueVar),
                typeof(void)));
        statements.Add(LinqExpression.Label(doneLabel));
        statements.Add(resultVar);

        return LinqExpression.Block(typeof(object), [valueVar, resultVar], statements);
    }

    private LinqExpression EmitSwitchStatement(BoundSwitchStatementExpr switchStatement)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var matchedVar = LinqExpression.Variable(typeof(bool), "switchMatched");
        var resultVar = LinqExpression.Variable(typeof(object), "switchResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "switchSignal");
        var doneLabel = LinqExpression.Label("switchDone");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, BoundEmitterSupport.AsObject(Emit(switchStatement.Expression))),
            LinqExpression.Assign(matchedVar, LinqExpression.Constant(false)),
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var defaultCaseIndex = -1;
        var previousSwitchDepth = _switchDepth;
        _switchDepth = previousSwitchDepth + 1;
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

                var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), $"switchPrevCtx{i}");
                var matchCondition = BuildSwitchCaseMatchCondition(valueVar, switchCase);
                var executeCase = EmitSwitchCaseExecution(switchStatement.Cases, i, resultVar, signalVar, doneLabel);

                statements.Add(
                    LinqExpression.Block(
                        typeof(void),
                        [previousContextVar],
                        LinqExpression.Assign(previousContextVar, _contextParam),
                        LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
                        LinqExpression.TryFinally(
                            LinqExpression.IfThen(
                                LinqExpression.AndAlso(LinqExpression.Not(matchedVar), matchCondition),
                                LinqExpression.Block(
                                    LinqExpression.Assign(matchedVar, LinqExpression.Constant(true)),
                                    LinqExpression.Assign(resultVar, executeCase),
                                    LinqExpression.Goto(doneLabel))),
                            LinqExpression.Assign(_contextParam, previousContextVar))));
            }

            if (defaultCaseIndex >= 0)
            {
                var executeDefault = EmitSwitchCaseExecution(switchStatement.Cases, defaultCaseIndex, resultVar, signalVar, doneLabel);
                statements.Add(
                    LinqExpression.IfThen(
                        LinqExpression.Not(matchedVar),
                        LinqExpression.Block(
                            LinqExpression.Assign(resultVar, executeDefault),
                            LinqExpression.Goto(doneLabel))));
            }

            statements.Add(LinqExpression.Label(doneLabel));
            statements.Add(resultVar);
            return LinqExpression.Block(typeof(object), [valueVar, matchedVar, resultVar, signalVar], statements);
        }
        finally
        {
            _switchDepth = previousSwitchDepth;
        }
    }

    private LinqExpression BuildSwitchCaseMatchCondition(
        ParameterExpression valueVar,
        BoundSwitchCase switchCase)
    {
        var patternMatch = LinqExpression.Call(
            MatchPatternMethod,
            valueVar,
            LinqExpression.Constant(switchCase.CasePattern!, typeof(Pattern)),
            _contextParam,
            _optionsParam,
            _ctParam);

        if (switchCase.WhenGuard == null)
            return patternMatch;

        return LinqExpression.AndAlso(
            patternMatch,
            LinqExpression.Call(RequireBooleanMethod, BoundEmitterSupport.AsObject(Emit(switchCase.WhenGuard))));
    }

    private LinqExpression EmitSwitchCaseExecution(
        ImmutableArray<BoundSwitchCase> cases,
        int startIndex,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget doneLabel)
    {
        var statements = new List<LinqExpression>();

        for (var i = startIndex; i < cases.Length; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            if (!TerminatesControlFlow(switchCase.Statements[^1]))
                throw new CsEvalException(DiagnosticDescriptors.CaseFallThrough);

            var caseDone = LinqExpression.Label($"switchCaseDone{i}");
            EmitStatementListBody(
                statements,
                switchCase.Statements,
                resultVar,
                signalVar,
                caseDone,
                unwrapReturnSignal: false);
            statements.Add(LinqExpression.Label(caseDone));

            var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
            statements.Add(
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Goto(doneLabel))));

            statements.Add(
                LinqExpression.Throw(
                    LinqExpression.Constant(new CsEvalException(DiagnosticDescriptors.CaseFallThrough)),
                    typeof(void)));
            break;
        }

        if (statements.Count == 0)
            return LinqExpression.Constant(null, typeof(object));

        statements.Add(resultVar);
        return LinqExpression.Block(typeof(object), statements);
    }

    private static bool TerminatesControlFlow(BoundExpr expr)
    {
        return expr switch
        {
            BoundBreakExpr => true,
            BoundReturnExpr => true,
            BoundContinueExpr => true,
            BoundThrowExpr => true,
            BoundThrowStatementExpr => true,
            BoundBlockExpr { Statements.Length: > 0 } block => TerminatesControlFlow(block.Statements[^1]),
            _ => false
        };
    }

    private LinqExpression EmitStatementSequence(ImmutableArray<BoundExpr> statements)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "tryResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "trySignal");
        var doneLabel = LinqExpression.Label("tryDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false);
        body.Add(LinqExpression.Label(doneLabel));
        body.Add(resultVar);

        return LinqExpression.Block(typeof(object), [resultVar, signalVar], body);
    }

    private void EmitLoopIterationBody(
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel,
        bool hasConditionCheck)
    {
        body.Add(LinqExpression.Call(
            CheckExecutionConstraintsMethod,
            LinqExpression.Call(_contextParam, GetConstraintStateProperty),
            LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.Constraints)),
            _ctParam));
        body.Add(LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(EmitScopedStatements(statements, includeConstraintChecks: false))));
        body.Add(BuildLoopSignalDispatch(resultVar, signalVar, breakLabel, continueLabel));
        if (!hasConditionCheck)
            body.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
    }

    private static LinqExpression BuildLoopSignalDispatch(
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel)
    {
        var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
        return LinqExpression.IfThen(
            LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
            LinqExpression.Block(
                LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                    LinqExpression.Block(
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Break(breakLabel, resultVar))),
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Continue)),
                    LinqExpression.Block(
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Goto(continueLabel))),
                LinqExpression.Break(breakLabel, resultVar)));
    }

    private LinqExpression EmitForeachIteration(
        string variableName,
        ParameterExpression currentValue,
        ImmutableArray<BoundExpr> statements)
    {
        var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), "foreachPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachIterResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "foreachIterSignal");
        var doneLabel = LinqExpression.Label("foreachIterDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Call(
                _contextParam,
                ContextDefineNewMethod,
                LinqExpression.Constant(variableName),
                currentValue,
                LinqExpression.Constant(typeof(object), typeof(Type)),
                LinqExpression.Constant(false))
        };
        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false,
            includeConstraintChecks: false);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private LinqExpression EmitChecked(BoundCheckedExpr checkedExpr)
    {
        var previous = _isChecked;
        _isChecked = checkedExpr.IsChecked;
        try
        {
            return Emit(checkedExpr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    private LinqExpression EmitChainedComparison(BoundChainedComparisonExpr chainedComparison)
    {
        var resultLabel = LinqExpression.Label(typeof(object), "chainResult");
        var variables = new List<ParameterExpression>();
        var body = new List<LinqExpression>();

        var firstValue = LinqExpression.Variable(typeof(object), "v0");
        variables.Add(firstValue);
        body.Add(LinqExpression.Assign(firstValue, BoundEmitterSupport.AsObject(Emit(chainedComparison.Operands[0]))));

        for (var i = 0; i < chainedComparison.Operators.Length; i++)
        {
            var nextValue = LinqExpression.Variable(typeof(object), $"v{i + 1}");
            variables.Add(nextValue);
            body.Add(LinqExpression.Assign(nextValue, BoundEmitterSupport.AsObject(Emit(chainedComparison.Operands[i + 1]))));

            var comparison = LinqExpression.Call(
                PerformComparisonMethod,
                variables[i],
                nextValue,
                LinqExpression.Constant(chainedComparison.Operators[i]),
                _optionsParam);

            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(comparison),
                LinqExpression.Return(resultLabel, LinqExpression.Constant(false, typeof(object)))));
        }

        body.Add(LinqExpression.Label(resultLabel, LinqExpression.Constant(true, typeof(object))));
        return LinqExpression.Block(typeof(object), variables, body);
    }

    private LinqExpression EmitScopedStatements(ImmutableArray<BoundExpr> statements, bool includeConstraintChecks = true)
    {
        var previousContextVar = LinqExpression.Variable(typeof(CsEvalContext), "scopePrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "scopeResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "scopeSignal");
        var doneLabel = LinqExpression.Label("scopeDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false,
            includeConstraintChecks: includeConstraintChecks);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private void EmitStatementListBody(
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget doneLabel,
        bool unwrapReturnSignal,
        bool includeConstraintChecks = true)
    {
        for (var i = 0; i < statements.Length; i++)
        {
            if (includeConstraintChecks)
            {
                body.Add(LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    LinqExpression.Call(_contextParam, GetConstraintStateProperty),
                    LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.Constraints)),
                    _ctParam));
            }
            body.Add(LinqExpression.Assign(resultVar, BoundEmitterSupport.AsObject(Emit(statements[i]))));
            body.Add(
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        unwrapReturnSignal
                            ? LinqExpression.IfThen(
                                LinqExpression.Equal(
                                    LinqExpression.Property(signalVar, ControlFlowSignalKindProperty),
                                    LinqExpression.Constant(ControlFlowSignal.Kind.Return)),
                                LinqExpression.Assign(
                                    resultVar,
                                    LinqExpression.Property(signalVar, ControlFlowValueProperty)))
                            : LinqExpression.Empty(),
                        LinqExpression.Goto(doneLabel))));
        }
    }

    private LinqExpression EmitRange(BoundRangeExpr range)
    {
        var start = LinqExpression.Call(ConvertToInt32ObjectMethod, BoundEmitterSupport.AsObject(Emit(range.Start)));
        var end = LinqExpression.Call(ConvertToInt32ObjectMethod, BoundEmitterSupport.AsObject(Emit(range.End)));
        return LinqExpression.Convert(
            LinqExpression.Call(
                GenerateRangeMethod,
                start,
                end,
                LinqExpression.Constant(range.ExclusiveEnd)),
            typeof(object));
    }

    private LinqExpression EmitVariableDecl(BoundVariableDeclExpr variableDecl)
    {
        return LinqExpression.Call(
            DefineVariableMethod,
            LinqExpression.Constant(variableDecl.Name),
            BoundEmitterSupport.AsObject(Emit(variableDecl.Initializer)),
            variableDecl.DeclaredType != null
                ? LinqExpression.Constant(variableDecl.DeclaredType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            _contextParam,
            LinqExpression.Constant(variableDecl.IsConst));
    }

    private LinqExpression EmitAssign(BoundAssignExpr assign)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "assignValue");
        return LinqExpression.Block(
            typeof(object),
            [valueVar],
            LinqExpression.Call(
                CheckAllowAssignmentMethod,
                _optionsParam,
                LinqExpression.Constant($"{assign.Name} = ...")),
            LinqExpression.Assign(valueVar, BoundEmitterSupport.AsObject(Emit(assign.Value))),
            LinqExpression.Assign(valueVar, LinqExpression.Call(
                ValidateVariableAssignmentMethod,
                LinqExpression.Constant(assign.Name),
                valueVar,
                _contextParam)),
            LinqExpression.Call(_contextParam, ContextSetMethod, LinqExpression.Constant(assign.Name), valueVar),
            valueVar);
    }

    private LinqExpression EmitNullCoalesceAssign(BoundNullCoalesceAssignExpr nullCoalesceAssign)
    {
        var currentVar = LinqExpression.Variable(typeof(object), "coalesceCurrent");
        var assignedVar = LinqExpression.Variable(typeof(object), "coalesceAssigned");
        return LinqExpression.Block(
            typeof(object),
            [currentVar, assignedVar],
            LinqExpression.Call(
                CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(nullCoalesceAssign.Name),
                _contextParam),
            LinqExpression.Assign(
                currentVar,
                LinqExpression.Call(_contextParam, ContextGetMethod, LinqExpression.Constant(nullCoalesceAssign.Name))),
            LinqExpression.Condition(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                currentVar,
                LinqExpression.Block(
                    LinqExpression.Call(
                        CheckAllowAssignmentMethod,
                        _optionsParam,
                        LinqExpression.Constant($"{nullCoalesceAssign.Name} ??= ...")),
                    LinqExpression.Assign(assignedVar, BoundEmitterSupport.AsObject(Emit(nullCoalesceAssign.Value))),
                    LinqExpression.Call(
                        _contextParam,
                        ContextSetMethod,
                        LinqExpression.Constant(nullCoalesceAssign.Name),
                        assignedVar),
                    assignedVar)));
    }

    private LinqExpression EmitCompoundAssign(BoundCompoundAssignExpr compoundAssign)
    {
        return LinqExpression.Call(
            ApplyCompoundAssignMethod,
            LinqExpression.Constant(compoundAssign.Name),
            LinqExpression.Constant(compoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(compoundAssign.Value)),
            _contextParam,
            _optionsParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIncrementDecrement(BoundIncrementDecrementExpr incrementDecrement)
    {
        return LinqExpression.Call(
            ApplyIncrementDecrementMethod,
            LinqExpression.Constant(incrementDecrement.Name),
            LinqExpression.Constant(incrementDecrement.Operator == TokenType.PlusPlus),
            LinqExpression.Constant(incrementDecrement.IsPrefix),
            _contextParam,
            _optionsParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitMemberAssign(BoundMemberAssignExpr memberAssign)
    {
        return LinqExpression.Call(
            ApplyMemberAssignMethod,
            BoundEmitterSupport.AsObject(Emit(memberAssign.Target)),
            LinqExpression.Constant(memberAssign.MemberName),
            BoundEmitterSupport.AsObject(Emit(memberAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexAssign.Index)),
            BoundEmitterSupport.AsObject(Emit(indexAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
        return LinqExpression.Call(
            ApplyMemberCompoundAssignMethod,
            BoundEmitterSupport.AsObject(Emit(memberCompoundAssign.Target)),
            LinqExpression.Constant(memberCompoundAssign.MemberName),
            LinqExpression.Constant(memberCompoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(memberCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
        return LinqExpression.Call(
            ApplyIndexCompoundAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Index)),
            LinqExpression.Constant(indexCompoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign)
    {
        return LinqExpression.Call(
            ApplyMemberNullCoalesceAssignMethod,
            BoundEmitterSupport.AsObject(Emit(memberNullCoalesceAssign.Target)),
            LinqExpression.Constant(memberNullCoalesceAssign.MemberName),
            BoundEmitterSupport.AsObject(Emit(memberNullCoalesceAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign)
    {
        return LinqExpression.Call(
            ApplyIndexNullCoalesceAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Index)),
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitMemberIncrement(BoundMemberIncrementExpr memberIncrement)
    {
        return LinqExpression.Call(
            ApplyMemberIncrementMethod,
            BoundEmitterSupport.AsObject(Emit(memberIncrement.Target)),
            LinqExpression.Constant(memberIncrement.MemberName),
            LinqExpression.Constant(memberIncrement.IsIncrement),
            LinqExpression.Constant(memberIncrement.IsPrefix),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIndexIncrement(BoundIndexIncrementExpr indexIncrement)
    {
        return LinqExpression.Call(
            ApplyIndexIncrementMethod,
            BoundEmitterSupport.AsObject(Emit(indexIncrement.Target)),
            BoundEmitterSupport.AsObject(Emit(indexIncrement.Index)),
            LinqExpression.Constant(indexIncrement.IsIncrement),
            LinqExpression.Constant(indexIncrement.IsPrefix),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitSlice(BoundSliceExpr slice)
    {
        var target = BoundEmitterSupport.AsObject(Emit(slice.Target));
        var start = slice.Start != null ? BoundEmitterSupport.AsObject(Emit(slice.Start)) : LinqExpression.Constant(null, typeof(object));
        var end = slice.End != null ? BoundEmitterSupport.AsObject(Emit(slice.End)) : LinqExpression.Constant(null, typeof(object));

        if (slice.Step != null)
        {
            return LinqExpression.Call(
                GetSliceStepMethod,
                target,
                start,
                end,
                BoundEmitterSupport.AsObject(Emit(slice.Step)),
                _optionsParam);
        }

        return LinqExpression.Call(
            GetSliceMethod,
            target,
            start,
            end,
            _optionsParam);
    }

    private LinqExpression EmitObjectCreation(BoundObjectCreationExpr objectCreation)
    {
        var argsArray = LinqExpression.NewArrayInit(
            typeof(object),
            objectCreation.Arguments.Select(arg => BoundEmitterSupport.AsObject(Emit(arg))));
        var result = LinqExpression.Call(
            InvokeConstructorMethod,
            ResolveTypeByName(objectCreation.TypeName),
            argsArray);

        if (objectCreation.InitializerEntries.IsDefaultOrEmpty)
            return result;

        var objVar = LinqExpression.Variable(typeof(object), "initObj");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(objVar, result)
        };

        for (var i = 0; i < objectCreation.InitializerEntries.Length; i++)
        {
            var entry = objectCreation.InitializerEntries[i];
            var value = BoundEmitterSupport.AsObject(Emit(entry.Value));
            if (entry.PropertyName != null)
            {
                statements.Add(LinqExpression.Call(
                    ApplyPropertyInitializerMethod,
                    objVar,
                    LinqExpression.Constant(entry.PropertyName),
                    value,
                    _optionsParam,
                    _contextParam));
            }
            else
            {
                statements.Add(LinqExpression.Call(
                    ApplyCollectionInitializerMethod,
                    objVar,
                    value));
            }
        }

        statements.Add(objVar);
        return LinqExpression.Block(typeof(object), [objVar], statements);
    }

    private LinqExpression EmitTypedArrayCreation(BoundTypedArrayCreationExpr typedArrayCreation)
    {
        return LinqExpression.Call(
            CreateTypedArrayFromTypeNameMethod,
            ResolveTypeByName(typedArrayCreation.ElementTypeName),
            BoundEmitterSupport.AsObject(Emit(typedArrayCreation.Size)));
    }

    private LinqExpression EmitTypedArrayLiteral(BoundTypedArrayLiteralExpr typedArrayLiteral)
    {
        var sourceArray = LinqExpression.NewArrayInit(
            typeof(object),
            typedArrayLiteral.Elements.Select(element => BoundEmitterSupport.AsObject(Emit(element))));
        return LinqExpression.Call(
            ConvertArrayToTypedMethod,
            sourceArray,
            ResolveTypeByName(typedArrayLiteral.ElementTypeName));
    }

    private LinqExpression EmitTuple(BoundTupleExpr tuple)
    {
        var elements = LinqExpression.NewArrayInit(
            typeof(object),
            tuple.Elements.Select(element => BoundEmitterSupport.AsObject(Emit(element))));
        return LinqExpression.Call(CreateTupleMethod, elements);
    }

    private LinqExpression EmitDeconstruction(BoundDeconstructionExpr deconstruction)
    {
        var variableNames = LinqExpression.NewArrayInit(
            typeof(string),
            deconstruction.VariableNames.Select(static name => LinqExpression.Constant(name)));
        return LinqExpression.Call(
            DeconstructTupleMethod,
            BoundEmitterSupport.AsObject(Emit(deconstruction.ValueExpression)),
            variableNames,
            _contextParam);
    }

    private LinqExpression EmitMultiDimTypedArrayCreation(BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation)
    {
        var sizes = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimTypedArrayCreation.Sizes.Select(size => BoundEmitterSupport.AsObject(Emit(size))));
        return LinqExpression.Call(
            CreateMultiDimArrayMethod,
            ResolveTypeByName(multiDimTypedArrayCreation.ElementTypeName),
            sizes);
    }

    private LinqExpression EmitMultiDimIndexAccess(BoundMultiDimIndexAccessExpr multiDimIndexAccess)
    {
        var target = BoundEmitterSupport.AsObject(Emit(multiDimIndexAccess.Target));
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimIndexAccess.Indices.Select(index => BoundEmitterSupport.AsObject(Emit(index))));

        if (!multiDimIndexAccess.NullSafe)
            return LinqExpression.Call(MultiDimArrayGetMethod, target, indices);

        var targetVar = LinqExpression.Variable(typeof(object), "mdTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, target),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(MultiDimArrayGetMethod, targetVar, indices)));
    }

    private LinqExpression EmitMultiDimIndexAssign(BoundMultiDimIndexAssignExpr multiDimIndexAssign)
    {
        var indices = LinqExpression.NewArrayInit(
            typeof(object),
            multiDimIndexAssign.Indices.Select(index => BoundEmitterSupport.AsObject(Emit(index))));
        return LinqExpression.Call(
            MultiDimArraySetMethod,
            BoundEmitterSupport.AsObject(Emit(multiDimIndexAssign.Target)),
            indices,
            BoundEmitterSupport.AsObject(Emit(multiDimIndexAssign.Value)));
    }

    private LinqExpression EmitThrow(BoundThrowExpr throwExpr)
    {
        var exception = LinqExpression.Call(ValidateThrowOperandMethod, BoundEmitterSupport.AsObject(Emit(throwExpr.Expression)));
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Throw(exception),
            LinqExpression.Default(typeof(object)));
    }

    private LinqExpression EmitMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        if (memberAccess.Plan?.Member is PropertyInfo property)
            return EmitDirectPropertyAccess(memberAccess, property);

        if (memberAccess.Plan?.Member is FieldInfo field)
            return EmitDirectFieldAccess(memberAccess, field);

        var target = BoundEmitterSupport.AsObject(Emit(memberAccess.Target));
        return LinqExpression.Call(
            GetMemberMethod,
            target,
            LinqExpression.Constant(memberAccess.MemberName),
            _optionsParam,
            LinqExpression.Constant(memberAccess.NullSafe),
            _contextParam);
    }

    private LinqExpression EmitIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        if (indexAccess.Plan?.IsDirectCollectionAccess == true)
            return EmitDirectCollectionIndexAccess(indexAccess);

        var targetExpr = BoundEmitterSupport.AsObject(Emit(indexAccess.Target));
        var indexExpr = BoundEmitterSupport.AsObject(Emit(indexAccess.Index));

        if (!indexAccess.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                _optionsParam,
                _contextParam);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "indexTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, targetExpr),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, _optionsParam, _contextParam)));
    }

    private LinqExpression EmitCall(BoundCallExpr call)
    {
        if (call.Callee is BoundMemberAccessExpr memberAccess && memberAccess.Plan != null)
            return EmitDirectPlannedCall(call, memberAccess);

        return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty);
    }

    private LinqExpression EmitDirectPropertyAccess(BoundMemberAccessExpr memberAccess, PropertyInfo property)
    {
        var plan = memberAccess.Plan!;
        var guardCheck = EmitMemberReadGuard(memberAccess, isField: false);

        if (plan.IsStatic)
        {
            var access = LinqExpression.Property(null, property);
            var guarded = BoundEmitterSupport.WrapGuardedValue(access, property.PropertyType, BoundEmitterSupport.CreateMemberGuardContext(memberAccess.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var targetType = property.DeclaringType ?? plan.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberAccess.MemberName));
        var typedTarget = BoundEmitterSupport.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Property(typedTarget, property);
        var guardedExpr = LinqExpression.Convert(
            BoundEmitterSupport.WrapGuardedValue(accessExpr, property.PropertyType, BoundEmitterSupport.CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(memberAccess.Target))),
            guardedExpr);
    }

    private LinqExpression EmitDirectFieldAccess(BoundMemberAccessExpr memberAccess, FieldInfo field)
    {
        var plan = memberAccess.Plan!;
        var guardCheck = EmitMemberReadGuard(memberAccess, isField: true);

        if (plan.IsStatic)
        {
            var access = LinqExpression.Field(null, field);
            var guarded = BoundEmitterSupport.WrapGuardedValue(access, field.FieldType, BoundEmitterSupport.CreateMemberGuardContext(memberAccess.MemberName));
            return LinqExpression.Block(
                typeof(object),
                guardCheck,
                LinqExpression.Convert(guarded, typeof(object)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "fieldTarget");
        var targetType = field.DeclaringType ?? plan.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberAccess.MemberName));
        var typedTarget = BoundEmitterSupport.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Field(typedTarget, field);
        var guardedExpr = LinqExpression.Convert(
            BoundEmitterSupport.WrapGuardedValue(accessExpr, field.FieldType, BoundEmitterSupport.CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(memberAccess.Target))),
            guardedExpr);
    }

    private LinqExpression EmitDirectCollectionIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var plan = indexAccess.Plan;
        if (plan == null)
            throw new BindingNotSupportedException("Direct collection index emission requires an index plan.");

        if (plan.TargetType == typeof(string))
            return EmitDirectStringIndexAccess(indexAccess);

        if (typeof(IList).IsAssignableFrom(plan.TargetType))
            return EmitDirectListIndexAccess(indexAccess);

        return LinqExpression.Call(
            GetIndexMethod,
            BoundEmitterSupport.AsObject(Emit(indexAccess.Target)),
            BoundEmitterSupport.AsObject(Emit(indexAccess.Index)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitDirectStringIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "indexTarget");
        var typedTarget = BoundEmitterSupport.EnsureTypedExpression(
            LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar),
            typeof(string));
        var indexExpr = BuildNormalizedIntIndex(indexAccess, LinqExpression.Property(typedTarget, StringLengthProperty));
        var charExpr = LinqExpression.Property(typedTarget, StringCharsProperty, indexExpr);
        var valueExpr = LinqExpression.Convert(charExpr, typeof(object));

        if (indexAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    valueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(indexAccess.Target))),
            valueExpr);
    }

    private LinqExpression EmitDirectListIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var plan = indexAccess.Plan
                   ?? throw new BindingNotSupportedException("Direct list index emission requires an index plan.");
        var targetObjVar = LinqExpression.Variable(typeof(object), "listTarget");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        LinqExpression typedTarget;
        LinqExpression countExpr;
        LinqExpression valueExpr;
        Type valueType;

        if (BoundEmitterSupport.TryGetIntIndexer(plan.TargetType, out var indexer) &&
            BoundEmitterSupport.TryGetCountProperty(plan.TargetType, out var countProperty))
        {
            typedTarget = BoundEmitterSupport.EnsureTypedExpression(checkedTarget, plan.TargetType);
            countExpr = LinqExpression.Property(typedTarget, countProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, indexer, indexExpr);
            valueType = indexer.PropertyType;
        }
        else
        {
            typedTarget = BoundEmitterSupport.EnsureTypedExpression(checkedTarget, typeof(IList));
            countExpr = LinqExpression.Property(
                BoundEmitterSupport.EnsureTypedExpression(typedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, IListIndexerProperty, indexExpr);
            valueType = typeof(object);
        }

        var guardedValueExpr = LinqExpression.Convert(
            BoundEmitterSupport.WrapGuardedValue(valueExpr, valueType, "index access"),
            typeof(object));

        if (indexAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedValueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(indexAccess.Target))),
            guardedValueExpr);
    }

    private LinqExpression BuildNormalizedIntIndex(BoundIndexAccessExpr indexAccess, LinqExpression lengthExpression)
    {
        if (indexAccess.Index is BoundLiteralExpr { Value: int literalIndex } && literalIndex >= 0)
            return LinqExpression.Constant(literalIndex, typeof(int));

        var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, BoundEmitterSupport.AsObject(Emit(indexAccess.Index)));
        var languageMode = LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.LanguageMode));
        return LinqExpression.Call(NormalizeIndexMethod, rawIndex, lengthExpression, languageMode);
    }

    private LinqExpression EmitDirectPlannedCall(BoundCallExpr call, BoundMemberAccessExpr memberAccess)
    {
        if (!BoundEmitterSupport.CanEmitDirectMethodCall(call.Plan, call.Arguments.Length))
            return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty);

        var method = call.Plan.SelectedMethod;
        var parameters = MethodDispatchCache.GetParameters(method);
        var guardCheck = EmitMethodCallGuard(method, call.Plan.IsStaticCall, call.Plan.IsModuleCall);
        var args = EmitPlannedCallArguments(call, parameters);

        if (call.Plan.IsStaticCall)
        {
            var staticCall = LinqExpression.Call(method, args);
            if (method.ReturnType == typeof(void))
                return LinqExpression.Block(guardCheck, staticCall, LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Block(
                method.ReturnType,
                guardCheck,
                BoundEmitterSupport.WrapGuardedValue(staticCall, method.ReturnType, BoundEmitterSupport.CreateMethodGuardContext(method.Name)));
        }

        var targetType = method.DeclaringType ?? memberAccess.Plan!.DeclaringType;
        var targetObjVar = LinqExpression.Variable(typeof(object), "callTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureCallTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(method.Name));
        var typedTarget = BoundEmitterSupport.EnsureTypedExpression(checkedTarget, targetType);
        var instanceCall = LinqExpression.Call(typedTarget, method, args);

        if (memberAccess.NullSafe)
        {
            var nullSafeBody = method.ReturnType == typeof(void)
                ? (LinqExpression)LinqExpression.Block(
                    typeof(object),
                    guardCheck,
                    instanceCall,
                    LinqExpression.Constant(null, typeof(object)))
                : LinqExpression.Convert(
                    LinqExpression.Block(
                        method.ReturnType,
                        guardCheck,
                        BoundEmitterSupport.WrapGuardedValue(instanceCall, method.ReturnType, BoundEmitterSupport.CreateMethodGuardContext(method.Name))),
                    typeof(object));

            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, BoundEmitterSupport.AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    nullSafeBody));
        }

        var targetVar = LinqExpression.Variable(targetType, "callTargetTyped");
        var assignTarget = LinqExpression.Assign(
            targetVar,
            BoundEmitterSupport.EnsureTypedExpression(Emit(memberAccess.Target), targetType));
        var ensureNonNullTarget = IsNonNullableValueType(targetType)
            ? (LinqExpression)LinqExpression.Empty()
            : LinqExpression.Call(
                EnsureCallTargetNotNullMethod,
                BoundEmitterSupport.AsObject(targetVar),
                LinqExpression.Constant(method.Name));
        var directInstanceCall = LinqExpression.Call(targetVar, method, args);

        if (method.ReturnType == typeof(void))
        {
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                guardCheck,
                assignTarget,
                ensureNonNullTarget,
                directInstanceCall,
                LinqExpression.Constant(null, typeof(object)));
        }

        return LinqExpression.Block(
            method.ReturnType,
            [targetVar],
            guardCheck,
            assignTarget,
            ensureNonNullTarget,
            BoundEmitterSupport.WrapGuardedValue(directInstanceCall, method.ReturnType, BoundEmitterSupport.CreateMethodGuardContext(method.Name)));
    }

    private static bool IsNonNullableValueType(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
    }

    private LinqExpression[] EmitPlannedCallArguments(BoundCallExpr call, ParameterInfo[] parameters)
    {
        var emitted = new LinqExpression[parameters.Length];
        var conversions = call.Plan.ArgumentConversions;

        foreach (var binding in call.Plan.ParameterBindings)
        {
            switch (binding.Kind)
            {
                case BoundParameterBindingKind.Argument:
                {
                    var sourceIndex = binding.SourceArgumentIndex;
                    var conversion = conversions[sourceIndex];
                    emitted[binding.ParameterIndex] = EmitCallArgument(call.Arguments[sourceIndex], conversion.TargetType);
                    break;
                }

                case BoundParameterBindingKind.DefaultValue:
                {
                    emitted[binding.ParameterIndex] = EmitDefaultArgument(parameters[binding.ParameterIndex]);
                    break;
                }

                case BoundParameterBindingKind.ParamsArray:
                {
                    var parameter = parameters[binding.ParameterIndex];
                    var elementType = parameter.ParameterType.GetElementType()
                                     ?? throw new BindingNotSupportedException("Params parameter must be an array type.");
                    var args = new LinqExpression[binding.SourceArgumentCount];

                    for (var i = 0; i < binding.SourceArgumentCount; i++)
                    {
                        var sourceIndex = binding.SourceArgumentIndex + i;
                        var conversion = conversions[sourceIndex];
                        var convertedArg = EmitCallArgument(call.Arguments[sourceIndex], conversion.TargetType);
                        args[i] = BoundEmitterSupport.EnsureTypedExpression(convertedArg, elementType);
                    }

                    emitted[binding.ParameterIndex] = LinqExpression.NewArrayInit(elementType, args);
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Bound parameter binding kind '{binding.Kind}' is not implemented");
            }
        }

        for (var i = 0; i < emitted.Length; i++)
        {
            if (emitted[i] == null)
                throw new BindingNotSupportedException($"No emitted argument for parameter index {i}.");
        }

        return emitted;
    }

    private static LinqExpression EmitDefaultArgument(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var defaultValue = parameter.DefaultValue;

        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
            return LinqExpression.Default(parameterType);

        return LinqExpression.Constant(defaultValue, parameterType);
    }

    private LinqExpression EmitCallArgument(BoundExpr argument, Type targetType)
    {
        var emittedArgument = Emit(argument);
        if (targetType == typeof(object))
            return BoundEmitterSupport.AsObject(emittedArgument);

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        if (emittedArgument.Type == typeof(object))
        {
            var coerced = LinqExpression.Call(
                CompilerReflectionCache.CoerceNumericMethod,
                emittedArgument,
                LinqExpression.Constant(targetType, typeof(Type)));
            return LinqExpression.Convert(coerced, targetType);
        }

        return LinqExpression.Convert(emittedArgument, targetType);
    }

    private LinqExpression EmitMethodCallGuard(MethodInfo method, bool isStaticCall, bool isModuleCall)
    {
        return LinqExpression.Call(
            EnsureMethodCallsAllowedMethod,
            _optionsParam,
            LinqExpression.Constant(method.Name),
            isStaticCall
                ? LinqExpression.Constant(method.DeclaringType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            LinqExpression.Constant(isModuleCall));
    }

    private LinqExpression EmitMemberReadGuard(BoundMemberAccessExpr memberAccess, bool isField)
    {
        var plan = memberAccess.Plan!;
        return LinqExpression.Call(
            EnsureMemberReadAllowedMethod,
            _optionsParam,
            LinqExpression.Constant(memberAccess.MemberName),
            LinqExpression.Constant(plan.IsStatic),
            LinqExpression.Constant(isField),
            LinqExpression.Constant(plan.DeclaringType, typeof(Type)));
    }

    private LinqExpression EmitInvoke(BoundInvokeExpr invoke)
    {
        return EmitInvokeCore(invoke.Callee, invoke.Arguments, invoke.TypeArguments);
    }

    private LinqExpression EmitLambda(BoundLambdaExpr lambda)
    {
        var parameters = LinqExpression.NewArrayInit(
            typeof(string),
            lambda.Parameters.Select(static name => LinqExpression.Constant(name)));

        return LinqExpression.Call(
            CreateLambdaValueMethod,
            parameters,
            LinqExpression.Constant(lambda.Body, typeof(Expr)),
            _contextParam,
            _optionsParam);
    }

    private LinqExpression EmitPipeline(BoundPipelineExpr pipeline)
    {
        if (pipeline.Right is BoundIdentifierExpr rightIdentifier)
        {
            return LinqExpression.Call(
                InvokePipelineIdentifierMethod,
                BoundEmitterSupport.AsObject(Emit(pipeline.Left)),
                LinqExpression.Constant(rightIdentifier.Name),
                _contextParam,
                _optionsParam,
                _ctParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            BoundEmitterSupport.AsObject(Emit(pipeline.Left)),
            BoundEmitterSupport.AsObject(Emit(pipeline.Right)),
            _contextParam,
            _optionsParam,
            _ctParam);
    }

    private LinqExpression EmitArrayLiteral(BoundArrayLiteralExpr arrayLiteral)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "arr");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(ListCtor))
        };

        for (var i = 0; i < arrayLiteral.Elements.Length; i++)
        {
            var element = arrayLiteral.Elements[i];
            if (element is BoundSpreadExpr spread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoListMethod,
                    listVar,
                    BoundEmitterSupport.AsObject(Emit(spread.Expression))));
                continue;
            }

            statements.Add(LinqExpression.Call(listVar, ListAddMethod, BoundEmitterSupport.AsObject(Emit(element))));
        }

        statements.Add(LinqExpression.Call(CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(typeof(object), [listVar], statements);
    }

    private LinqExpression EmitObjectLiteral(BoundObjectLiteralExpr objectLiteral)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(ExpandoObjectCtor))
        };

        var itemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        for (var i = 0; i < objectLiteral.Properties.Length; i++)
        {
            var property = objectLiteral.Properties[i];
            if (property.IsSpread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoDictMethod,
                    dictVar,
                    BoundEmitterSupport.AsObject(Emit(property.Value)),
                    _contextParam));
                continue;
            }

            statements.Add(LinqExpression.Assign(
                LinqExpression.Property(dictVar, itemProperty, LinqExpression.Constant(property.PropertyName!)),
                BoundEmitterSupport.AsObject(Emit(property.Value))));
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(typeof(object), [dictVar], statements);
    }

    private LinqExpression EmitInterpolatedString(BoundInterpolatedStringExpr interpolatedString)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var variables = new List<ParameterExpression> { sbVar };
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(StringBuilderCtor))
        };

        for (var i = 0; i < interpolatedString.Parts.Length; i++)
        {
            switch (interpolatedString.Parts[i])
            {
                case BoundInterpolatedTextPart textPart:
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod, LinqExpression.Constant(textPart.Text)));
                    break;

                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var valueVar = LinqExpression.Variable(typeof(object), $"interpValue{i}");
                    variables.Add(valueVar);
                    var format =
                        "{0" +
                        (expressionPart.AlignmentSpecifier != null ? "," + expressionPart.AlignmentSpecifier : string.Empty) +
                        (expressionPart.FormatSpecifier != null ? ":" + expressionPart.FormatSpecifier : string.Empty) +
                        "}";

                    statements.Add(LinqExpression.Assign(valueVar, BoundEmitterSupport.AsObject(Emit(expressionPart.Expression))));
                    statements.Add(LinqExpression.Call(
                        sbVar,
                        StringBuilderAppendMethod,
                        expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null
                            ? LinqExpression.Call(StringFormatMethod, LinqExpression.Constant(format), valueVar)
                            : LinqExpression.Condition(
                                LinqExpression.Equal(valueVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Constant(string.Empty),
                                LinqExpression.Call(valueVar, ObjectToStringMethod))));
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Bound interpolated part '{interpolatedString.Parts[i].GetType().Name}' is not implemented");
            }
        }

        statements.Add(LinqExpression.Call(sbVar, StringBuilderToStringMethod));
        return LinqExpression.Block(typeof(object), variables, statements);
    }

    private LinqExpression EmitNamedArgument(BoundNamedArgumentExpr namedArgument)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                NamedArgCtor,
                LinqExpression.Constant(namedArgument.Name),
                BoundEmitterSupport.AsObject(Emit(namedArgument.Value))),
            typeof(object));
    }

    private static LinqExpression EmitOutArg(BoundOutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                OutArgMarkerCtor,
                LinqExpression.Constant(outArg.VariableName),
                LinqExpression.Constant(outArg.TypeName, typeof(string)),
                LinqExpression.Constant(outArg.IsDiscard)),
            typeof(object));
    }

    private static LinqExpression EmitInvalidSpread()
    {
        throw new CsEvalException("Spread operator can only be used in array or object literals");
    }

    private LinqExpression EmitInvokeCore(
        BoundExpr callee,
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<string> typeArguments)
    {
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            arguments.Select(argument => BoundEmitterSupport.AsObject(Emit(argument))));
        var emittedTypeArguments = EmitTypeArguments(typeArguments);
        var outBindings = BoundEmitterSupport.CollectOutBindings(arguments);

        LinqExpression invokeExpr;
        if (callee is BoundIdentifierExpr identifier)
        {
            invokeExpr = LinqExpression.Call(
                InvokeIdentifierCallMethod,
                LinqExpression.Constant(identifier.Name),
                argsVar,
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }
        else if (callee is BoundMemberAccessExpr memberAccess)
        {
            invokeExpr = LinqExpression.Call(
                InvokeMemberCallMethod,
                BoundEmitterSupport.AsObject(Emit(memberAccess.Target)),
                LinqExpression.Constant(memberAccess.MemberName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }
        else
        {
            invokeExpr = LinqExpression.Call(
                InvokeCallMethod,
                BoundEmitterSupport.AsObject(Emit(callee)),
                argsVar,
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }

        if (outBindings.Length == 0)
        {
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                invokeExpr);
        }

        var resultVar = LinqExpression.Variable(typeof(object), "invokeResult");
        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, invokeExpr),
            LinqExpression.Call(
                DefineOutVariablesMethod,
                argsVar,
                LinqExpression.Constant(outBindings, typeof(IReadOnlyList<OutVariableBinding>)),
                _contextParam),
            resultVar);
    }

    private static LinqExpression EmitTypeArguments(ImmutableArray<string> typeArguments)
    {
        if (typeArguments.IsDefaultOrEmpty)
            return LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        return LinqExpression.Constant(typeArguments.ToArray(), typeof(IReadOnlyList<string>));
    }

    private LinqExpression ResolveTypeByName(string typeName)
    {
        return LinqExpression.Call(
            LinqExpression.Call(_contextParam, GetTypeResolverProperty),
            ResolveTypeMethod,
            LinqExpression.Constant(typeName));
    }
}
