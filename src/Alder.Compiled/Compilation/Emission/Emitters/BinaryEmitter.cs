using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class BinaryEmitter : INodeEmitter<BoundBinaryExpr>
{
    public Expression Emit(BoundBinaryExpr node, EmissionContext ctx)
    {
        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = ctx.Emit(leftmost);
        for (var i = chain.Count - 1; i >= 0; i--)
            result = EmitBinaryFold(chain[i], result, ctx);
        return result;
    }

    private static Expression EmitBinaryFold(BoundBinaryExpr binary, Expression left, EmissionContext ctx)
    {
        if (TryEmitPrimitiveFastPath(binary, left, ctx, out var direct))
            return direct;

        if (TryEmitStringConcatFastPath(binary, left, ctx, out var stringDirect))
            return stringDirect;

        if (ShouldApplyConstantPromotion(binary))
            return EmitWithConstantPromotion(binary, left, ctx);

        var isStringContext = binary.Operator == TokenType.Plus &&
            (binary.Left.StaticType.ClrType == typeof(string) || binary.Right.StaticType.ClrType == typeof(string));

        return EmitCore(binary.Operator, EmitHelpers.AsObject(left), EmitHelpers.AsObject(ctx.Emit(binary.Right)), ctx, isStringContext);
    }

    private static bool TryEmitPrimitiveFastPath(BoundBinaryExpr binary, Expression preEmittedLeft, EmissionContext ctx, out Expression direct)
    {
        direct = null!;
        if (binary.PromotedType is not { } promotedType)
            return false;

        var isShift = binary.Operator is TokenType.LessLess or TokenType.GreaterGreater;
        var left = EmitHelpers.EnsureTypedExpression(preEmittedLeft, binary.Left.StaticType.ClrType);
        var right = EmitHelpers.EnsureTypedExpression(ctx.Emit(binary.Right), binary.Right.StaticType.ClrType);
        if (left.Type != promotedType)
            left = LinqExpression.Convert(left, promotedType);
        var rightTarget = isShift ? typeof(int) : promotedType;
        if (right.Type != rightTarget)
            right = LinqExpression.Convert(right, rightTarget);

        Expression? typed = binary.Operator switch
        {
            TokenType.Plus => ctx.IsChecked ? LinqExpression.AddChecked(left, right) : LinqExpression.Add(left, right),
            TokenType.Minus => ctx.IsChecked ? LinqExpression.SubtractChecked(left, right) : LinqExpression.Subtract(left, right),
            TokenType.Star => ctx.IsChecked ? LinqExpression.MultiplyChecked(left, right) : LinqExpression.Multiply(left, right),
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
            return false;

        direct = typed;
        return true;
    }

    private static bool TryEmitStringConcatFastPath(BoundBinaryExpr binary, Expression preEmittedLeft, EmissionContext ctx, out Expression result)
    {
        result = null!;
        if (binary.Operator != TokenType.Plus)
            return false;

        var leftIsString = binary.Left.StaticType.ClrType == typeof(string);
        var rightIsString = binary.Right.StaticType.ClrType == typeof(string);
        if (!leftIsString && !rightIsString)
            return false;

        var left = leftIsString
            ? EmitHelpers.EnsureTypedExpression(preEmittedLeft, typeof(string))
            : EmitHelpers.ToStringExpression(preEmittedLeft);
        var right = rightIsString
            ? EmitHelpers.EnsureTypedExpression(ctx.Emit(binary.Right), typeof(string))
            : EmitHelpers.ToStringExpression(ctx.Emit(binary.Right));

        result = LinqExpression.Call(StringConcatTwoStringsMethod, left, right);
        return true;
    }

    private static bool ShouldApplyConstantPromotion(BoundBinaryExpr binary)
    {
        if (binary.Operator is not (TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or TokenType.Percent))
            return false;
        return binary.Left is BoundLiteralExpr || binary.Right is BoundLiteralExpr;
    }

    private static Expression EmitWithConstantPromotion(BoundBinaryExpr binary, Expression preEmittedLeft, EmissionContext ctx)
    {
        var leftVar = LinqExpression.Variable(typeof(object), "binaryLeft");
        var rightVar = LinqExpression.Variable(typeof(object), "binaryRight");
        var promotedVar = LinqExpression.Variable(typeof(ValueTuple<object?, object?>), "binaryPromoted");

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar, promotedVar],
            LinqExpression.Assign(leftVar, EmitHelpers.AsObject(preEmittedLeft)),
            LinqExpression.Assign(rightVar, EmitHelpers.AsObject(ctx.Emit(binary.Right))),
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
            EmitCore(binary.Operator, leftVar, rightVar, ctx));
    }

    private static MethodCallExpression EmitCore(TokenType op, Expression left, Expression right, EmissionContext ctx, bool isStringContext = false)
    {
        left = EmitHelpers.AsObject(left);
        right = EmitHelpers.AsObject(right);

        return op switch
        {
            TokenType.Plus => LinqExpression.Call(AddMethod, left, right, ctx.ConfigParam, ctx.ContextParam, LinqExpression.Constant(ctx.IsChecked), LinqExpression.Constant(isStringContext)),
            TokenType.Minus => LinqExpression.Call(SubtractMethod, left, right, LinqExpression.Constant(ctx.IsChecked)),
            TokenType.Star => LinqExpression.Call(MultiplyMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.LanguageMode)), LinqExpression.Constant(ctx.IsChecked)),
            TokenType.Slash => LinqExpression.Call(DivideMethod, left, right),
            TokenType.Percent => LinqExpression.Call(ModuloMethod, left, right),
            TokenType.EqualEqual => LinqExpression.Call(EqualsMethod, left, right),
            TokenType.BangEqual => LinqExpression.Call(NotEqualsMethod, left, right),
            TokenType.EqualEqualEqual => LinqExpression.Call(StrictEqualsMethod, left, right),
            TokenType.BangEqualEqual => LinqExpression.Call(StrictNotEqualsMethod, left, right),
            TokenType.Less => LinqExpression.Call(LessThanMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison))),
            TokenType.LessEqual => LinqExpression.Call(LessThanOrEqualMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison))),
            TokenType.Greater => LinqExpression.Call(GreaterThanMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison))),
            TokenType.GreaterEqual => LinqExpression.Call(GreaterThanOrEqualMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison))),
            TokenType.Amp => LinqExpression.Call(BitwiseAndMethod, left, right),
            TokenType.Pipe => LinqExpression.Call(BitwiseOrMethod, left, right),
            TokenType.Caret => LinqExpression.Call(BitwiseXorMethod, left, right),
            TokenType.LessLess => LinqExpression.Call(LeftShiftMethod, left, right),
            TokenType.GreaterGreater => LinqExpression.Call(RightShiftMethod, left, right),
            TokenType.GreaterGreaterGreater => LinqExpression.Call(UnsignedRightShiftMethod, left, right),
            TokenType.StarStar => LinqExpression.Call(PowerMethod, left, right),
            TokenType.In => LinqExpression.Call(InOperatorMethod, left, right),
            TokenType.Like => LinqExpression.Call(LikeMethod, left, right, LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.StringComparison))),
            TokenType.EqualTilde => LinqExpression.Call(RegexMatchMethod, left, right),
            TokenType.BangTilde => LinqExpression.Call(RegexNotMatchMethod, left, right),
            TokenType.LessEqualGreater => LinqExpression.Call(SpaceshipMethod, left, right),
            _ => throw new BindingNotSupportedException($"Unsupported bound binary operator '{op}'")
        };
    }
}
