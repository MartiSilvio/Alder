using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private static ConstantExpression EmitLiteral(BoundLiteralExpr literal)
    {
        if (literal.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(literal.Value, literal.Value.GetType());
    }

    private LinqExpression EmitIdentifier(BoundIdentifierExpr identifier)
    {
        if (identifier.LocalId is { } localId &&
            _promotedLocals != null &&
            _promotedLocals.TryGetValue(localId, out var promoted))
        {
            return promoted.Variable;
        }

        if (_hoistedIdentifiers != null &&
            _hoistedIdentifiers.TryGetValue(identifier.Name, out var hoisted))
        {
            return hoisted.Variable;
        }

        if (identifier.StaticType != typeof(object) && !IsValueTupleType(identifier.StaticType))
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
        var sourceType = cast.Expression.StaticType;
        var targetType = cast.TargetType;

        if (sourceType != typeof(object) && !sourceType.IsEnum && !targetType.IsEnum)
        {
            if ((TypeHelpers.IsArithmetic(sourceType) || sourceType == typeof(bool)) &&
                (TypeHelpers.IsArithmetic(targetType) || targetType == typeof(bool)))
            {
                var operand = Emit(cast.Expression);
                operand = EmitHelpers.EnsureTypedExpression(operand, sourceType);
                return _isChecked
                    ? LinqExpression.ConvertChecked(operand, targetType)
                    : LinqExpression.Convert(operand, targetType);
            }

            if (!targetType.IsValueType)
            {
                var operand = Emit(cast.Expression);
                operand = EmitHelpers.EnsureTypedExpression(operand, sourceType);
                return LinqExpression.Convert(operand, targetType);
            }
        }

        return LinqExpression.Call(
            ExplicitCastMethod,
            EmitHelpers.AsObject(Emit(cast.Expression)),
            LinqExpression.Constant(cast.TargetType, typeof(Type)),
            cast.SourceStaticType == null
                ? LinqExpression.Constant(null, typeof(Type))
                : LinqExpression.Constant(cast.SourceStaticType, typeof(Type)),
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitAs(BoundAsExpr asExpr)
    {
        if (!asExpr.TargetType.IsValueType)
        {
            var operand = Emit(asExpr.Expression);
            operand = EmitHelpers.AsObject(operand);
            return LinqExpression.TypeAs(operand, asExpr.TargetType);
        }

        return LinqExpression.Call(
            TryAsMethod,
            EmitHelpers.AsObject(Emit(asExpr.Expression)),
            LinqExpression.Constant(asExpr.TargetType, typeof(Type)));
    }

    private LinqExpression EmitIsPattern(BoundIsPatternExpr isPattern)
    {
        if (isPattern.Pattern is TypePattern { VariableName: null } typePattern
            && BuiltInTypeNames.TryGetValue(typePattern.TypeToken.Lexeme, out var resolvedType))
        {
            return LinqExpression.Convert(
                LinqExpression.TypeIs(
                    EmitHelpers.AsObject(Emit(isPattern.Expression)),
                    resolvedType),
                typeof(object));
        }

        return LinqExpression.Convert(
            LinqExpression.Call(
                MatchPatternMethod,
                EmitHelpers.AsObject(Emit(isPattern.Expression)),
                LinqExpression.Constant(isPattern.Pattern, typeof(Pattern)),
                _contextParam,
                _optionsParam,
                _ctParam),
            typeof(object));
    }

    private static readonly Dictionary<string, Type> BuiltInTypeNames = new(StringComparer.Ordinal)
    {
        ["sbyte"] = typeof(sbyte), ["byte"] = typeof(byte),
        ["short"] = typeof(short), ["ushort"] = typeof(ushort),
        ["int"] = typeof(int), ["uint"] = typeof(uint),
        ["long"] = typeof(long), ["ulong"] = typeof(ulong),
        ["float"] = typeof(float), ["double"] = typeof(double),
        ["decimal"] = typeof(decimal), ["bool"] = typeof(bool),
        ["char"] = typeof(char), ["string"] = typeof(string),
        ["object"] = typeof(object),
    };

    private LinqExpression EmitBoolCondition(BoundExpr condition)
    {
        var emitted = Emit(condition);
        if (condition.StaticType == typeof(bool) && emitted.Type == typeof(bool))
            return emitted;
        return LinqExpression.Call(RequireBooleanMethod, EmitHelpers.AsObject(emitted));
    }

    private LinqExpression EmitUnary(BoundUnaryExpr unary)
    {
        if (unary.PromotedType is { } promoted && !_isChecked)
        {
            var operand = EmitHelpers.EnsureTypedExpression(Emit(unary.Operand), unary.Operand.StaticType);
            if (operand.Type != promoted)
                operand = LinqExpression.Convert(operand, promoted);

            return unary.Operator switch
            {
                TokenType.Bang => LinqExpression.Not(operand),
                TokenType.Tilde => LinqExpression.Not(operand),
                TokenType.Minus => LinqExpression.Negate(operand),
                TokenType.Plus => operand,
                _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{unary.Operator}'")
            };
        }

        var boxed = EmitHelpers.AsObject(Emit(unary.Operand));
        return unary.Operator switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, boxed, LinqExpression.Constant(_isChecked)),
            TokenType.Plus => LinqExpression.Call(UnaryPlusMethod, boxed),
            TokenType.Bang => LinqExpression.Call(LogicalNotMethod, boxed),
            TokenType.Tilde => LinqExpression.Call(BitwiseNotMethod, boxed),
            _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{unary.Operator}'")
        };
    }

    private LinqExpression EmitBinary(BoundBinaryExpr binary)
    {
        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = binary;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = Emit(leftmost);
        for (var i = chain.Count - 1; i >= 0; i--)
            result = EmitBinaryFold(chain[i], result);
        return result;
    }

    private LinqExpression EmitBinaryFold(BoundBinaryExpr binary, LinqExpression left)
    {
        if (TryEmitPrimitiveBinaryFastPathWithLeft(binary, left, out var direct))
            return direct;

        if (TryEmitStringConcatFastPathWithLeft(binary, left, out var stringDirect))
            return stringDirect;

        if (ShouldApplyConstantPromotion(binary))
            return EmitBinaryWithConstantPromotionWithLeft(binary, left);

        var isStringContext = binary.Operator == TokenType.Plus &&
            (binary.Left.StaticType == typeof(string) || binary.Right.StaticType == typeof(string));

        return EmitBinaryCore(binary.Operator, EmitHelpers.AsObject(left), EmitHelpers.AsObject(Emit(binary.Right)), isStringContext);
    }

    private bool TryEmitStringConcatFastPath(BoundBinaryExpr binary, out LinqExpression result)
    {
        result = null!;
        if (binary.Operator != TokenType.Plus)
            return false;

        var leftIsString = binary.Left.StaticType == typeof(string);
        var rightIsString = binary.Right.StaticType == typeof(string);
        if (!leftIsString && !rightIsString)
            return false;

        var left = Emit(binary.Left);
        var right = Emit(binary.Right);

        if (!leftIsString)
            left = EmitHelpers.ToStringExpression(left);
        else
            left = EmitHelpers.EnsureTypedExpression(left, typeof(string));

        if (!rightIsString)
            right = EmitHelpers.ToStringExpression(right);
        else
            right = EmitHelpers.EnsureTypedExpression(right, typeof(string));

        result = LinqExpression.Call(StringConcatTwoStringsMethod, left, right);
        return true;
    }

    private bool TryEmitStringConcatFastPathWithLeft(BoundBinaryExpr binary, LinqExpression preEmittedLeft, out LinqExpression result)
    {
        result = null!;
        if (binary.Operator != TokenType.Plus)
            return false;

        var leftIsString = binary.Left.StaticType == typeof(string);
        var rightIsString = binary.Right.StaticType == typeof(string);
        if (!leftIsString && !rightIsString)
            return false;

        var left = preEmittedLeft;
        var right = Emit(binary.Right);

        if (!leftIsString)
            left = EmitHelpers.ToStringExpression(left);
        else
            left = EmitHelpers.EnsureTypedExpression(left, typeof(string));

        if (!rightIsString)
            right = EmitHelpers.ToStringExpression(right);
        else
            right = EmitHelpers.EnsureTypedExpression(right, typeof(string));

        result = LinqExpression.Call(StringConcatTwoStringsMethod, left, right);
        return true;
    }

    private BlockExpression EmitBinaryWithConstantPromotion(BoundBinaryExpr binary)
    {
        var leftVar = LinqExpression.Variable(typeof(object), "binaryLeft");
        var rightVar = LinqExpression.Variable(typeof(object), "binaryRight");
        var promotedVar = LinqExpression.Variable(typeof(ValueTuple<object?, object?>), "binaryPromoted");

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar, promotedVar],
            LinqExpression.Assign(leftVar, EmitHelpers.AsObject(Emit(binary.Left))),
            LinqExpression.Assign(rightVar, EmitHelpers.AsObject(Emit(binary.Right))),
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

    private BlockExpression EmitBinaryWithConstantPromotionWithLeft(BoundBinaryExpr binary, LinqExpression preEmittedLeft)
    {
        var leftVar = LinqExpression.Variable(typeof(object), "binaryLeft");
        var rightVar = LinqExpression.Variable(typeof(object), "binaryRight");
        var promotedVar = LinqExpression.Variable(typeof(ValueTuple<object?, object?>), "binaryPromoted");

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar, promotedVar],
            LinqExpression.Assign(leftVar, EmitHelpers.AsObject(preEmittedLeft)),
            LinqExpression.Assign(rightVar, EmitHelpers.AsObject(Emit(binary.Right))),
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

    private MethodCallExpression EmitBinaryCore(TokenType op, LinqExpression left, LinqExpression right, bool isStringContext = false)
    {
        left = EmitHelpers.AsObject(left);
        right = EmitHelpers.AsObject(right);

        return op switch
        {
            TokenType.Plus => LinqExpression.Call(AddMethod, left, right, _optionsParam, _contextParam, LinqExpression.Constant(_isChecked), LinqExpression.Constant(isStringContext)),
            TokenType.Minus => LinqExpression.Call(SubtractMethod, left, right, LinqExpression.Constant(_isChecked)),
            TokenType.Star => LinqExpression.Call(MultiplyMethod, left, right, _optionsParam, LinqExpression.Constant(_isChecked)),
            TokenType.Slash => LinqExpression.Call(DivideMethod, left, right),
            TokenType.Percent => LinqExpression.Call(ModuloMethod, left, right),
            TokenType.EqualEqual => LinqExpression.Call(EqualsMethod, left, right),
            TokenType.BangEqual => LinqExpression.Call(NotEqualsMethod, left, right),
            TokenType.EqualEqualEqual => LinqExpression.Call(StrictEqualsMethod, left, right),
            TokenType.BangEqualEqual => LinqExpression.Call(StrictNotEqualsMethod, left, right),
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
            TokenType.Like => LinqExpression.Call(LikeMethod, left, right, _optionsParam),
            TokenType.EqualTilde => LinqExpression.Call(RegexMatchMethod, left, right),
            TokenType.BangTilde => LinqExpression.Call(RegexNotMatchMethod, left, right),
            TokenType.LessEqualGreater => LinqExpression.Call(SpaceshipMethod, left, right),
            _ => throw new BindingNotSupportedException($"Unsupported bound binary operator '{op}'")
        };
    }

    private bool TryEmitPrimitiveBinaryFastPath(BoundBinaryExpr binary, out LinqExpression direct)
    {
        direct = null!;
        if (binary.PromotedType is not { } promotedType)
            return false;

        var isShift = binary.Operator is TokenType.LessLess or TokenType.GreaterGreater;
        var left = EmitHelpers.EnsureTypedExpression(Emit(binary.Left), binary.Left.StaticType);
        var right = EmitHelpers.EnsureTypedExpression(Emit(binary.Right), binary.Right.StaticType);
        if (left.Type != promotedType)
            left = LinqExpression.Convert(left, promotedType);
        var rightTarget = isShift ? typeof(int) : promotedType;
        if (right.Type != rightTarget)
            right = LinqExpression.Convert(right, rightTarget);

        return TryEmitPrimitiveBinaryOp(binary.Operator, left, right, out direct);
    }

    private bool TryEmitPrimitiveBinaryFastPathWithLeft(BoundBinaryExpr binary, LinqExpression preEmittedLeft, out LinqExpression direct)
    {
        direct = null!;
        if (binary.PromotedType is not { } promotedType)
            return false;

        var isShift = binary.Operator is TokenType.LessLess or TokenType.GreaterGreater;
        var left = EmitHelpers.EnsureTypedExpression(preEmittedLeft, binary.Left.StaticType);
        var right = EmitHelpers.EnsureTypedExpression(Emit(binary.Right), binary.Right.StaticType);
        if (left.Type != promotedType)
            left = LinqExpression.Convert(left, promotedType);
        var rightTarget = isShift ? typeof(int) : promotedType;
        if (right.Type != rightTarget)
            right = LinqExpression.Convert(right, rightTarget);

        return TryEmitPrimitiveBinaryOp(binary.Operator, left, right, out direct);
    }

    private bool TryEmitPrimitiveBinaryOp(TokenType op, LinqExpression left, LinqExpression right, out LinqExpression direct)
    {
        LinqExpression? typed = op switch
        {
            TokenType.Plus => _isChecked ? LinqExpression.AddChecked(left, right) : LinqExpression.Add(left, right),
            TokenType.Minus => _isChecked ? LinqExpression.SubtractChecked(left, right) : LinqExpression.Subtract(left, right),
            TokenType.Star => _isChecked ? LinqExpression.MultiplyChecked(left, right) : LinqExpression.Multiply(left, right),
            TokenType.Slash => LinqExpression.Divide(left, right),
            TokenType.Percent => LinqExpression.Modulo(left, right),
            TokenType.EqualEqual => LinqExpression.Equal(left, right),
            TokenType.BangEqual => LinqExpression.NotEqual(left, right),
            TokenType.Less => LinqExpression.LessThan(left, right),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(left, right),
            TokenType.Greater => LinqExpression.GreaterThan(left, right),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(left, right),
            TokenType.Amp => LinqExpression.And(left, right),
            TokenType.Pipe => LinqExpression.Or(left, right),
            TokenType.Caret => LinqExpression.ExclusiveOr(left, right),
            TokenType.LessLess => LinqExpression.LeftShift(left, right),
            TokenType.GreaterGreater => LinqExpression.RightShift(left, right),
            _ => null
        };

        if (typed == null)
        {
            direct = null!;
            return false;
        }

        direct = typed;
        return true;
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

        // §12.14.2: nullable bool conditional operators
        if (logical.Left.StaticType == typeof(bool?) || logical.Right.StaticType == typeof(bool?))
        {
            var method = logical.Operator == TokenType.AmpAmp ? NullableBoolAndMethod : NullableBoolOrMethod;
            return LinqExpression.Call(method, EmitHelpers.AsObject(leftCandidate), EmitHelpers.AsObject(rightCandidate));
        }

        var opLexeme = TokenLexemes.GetCanonical(logical.Operator);
        var leftBool = LinqExpression.Call(
            RequireBooleanForLogicalOperatorMethod,
            EmitHelpers.AsObject(leftCandidate),
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(EmitHelpers.GetBoundTypeName(logical.Right)));
        var rightBoolAsObject = LinqExpression.Convert(
            LinqExpression.Call(
                RequireBooleanForLogicalOperatorMethod,
                EmitHelpers.AsObject(rightCandidate),
                LinqExpression.Constant(opLexeme),
                LinqExpression.Constant(EmitHelpers.GetBoundTypeName(logical.Left))),
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
        var leftType = nullCoalesce.Left.StaticType;
        var rightType = nullCoalesce.Right.StaticType;

        if (leftType != typeof(object) && rightType != typeof(object)
            && (leftType.IsClass || leftType.IsInterface || Nullable.GetUnderlyingType(leftType) != null))
        {
            var left = Emit(nullCoalesce.Left);
            left = EmitHelpers.EnsureTypedExpression(left, leftType);
            var right = Emit(nullCoalesce.Right);
            right = EmitHelpers.EnsureTypedExpression(right, rightType);

            if (left.Type != right.Type)
            {
                var commonType = nullCoalesce.StaticType;
                if (commonType != typeof(object))
                {
                    if (right.Type != commonType)
                        right = LinqExpression.Convert(right, commonType);
                }
                else
                {
                    left = EmitHelpers.AsObject(left);
                    right = EmitHelpers.AsObject(right);
                }
            }

            return LinqExpression.Coalesce(left, right);
        }

        var leftVar = LinqExpression.Variable(typeof(object), "coalesceLeft");
        return LinqExpression.Block(
            typeof(object),
            [leftVar],
            LinqExpression.Assign(leftVar, EmitHelpers.AsObject(Emit(nullCoalesce.Left))),
            LinqExpression.Condition(
                LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(object))),
                EmitHelpers.AsObject(Emit(nullCoalesce.Right)),
                leftVar));
    }

    private LinqExpression EmitConditional(BoundConditionalExpr conditional)
    {
        var conditionCandidate = Emit(conditional.Condition);
        var condition = conditionCandidate.Type == typeof(bool)
            ? conditionCandidate
            : LinqExpression.Call(RequireBooleanMethod, EmitHelpers.AsObject(conditionCandidate));

        var thenCandidate = Emit(conditional.ThenBranch);
        var elseCandidate = Emit(conditional.ElseBranch);

        if (TryEmitTypedConditional(conditional, condition, thenCandidate, elseCandidate, out var typed))
            return typed;

        if (thenCandidate.Type == elseCandidate.Type && thenCandidate.Type != typeof(object))
        {
            return EmitHelpers.AsObject(
                LinqExpression.Condition(condition, thenCandidate, elseCandidate));
        }

        return LinqExpression.Condition(
            condition,
            EmitHelpers.AsObject(thenCandidate),
            EmitHelpers.AsObject(elseCandidate));
    }

    private static bool TryEmitTypedConditional(
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

        if (thenType == typeof(object) || elseType == typeof(object) || resultType == typeof(object))
            return false;

        if (!IsConvertSafe(thenType, resultType) || !IsConvertSafe(elseType, resultType))
            return false;

        var thenTyped = EmitHelpers.EnsureTypedExpression(thenCandidate, thenType);
        var elseTyped = EmitHelpers.EnsureTypedExpression(elseCandidate, elseType);

        if (thenTyped.Type != resultType)
            thenTyped = LinqExpression.Convert(thenTyped, resultType);
        if (elseTyped.Type != resultType)
            elseTyped = LinqExpression.Convert(elseTyped, resultType);

        typed = LinqExpression.Condition(condition, thenTyped, elseTyped);
        return true;
    }

    private static bool IsConvertSafe(Type sourceType, Type targetType) =>
        sourceType == targetType
        || targetType.IsAssignableFrom(sourceType)
        || (IsArithmeticFastPathType(sourceType) && IsArithmeticFastPathType(targetType));
}