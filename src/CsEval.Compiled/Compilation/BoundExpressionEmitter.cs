using System.Collections.Immutable;
using System.Collections;
using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;

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

    private static readonly MethodInfo ResolveIdentifierMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ResolveIdentifier))!;

    private static readonly MethodInfo GetMemberMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetMember))!;

    private static readonly MethodInfo GetIndexMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetIndex))!;

    private static readonly MethodInfo NormalizeIndexMethod =
        typeof(MemberAccess).GetMethod(nameof(MemberAccess.NormalizeIndex), [typeof(int), typeof(int), typeof(LanguageMode)])!;

    private static readonly MethodInfo ConvertToInt32ObjectMethod =
        typeof(Convert).GetMethod(nameof(Convert.ToInt32), [typeof(object)])!;

    private static readonly MethodInfo InvokeCallMethod =
        typeof(CsEval.Runtime.MethodInvoker).GetMethod(nameof(CsEval.Runtime.MethodInvoker.InvokeCall))!;

    private static readonly MethodInfo InvokeMemberCallMethod =
        typeof(CsEval.Runtime.MethodInvoker).GetMethod(nameof(CsEval.Runtime.MethodInvoker.InvokeMemberCall))!;

    private static readonly MethodInfo InvokeIdentifierCallMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.InvokeIdentifierCall))!;

    private static readonly MethodInfo InvokePipelineIdentifierMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.InvokePipelineIdentifier))!;

    private static readonly MethodInfo InvokePipelineMethod =
        typeof(PipelineOperator).GetMethod(nameof(PipelineOperator.InvokePipeline))!;

    private static readonly MethodInfo CreateLambdaValueMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateLambdaValue))!;

    private static readonly MethodInfo EnsureMethodCallsAllowedMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureMethodCallsAllowed))!;

    private static readonly MethodInfo EnsureMemberReadAllowedMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureMemberReadAllowed))!;

    private static readonly MethodInfo EnsureMemberTargetNotNullMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureMemberTargetNotNull))!;

    private static readonly MethodInfo EnsureCallTargetNotNullMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureCallTargetNotNull))!;

    private static readonly MethodInfo EnsureIndexTargetNotNullMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureIndexTargetNotNull))!;

    private static readonly MethodInfo RequireBooleanMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBoolean))!;

    private static readonly MethodInfo RequireBooleanForLogicalOperatorMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBooleanForLogicalOperator))!;

    private static readonly MethodInfo ExplicitCastMethod =
        typeof(TypeHelpers).GetMethod(
            nameof(TypeHelpers.ExplicitCast),
            [typeof(object), typeof(Type), typeof(Type), typeof(bool)])!;

    private static readonly MethodInfo TryAsMethod =
        typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.TryAs), [typeof(object), typeof(Type)])!;

    private static readonly MethodInfo PromoteToTypeMethod =
        typeof(NumericDispatch).GetMethod(nameof(NumericDispatch.PromoteToType), [typeof(object), typeof(Type)])!;

    private static readonly PropertyInfo StringLengthProperty =
        typeof(string).GetProperty(nameof(string.Length))!;

    private static readonly PropertyInfo StringCharsProperty =
        typeof(string).GetProperty("Chars")!;

    private static readonly PropertyInfo IListIndexerProperty =
        typeof(IList).GetProperty("Item")!;

    private static readonly PropertyInfo ICollectionCountProperty =
        typeof(ICollection).GetProperty(nameof(ICollection.Count))!;

    private static readonly MethodInfo NegateMethod =
        typeof(Operators).GetMethod(nameof(Operators.Negate), [typeof(object), typeof(bool)])!;

    private static readonly MethodInfo UnaryPlusMethod =
        typeof(Operators).GetMethod(nameof(Operators.UnaryPlus), [typeof(object)])!;

    private static readonly MethodInfo LogicalNotMethod =
        typeof(Operators).GetMethod(nameof(Operators.LogicalNot), [typeof(object)])!;

    private static readonly MethodInfo BitwiseNotMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseNot), [typeof(object)])!;

    private static readonly MethodInfo AddMethod =
        typeof(Operators).GetMethod(nameof(Operators.Add), [typeof(object), typeof(object), typeof(CsEvalOptions)])!;

    private static readonly MethodInfo SubtractMethod =
        typeof(Operators).GetMethod(nameof(Operators.Subtract), [typeof(object), typeof(object), typeof(bool)])!;

    private static readonly MethodInfo MultiplyMethod =
        typeof(Operators).GetMethod(nameof(Operators.Multiply), [typeof(object), typeof(object), typeof(CsEvalOptions), typeof(bool)])!;

    private static readonly MethodInfo DivideMethod =
        typeof(Operators).GetMethod(nameof(Operators.Divide), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo ModuloMethod =
        typeof(Operators).GetMethod(nameof(Operators.Modulo), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo EqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.Equals), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo NotEqualsMethod =
        typeof(Operators).GetMethod(nameof(Operators.NotEquals), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo LessThanMethod =
        typeof(Operators).GetMethod(nameof(Operators.LessThan), [typeof(object), typeof(object), typeof(CsEvalOptions)])!;

    private static readonly MethodInfo LessThanOrEqualMethod =
        typeof(Operators).GetMethod(nameof(Operators.LessThanOrEqual), [typeof(object), typeof(object), typeof(CsEvalOptions)])!;

    private static readonly MethodInfo GreaterThanMethod =
        typeof(Operators).GetMethod(nameof(Operators.GreaterThan), [typeof(object), typeof(object), typeof(CsEvalOptions)])!;

    private static readonly MethodInfo GreaterThanOrEqualMethod =
        typeof(Operators).GetMethod(nameof(Operators.GreaterThanOrEqual), [typeof(object), typeof(object), typeof(CsEvalOptions)])!;

    private static readonly MethodInfo BitwiseAndMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseAnd), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo BitwiseOrMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseOr), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo BitwiseXorMethod =
        typeof(Operators).GetMethod(nameof(Operators.BitwiseXor), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo LeftShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.LeftShift), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo RightShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.RightShift), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo UnsignedRightShiftMethod =
        typeof(Operators).GetMethod(nameof(Operators.UnsignedRightShift), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo PowerMethod =
        typeof(Operators).GetMethod(nameof(Operators.Power), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo InOperatorMethod =
        typeof(Operators).GetMethod(nameof(Operators.InOperator), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo LikeMethod =
        typeof(Operators).GetMethod(nameof(Operators.Like), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo RegexMatchMethod =
        typeof(Operators).GetMethod(nameof(Operators.RegexMatch), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo RegexNotMatchMethod =
        typeof(Operators).GetMethod(nameof(Operators.RegexNotMatch), [typeof(object), typeof(object)])!;

    private static readonly MethodInfo SpaceshipMethod =
        typeof(Operators).GetMethod(nameof(Operators.Spaceship), [typeof(object), typeof(object)])!;

    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression optionsParam,
        ParameterExpression ctParam)
    {
        _contextParam = contextParam;
        _optionsParam = optionsParam;
        _ctParam = ctParam;
    }

    public LinqExpression Emit(BoundExpr expr)
    {
        return expr switch
        {
            BoundLiteralExpr literal => EmitLiteral(literal),
            BoundIdentifierExpr identifier => EmitIdentifier(identifier),
            BoundCastExpr cast => EmitCast(cast),
            BoundAsExpr asExpr => EmitAs(asExpr),
            BoundUnaryExpr unary => EmitUnary(unary),
            BoundBinaryExpr binary => EmitBinary(binary),
            BoundLogicalExpr logical => EmitLogical(logical),
            BoundNullCoalesceExpr nullCoalesce => EmitNullCoalesce(nullCoalesce),
            BoundConditionalExpr conditional => EmitConditional(conditional),
            BoundMemberAccessExpr memberAccess => EmitMemberAccess(memberAccess),
            BoundIndexAccessExpr indexAccess => EmitIndexAccess(indexAccess),
            BoundCallExpr call => EmitCall(call),
            BoundInvokeExpr invoke => EmitInvoke(invoke),
            BoundLambdaExpr lambda => EmitLambda(lambda),
            BoundPipelineExpr pipeline => EmitPipeline(pipeline),
            _ => throw new BindingNotSupportedException(
                $"Bound compiled emission not implemented for '{expr.GetType().Name}'")
        };
    }

    private static LinqExpression EmitLiteral(BoundLiteralExpr literal)
    {
        return LinqExpression.Constant(literal.Value, typeof(object));
    }

    private LinqExpression EmitIdentifier(BoundIdentifierExpr identifier)
    {
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
            AsObject(Emit(cast.Expression)),
            LinqExpression.Constant(cast.TargetType, typeof(Type)),
            cast.SourceStaticType == null
                ? LinqExpression.Constant(null, typeof(Type))
                : LinqExpression.Constant(cast.SourceStaticType, typeof(Type)),
            LinqExpression.Constant(false));
    }

    private LinqExpression EmitAs(BoundAsExpr asExpr)
    {
        return LinqExpression.Call(
            TryAsMethod,
            AsObject(Emit(asExpr.Expression)),
            LinqExpression.Constant(asExpr.TargetType, typeof(Type)));
    }

    private LinqExpression EmitUnary(BoundUnaryExpr unary)
    {
        var operand = AsObject(Emit(unary.Operand));
        return unary.Operator switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand, LinqExpression.Constant(false)),
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

        var left = AsObject(Emit(binary.Left));
        var right = AsObject(Emit(binary.Right));

        return binary.Operator switch
        {
            TokenType.Plus => LinqExpression.Call(AddMethod, left, right, _optionsParam),
            TokenType.Minus => LinqExpression.Call(SubtractMethod, left, right, LinqExpression.Constant(false)),
            TokenType.Star => LinqExpression.Call(MultiplyMethod, left, right, _optionsParam, LinqExpression.Constant(false)),
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
            _ => throw new BindingNotSupportedException($"Unsupported bound binary operator '{binary.Operator}'")
        };
    }

    private bool TryEmitPrimitiveBinaryFastPath(BoundBinaryExpr binary, out LinqExpression direct)
    {
        direct = null!;
        var leftType = binary.Left.StaticType;
        var rightType = binary.Right.StaticType;
        if (leftType != rightType || !IsPrimitiveBinaryFastPathType(leftType))
            return false;

        var left = EnsureTypedExpression(Emit(binary.Left), leftType);
        var right = EnsureTypedExpression(Emit(binary.Right), rightType);

        LinqExpression? typed = binary.Operator switch
        {
            TokenType.Plus => LinqExpression.Add(left, right),
            TokenType.Minus => LinqExpression.Subtract(left, right),
            TokenType.Star => LinqExpression.Multiply(left, right),
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

        direct = LinqExpression.Convert(typed, typeof(object));
        return true;
    }

    private static bool IsPrimitiveBinaryFastPathType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private LinqExpression EmitLogical(BoundLogicalExpr logical)
    {
        var opLexeme = TokenLexemes.GetCanonical(logical.Operator);
        var leftBool = LinqExpression.Call(
            RequireBooleanForLogicalOperatorMethod,
            AsObject(Emit(logical.Left)),
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(GetBoundTypeName(logical.Right)));
        var rightBoolAsObject = LinqExpression.Convert(
            LinqExpression.Call(
                RequireBooleanForLogicalOperatorMethod,
                AsObject(Emit(logical.Right)),
                LinqExpression.Constant(opLexeme),
                LinqExpression.Constant(GetBoundTypeName(logical.Left))),
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
            LinqExpression.Assign(leftVar, AsObject(Emit(nullCoalesce.Left))),
            LinqExpression.Condition(
                LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(object))),
                AsObject(Emit(nullCoalesce.Right)),
                leftVar));
    }

    private LinqExpression EmitConditional(BoundConditionalExpr conditional)
    {
        var condition = LinqExpression.Call(RequireBooleanMethod, AsObject(Emit(conditional.Condition)));
        var thenExpr = AsObject(Emit(conditional.ThenBranch));
        var elseExpr = AsObject(Emit(conditional.ElseBranch));

        var result = LinqExpression.Condition(condition, thenExpr, elseExpr);
        var thenType = conditional.ThenBranch.StaticType;
        var elseType = conditional.ElseBranch.StaticType;
        if (thenType != typeof(object) &&
            elseType != typeof(object) &&
            thenType != elseType &&
            TypeHelpers.IsArithmetic(thenType) &&
            TypeHelpers.IsArithmetic(elseType))
        {
            var resultType = NumericDispatch.GetResultType(thenType, elseType);
            return LinqExpression.Call(
                PromoteToTypeMethod,
                result,
                LinqExpression.Constant(resultType, typeof(Type)));
        }

        return result;
    }

    private LinqExpression EmitMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        if (memberAccess.Plan?.Member is PropertyInfo property)
            return EmitDirectPropertyAccess(memberAccess, property);

        if (memberAccess.Plan?.Member is FieldInfo field)
            return EmitDirectFieldAccess(memberAccess, field);

        var target = AsObject(Emit(memberAccess.Target));
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

        var targetExpr = AsObject(Emit(indexAccess.Target));
        var indexExpr = AsObject(Emit(indexAccess.Index));

        if (!indexAccess.NullSafe)
        {
            return LinqExpression.Call(
                GetIndexMethod,
                targetExpr,
                indexExpr,
                _optionsParam);
        }

        var targetVar = LinqExpression.Variable(typeof(object), "indexTarget");
        return LinqExpression.Block(
            typeof(object),
            [targetVar],
            LinqExpression.Assign(targetVar, targetExpr),
            LinqExpression.Condition(
                LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Constant(null, typeof(object)),
                LinqExpression.Call(GetIndexMethod, targetVar, indexExpr, _optionsParam)));
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
            var guarded = WrapGuardedValue(access, property.PropertyType, CreateMemberGuardContext(memberAccess.MemberName));
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
        var typedTarget = EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Property(typedTarget, property);
        var guardedExpr = LinqExpression.Convert(
            WrapGuardedValue(accessExpr, property.PropertyType, CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
            guardedExpr);
    }

    private LinqExpression EmitDirectFieldAccess(BoundMemberAccessExpr memberAccess, FieldInfo field)
    {
        var plan = memberAccess.Plan!;
        var guardCheck = EmitMemberReadGuard(memberAccess, isField: true);

        if (plan.IsStatic)
        {
            var access = LinqExpression.Field(null, field);
            var guarded = WrapGuardedValue(access, field.FieldType, CreateMemberGuardContext(memberAccess.MemberName));
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
        var typedTarget = EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = LinqExpression.Field(typedTarget, field);
        var guardedExpr = LinqExpression.Convert(
            WrapGuardedValue(accessExpr, field.FieldType, CreateMemberGuardContext(memberAccess.MemberName)),
            typeof(object));

        if (memberAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
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
            AsObject(Emit(indexAccess.Target)),
            AsObject(Emit(indexAccess.Index)),
            _optionsParam);
    }

    private LinqExpression EmitDirectStringIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "indexTarget");
        var typedTarget = EnsureTypedExpression(
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
                LinqExpression.Assign(targetObjVar, AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    valueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, AsObject(Emit(indexAccess.Target))),
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

        if (TryGetIntIndexer(plan.TargetType, out var indexer) &&
            TryGetCountProperty(plan.TargetType, out var countProperty))
        {
            typedTarget = EnsureTypedExpression(checkedTarget, plan.TargetType);
            countExpr = LinqExpression.Property(typedTarget, countProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, indexer, indexExpr);
            valueType = indexer.PropertyType;
        }
        else
        {
            typedTarget = EnsureTypedExpression(checkedTarget, typeof(IList));
            countExpr = LinqExpression.Property(
                EnsureTypedExpression(typedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            var indexExpr = BuildNormalizedIntIndex(indexAccess, countExpr);
            valueExpr = LinqExpression.Property(typedTarget, IListIndexerProperty, indexExpr);
            valueType = typeof(object);
        }

        var guardedValueExpr = LinqExpression.Convert(
            WrapGuardedValue(valueExpr, valueType, "index access"),
            typeof(object));

        if (indexAccess.NullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, AsObject(Emit(indexAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedValueExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, AsObject(Emit(indexAccess.Target))),
            guardedValueExpr);
    }

    private LinqExpression BuildNormalizedIntIndex(BoundIndexAccessExpr indexAccess, LinqExpression lengthExpression)
    {
        if (indexAccess.Index is BoundLiteralExpr { Value: int literalIndex } && literalIndex >= 0)
            return LinqExpression.Constant(literalIndex, typeof(int));

        var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, AsObject(Emit(indexAccess.Index)));
        var languageMode = LinqExpression.Property(_optionsParam, nameof(CsEvalOptions.LanguageMode));
        return LinqExpression.Call(NormalizeIndexMethod, rawIndex, lengthExpression, languageMode);
    }

    private LinqExpression EmitDirectPlannedCall(BoundCallExpr call, BoundMemberAccessExpr memberAccess)
    {
        if (!CanEmitDirectMethodCall(call.Plan.SelectedMethod, call.Arguments.Length))
            return EmitInvokeCore(call.Callee, call.Arguments, ImmutableArray<string>.Empty);

        var method = call.Plan.SelectedMethod;
        var guardCheck = EmitMethodCallGuard(method, call.Plan.IsStaticCall, call.Plan.IsModuleCall);
        var args = EmitConvertedCallArguments(call.Arguments, call.Plan.ArgumentConversions);

        if (call.Plan.IsStaticCall)
        {
            var staticCall = LinqExpression.Call(method, args);
            if (method.ReturnType == typeof(void))
                return LinqExpression.Block(guardCheck, staticCall, LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Block(
                method.ReturnType,
                guardCheck,
                WrapGuardedValue(staticCall, method.ReturnType, CreateMethodGuardContext(method.Name)));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "callTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureCallTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(method.Name));
        var typedTarget = EnsureTypedExpression(checkedTarget, method.DeclaringType ?? memberAccess.Plan!.DeclaringType);
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
                        WrapGuardedValue(instanceCall, method.ReturnType, CreateMethodGuardContext(method.Name))),
                    typeof(object));

            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    nullSafeBody));
        }

        if (method.ReturnType == typeof(void))
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                guardCheck,
                LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
                instanceCall,
                LinqExpression.Constant(null, typeof(object)));
        }

        return LinqExpression.Block(
            method.ReturnType,
            [targetObjVar],
            guardCheck,
            LinqExpression.Assign(targetObjVar, AsObject(Emit(memberAccess.Target))),
            WrapGuardedValue(instanceCall, method.ReturnType, CreateMethodGuardContext(method.Name)));
    }

    private static bool CanEmitDirectMethodCall(MethodInfo method, int argumentCount)
    {
        if (method.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(method);
        if (parameters.Length != argumentCount)
            return false;

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef ||
                parameter.IsDefined(typeof(ParamArrayAttribute), false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetIntIndexer(Type targetType, out PropertyInfo indexer)
    {
        foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!string.Equals(property.Name, "Item", StringComparison.Ordinal))
                continue;

            var parameters = property.GetIndexParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                indexer = property;
                return true;
            }
        }

        indexer = null!;
        return false;
    }

    private static bool TryGetCountProperty(Type targetType, out PropertyInfo countProperty)
    {
        foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!string.Equals(property.Name, "Count", StringComparison.Ordinal))
                continue;
            if (property.GetIndexParameters().Length != 0 || property.PropertyType != typeof(int))
                continue;

            countProperty = property;
            return true;
        }

        countProperty = null!;
        return false;
    }

    private LinqExpression[] EmitConvertedCallArguments(
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<BoundConversionPlan> conversions)
    {
        var emitted = new LinqExpression[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            var conversion = i < conversions.Length
                ? conversions[i]
                : new BoundConversionPlan(typeof(object), typeof(object), IsIdentity: true);
            emitted[i] = EmitCallArgument(arguments[i], conversion.TargetType);
        }
        return emitted;
    }

    private LinqExpression EmitCallArgument(BoundExpr argument, Type targetType)
    {
        var emittedArgument = Emit(argument);
        if (targetType == typeof(object))
            return AsObject(emittedArgument);

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        if (emittedArgument.Type == typeof(object))
        {
            var coerced = LinqExpression.Call(
                CompilerContext.CoerceNumericMethod,
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
                AsObject(Emit(pipeline.Left)),
                LinqExpression.Constant(rightIdentifier.Name),
                _contextParam,
                _optionsParam,
                _ctParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            AsObject(Emit(pipeline.Left)),
            AsObject(Emit(pipeline.Right)),
            _contextParam,
            _optionsParam,
            _ctParam);
    }

    private LinqExpression EmitInvokeCore(
        BoundExpr callee,
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<string> typeArguments)
    {
        var args = LinqExpression.NewArrayInit(typeof(object), arguments.Select(Emit));
        var emittedTypeArguments = EmitTypeArguments(typeArguments);

        if (callee is BoundIdentifierExpr identifier)
        {
            return LinqExpression.Call(
                InvokeIdentifierCallMethod,
                LinqExpression.Constant(identifier.Name),
                args,
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }

        if (callee is BoundMemberAccessExpr memberAccess)
        {
            return LinqExpression.Call(
                InvokeMemberCallMethod,
                AsObject(Emit(memberAccess.Target)),
                LinqExpression.Constant(memberAccess.MemberName),
                args,
                LinqExpression.Constant(memberAccess.NullSafe),
                _contextParam,
                _optionsParam,
                _ctParam,
                emittedTypeArguments);
        }

        return LinqExpression.Call(
            InvokeCallMethod,
            AsObject(Emit(callee)),
            args,
            _contextParam,
            _optionsParam,
            _ctParam,
            emittedTypeArguments);
    }

    private static LinqExpression EmitTypeArguments(ImmutableArray<string> typeArguments)
    {
        if (typeArguments.IsDefaultOrEmpty)
            return LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        return LinqExpression.Constant(typeArguments.ToArray(), typeof(IReadOnlyList<string>));
    }

    private static LinqExpression AsObject(LinqExpression expression)
    {
        return expression.Type == typeof(object)
            ? expression
            : LinqExpression.Convert(expression, typeof(object));
    }

    private static LinqExpression EnsureTypedExpression(LinqExpression expression, Type targetType)
    {
        return expression.Type == targetType
            ? expression
            : LinqExpression.Convert(expression, targetType);
    }

    private static LinqExpression WrapGuardedValue(
        LinqExpression value,
        Type valueType,
        string context)
    {
        if (!TypeHelpers.RequiresReflectionLeakGuard(valueType))
            return value;

        return LinqExpression.Call(
            CompilerContext.GetGuardReflectionLeakTypedMethod(valueType),
            value,
            LinqExpression.Constant(context));
    }

    private static string CreateMemberGuardContext(string memberName) => $"bound member access {memberName}";
    private static string CreateMethodGuardContext(string methodName) => $"bound method call {methodName}";

    private static string GetBoundTypeName(BoundExpr expr)
    {
        if (expr is BoundLiteralExpr { Value: null })
            return TypeNameFormatter.Null;

        if (expr is BoundLiteralExpr { Value: { } value })
            return value.GetType().Name;

        return expr.StaticType == typeof(object)
            ? "unknown"
            : expr.StaticType.Name;
    }
}
