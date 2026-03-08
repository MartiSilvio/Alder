using System.Reflection;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal sealed partial class DirectEmitCompilerUnit
{
    private static readonly MethodInfo InOperatorMethod =
        typeof(Operators).GetMethod(nameof(Operators.InOperator), [typeof(object), typeof(object)])!;
    private static readonly MethodInfo LikeMethod =
        typeof(Operators).GetMethod(nameof(Operators.Like), [typeof(object), typeof(object)])!;
    private static readonly MethodInfo StringStartsWithOrdinalMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringEndsWithOrdinalMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringContainsOrdinalMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringEqualsOrdinalMethod =
        typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;

    internal LinqExpression? TryEmitDirectBinary(BinaryExpr b)
    {
        var op = b.Op.Type;

        var (leftExpr, leftType) = CompileTyped(b.Left);
        var (rightExpr, rightType) = CompileTyped(b.Right);

        if (TryEmitDirectComparableEquality(op, leftExpr, leftType, rightExpr, rightType) is { } equalityDirect)
            return equalityDirect;

        if (TryEmitDirectContainsBinary(op, leftExpr, leftType, rightExpr, rightType) is { } containsDirect)
            return containsDirect;
        if (TryEmitDirectLikeBinary(b, op, leftExpr, leftType) is { } likeDirect)
            return likeDirect;

        // ECMA-334 §10.2.11 constant expression promotions (e.g., uint + 1).
        // Keep direct codegen active by applying the promotion eagerly for literal operands.
        if (b is { Left: LiteralExpr { Value: not null } leftLiteral, Right: LiteralExpr { Value: not null } rightLiteral })
        {
            var promoted = NumericDispatch.TryConstantPromotion(
                leftLiteral.Value, leftLiteral.IsConstant,
                rightLiteral.Value, rightLiteral.IsConstant);
            if (promoted != null)
            {
                leftType = promoted.Value.Left.GetType();
                rightType = promoted.Value.Right.GetType();
                leftExpr = LinqExpression.Constant(promoted.Value.Left, leftType);
                rightExpr = LinqExpression.Constant(promoted.Value.Right, rightType);
            }
        }

        // For variable + constant forms where ECMA constant promotion depends on runtime value
        // (e.g., uint + const-int), keep runtime dispatch to preserve exact semantics.
        var leftIsConst = b.Left is LiteralExpr { IsConstant: true };
        var rightIsConst = b.Right is LiteralExpr { IsConstant: true };
        if (leftIsConst ^ rightIsConst)
        {
            var constType = leftIsConst ? leftType : rightType;
            var otherType = leftIsConst ? rightType : leftType;
            var mayNeedRuntimeConstantPromotion =
                (otherType == typeof(uint) && constType == typeof(int)) ||
                (otherType == typeof(ulong) && (constType == typeof(int) || constType == typeof(long)));
            if (mayNeedRuntimeConstantPromotion)
                return null;
        }

        // Both must be known numeric types
        if (!IsDirectableNumericType(leftType) || !IsDirectableNumericType(rightType))
            return null;

        // decimal + float/double has no implicit conversion in IL
        if (leftType == typeof(decimal) && (rightType == typeof(double) || rightType == typeof(float)))
            return null;
        if (rightType == typeof(decimal) && (leftType == typeof(double) || leftType == typeof(float)))
            return null;

        // Bitwise/shift ops require integer operands
        bool isBitwiseOrShift = op is TokenType.Amp or TokenType.Pipe or TokenType.Caret
            or TokenType.LessLess or TokenType.GreaterGreater;
        if (isBitwiseOrShift && (!IsIntegerType(leftType) || !IsIntegerType(rightType)))
            return null;

        // Shift ops: right operand must be int per ECMA-334
        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            if (rightType != typeof(int))
                return null;
        }

        var resultType = NumericDispatch.GetResultType(leftType, rightType);

        // Ensure expressions have their concrete types.
        // For object-typed operands, apply runtime numeric coercion before cast so
        // compiled delegates remain stable when variable numeric runtime types change.
        var typedLeft = leftExpr.Type == leftType
            ? leftExpr
            : leftExpr.Type == typeof(object)
                ? LinqExpression.Convert(
                    LinqExpression.Call(
                        CompilerContext.CoerceNumericMethod,
                        leftExpr,
                        LinqExpression.Constant(leftType, typeof(Type))),
                    leftType)
                : leftExpr;
        var typedRight = rightExpr.Type == rightType
            ? rightExpr
            : rightExpr.Type == typeof(object)
                ? LinqExpression.Convert(
                    LinqExpression.Call(
                        CompilerContext.CoerceNumericMethod,
                        rightExpr,
                        LinqExpression.Constant(rightType, typeof(Type))),
                    rightType)
                : rightExpr;

        // Promote to result type if needed (e.g., int + double → both become double)
        if (typedLeft.Type != resultType)
            typedLeft = LinqExpression.Convert(typedLeft, resultType);
        if (typedRight.Type != resultType)
            typedRight = LinqExpression.Convert(typedRight, resultType);

        // Shift ops: right operand stays int regardless of result type
        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            typedRight = rightExpr.Type == typeof(int)
                ? rightExpr
                : rightExpr.Type == typeof(object)
                    ? LinqExpression.Convert(
                        LinqExpression.Call(
                            CompilerContext.CoerceNumericMethod,
                            rightExpr,
                            LinqExpression.Constant(typeof(int), typeof(Type))),
                        typeof(int))
                    : LinqExpression.Convert(rightExpr, typeof(int));
        }

        var isChecked = _ctx.IsChecked;
        LinqExpression? result = op switch
        {
            TokenType.Plus => isChecked ? LinqExpression.AddChecked(typedLeft, typedRight) : LinqExpression.Add(typedLeft, typedRight),
            TokenType.Minus => isChecked ? LinqExpression.SubtractChecked(typedLeft, typedRight) : LinqExpression.Subtract(typedLeft, typedRight),
            TokenType.Star => isChecked ? LinqExpression.MultiplyChecked(typedLeft, typedRight) : LinqExpression.Multiply(typedLeft, typedRight),
            TokenType.Slash => LinqExpression.Divide(typedLeft, typedRight),
            TokenType.Percent => LinqExpression.Modulo(typedLeft, typedRight),
            TokenType.Less => LinqExpression.LessThan(typedLeft, typedRight),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(typedLeft, typedRight),
            TokenType.Greater => LinqExpression.GreaterThan(typedLeft, typedRight),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(typedLeft, typedRight),
            TokenType.EqualEqual => LinqExpression.Equal(typedLeft, typedRight),
            TokenType.BangEqual => LinqExpression.NotEqual(typedLeft, typedRight),
            TokenType.LessEqualGreater => CreateSpaceshipNumericResult(typedLeft, typedRight),
            TokenType.Amp => LinqExpression.And(typedLeft, typedRight),
            TokenType.Pipe => LinqExpression.Or(typedLeft, typedRight),
            TokenType.Caret => LinqExpression.ExclusiveOr(typedLeft, typedRight),
            TokenType.LessLess => LinqExpression.LeftShift(typedLeft, typedRight),
            TokenType.GreaterGreater => LinqExpression.RightShift(typedLeft, typedRight),
            _ => null
        };

        if (result == null) return null;

        return LinqExpression.Convert(result, typeof(object));
    }

    private static LinqExpression? TryEmitDirectComparableEquality(
        TokenType op,
        LinqExpression leftExpr,
        Type leftType,
        LinqExpression rightExpr,
        Type rightType)
    {
        if (op is not TokenType.EqualEqual and not TokenType.BangEqual)
            return null;

        if (leftType == typeof(object) || rightType == typeof(object))
            return null;

        if (leftType != rightType || !IsDirectEqualityComparableType(leftType))
            return null;

        var typedLeft = EnsureTypedExpression(leftExpr, leftType);
        var typedRight = EnsureTypedExpression(rightExpr, rightType);

        LinqExpression result = op == TokenType.EqualEqual
            ? LinqExpression.Equal(typedLeft, typedRight)
            : LinqExpression.NotEqual(typedLeft, typedRight);

        return LinqExpression.Convert(result, typeof(object));
    }

    private static bool IsDirectEqualityComparableType(Type type) =>
        type == typeof(bool) ||
        type == typeof(string) ||
        type == typeof(char) ||
        TypeHelpers.IsNumeric(type) ||
        type.IsEnum;

    private LinqExpression? TryEmitDirectContainsBinary(
        TokenType op,
        LinqExpression leftExpr,
        Type leftType,
        LinqExpression rightExpr,
        Type rightType)
    {
        if (op is not TokenType.In and not TokenType.NotIn)
            return null;

        if (leftType == typeof(object) || rightType == typeof(object))
            return null;

        var containsMethod = FindContainsMethod(rightType, leftType);
        if (containsMethod == null)
            return null;

        var parameterType = containsMethod.GetParameters()[0].ParameterType;
        if (!CanDirectlyBindContainsParameter(parameterType, leftType))
            return null;

        var typedLeft = EnsureTypedExpression(leftExpr, leftType);
        var typedRight = EnsureTypedExpression(rightExpr, rightType);

        var leftVar = LinqExpression.Variable(leftType, "containsLeft");
        var rightVar = LinqExpression.Variable(rightType, "containsRight");

        var containsArg = ConvertExpressionForParameter(leftVar, parameterType);
        var containsCall = LinqExpression.Call(rightVar, containsMethod, containsArg);
        LinqExpression boolResult = op == TokenType.NotIn
            ? LinqExpression.Not(containsCall)
            : containsCall;

        // Preserve "in" null-collection diagnostics: null collection is not a NullReferenceException.
        var nullFallback = LinqExpression.Call(
            InOperatorMethod,
            EnsureObjectExpression(leftVar),
            LinqExpression.Constant(null, typeof(object)));

        var guardedResult = LinqExpression.Condition(
            LinqExpression.Equal(
                LinqExpression.Convert(rightVar, typeof(object)),
                LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Convert(nullFallback, typeof(bool)),
            boolResult);

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar],
            LinqExpression.Assign(leftVar, typedLeft),
            LinqExpression.Assign(rightVar, typedRight),
            LinqExpression.Convert(guardedResult, typeof(object)));
    }

    private LinqExpression? TryEmitDirectLikeBinary(
        BinaryExpr binary,
        TokenType op,
        LinqExpression leftExpr,
        Type leftType)
    {
        if (op is not TokenType.Like and not TokenType.NotLike)
            return null;

        if (leftType != typeof(string))
            return null;

        if (binary.Right is not LiteralExpr { Value: string pattern })
            return null;

        var patternMode = Operators.ClassifyLikePattern(pattern);
        if (patternMode == Operators.LikePatternMode.General)
            return null;

        var typedLeft = EnsureTypedExpression(leftExpr, typeof(string));
        var leftVar = LinqExpression.Variable(typeof(string), "likeLeft");

        var patternConstant = LinqExpression.Constant(pattern, typeof(string));
        var comparisonConstant = LinqExpression.Constant(StringComparison.Ordinal);

        LinqExpression optimized = patternMode switch
        {
            Operators.LikePatternMode.Exact => LinqExpression.Call(
                StringEqualsOrdinalMethod,
                leftVar,
                patternConstant,
                comparisonConstant),
            Operators.LikePatternMode.Prefix => LinqExpression.Call(
                leftVar,
                StringStartsWithOrdinalMethod,
                LinqExpression.Constant(pattern[..^1], typeof(string)),
                comparisonConstant),
            Operators.LikePatternMode.Suffix => LinqExpression.Call(
                leftVar,
                StringEndsWithOrdinalMethod,
                LinqExpression.Constant(pattern[1..], typeof(string)),
                comparisonConstant),
            Operators.LikePatternMode.Contains => LinqExpression.Call(
                leftVar,
                StringContainsOrdinalMethod,
                LinqExpression.Constant(pattern[1..^1], typeof(string)),
                comparisonConstant),
            _ => throw new InvalidOperationException($"Unexpected LIKE mode: {patternMode}")
        };

        // Preserve Like() operand validation semantics for null LHS.
        var nullFallback = LinqExpression.Call(
            LikeMethod,
            EnsureObjectExpression(leftVar),
            LinqExpression.Constant((object)pattern, typeof(object)));

        LinqExpression guarded = LinqExpression.Condition(
            LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(string))),
            LinqExpression.Convert(nullFallback, typeof(bool)),
            optimized);

        if (op == TokenType.NotLike)
            guarded = LinqExpression.Not(guarded);

        return LinqExpression.Block(
            typeof(object),
            [leftVar],
            LinqExpression.Assign(leftVar, typedLeft),
            LinqExpression.Convert(guarded, typeof(object)));
    }

    internal bool TryCompileDirectNumericChainedComparison(
        ChainedComparisonExpr expr,
        out LinqExpression compiled)
    {
        compiled = null!;
        if (expr.Operands.Count < 2 || expr.Operators.Count != expr.Operands.Count - 1)
            return false;

        Type? operandType = null;
        var typedOperands = new List<LinqExpression>(expr.Operands.Count);

        foreach (var operand in expr.Operands)
        {
            var (typedExpression, knownType) = CompileTyped(operand);
            if (!IsDirectableNumericType(knownType))
                return false;

            operandType ??= knownType;
            if (knownType != operandType)
                return false;

            typedOperands.Add(ConvertToNumericType(typedExpression, knownType, operandType));
        }

        if (operandType == null)
            return false;

        var variables = new List<System.Linq.Expressions.ParameterExpression>(typedOperands.Count);
        var body = new List<LinqExpression>(typedOperands.Count + 1);

        for (var i = 0; i < typedOperands.Count; i++)
        {
            var local = LinqExpression.Variable(operandType, $"v{i}");
            variables.Add(local);
            body.Add(LinqExpression.Assign(local, typedOperands[i]));
        }

        LinqExpression? chain = null;
        for (var i = 0; i < expr.Operators.Count; i++)
        {
            var comparison = CreateNumericComparison(expr.Operators[i].Type, variables[i], variables[i + 1]);
            if (comparison == null)
                return false;

            chain = chain == null ? comparison : LinqExpression.AndAlso(chain, comparison);
        }

        if (chain == null)
            return false;

        body.Add(LinqExpression.Convert(chain, typeof(object)));
        compiled = LinqExpression.Block(typeof(object), variables, body);
        return true;
    }

    private static LinqExpression ConvertToNumericType(LinqExpression expression, Type sourceType, Type targetType)
    {
        LinqExpression converted = expression;
        if (converted.Type == typeof(object))
        {
            converted = LinqExpression.Convert(
                LinqExpression.Call(
                    CompilerContext.CoerceNumericMethod,
                    converted,
                    LinqExpression.Constant(targetType, typeof(Type))),
                targetType);
            return converted;
        }

        if (converted.Type != sourceType)
            converted = LinqExpression.Convert(converted, sourceType);

        if (sourceType != targetType)
            converted = LinqExpression.Convert(converted, targetType);

        return converted;
    }

    private static LinqExpression? CreateNumericComparison(TokenType opType, LinqExpression left, LinqExpression right)
    {
        return opType switch
        {
            TokenType.Less => LinqExpression.LessThan(left, right),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(left, right),
            TokenType.Greater => LinqExpression.GreaterThan(left, right),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(left, right),
            TokenType.EqualEqual => LinqExpression.Equal(left, right),
            TokenType.BangEqual => LinqExpression.NotEqual(left, right),
            _ => null
        };
    }

    private static LinqExpression CreateSpaceshipNumericResult(LinqExpression left, LinqExpression right)
    {
        var compareTo = left.Type.GetMethod(nameof(int.CompareTo), [right.Type]);
        if (compareTo != null && compareTo.ReturnType == typeof(int))
            return LinqExpression.Call(left, compareTo, right);

        return LinqExpression.Condition(
            LinqExpression.LessThan(left, right),
            LinqExpression.Constant(-1),
            LinqExpression.Condition(
                LinqExpression.GreaterThan(left, right),
                LinqExpression.Constant(1),
                LinqExpression.Constant(0)));
    }

    private static bool IsDirectableNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(short) || type == typeof(ushort) || type == typeof(byte) ||
        type == typeof(sbyte) || type == typeof(decimal) || type == typeof(char);

    private static bool IsIntegerType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(uint) ||
        type == typeof(ulong) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(char);

    private static MethodInfo? FindContainsMethod(Type collectionType, Type valueType)
    {
        if (collectionType == typeof(string))
        {
            if (valueType == typeof(string))
                return typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
            if (valueType == typeof(char))
                return typeof(string).GetMethod(nameof(string.Contains), [typeof(char)]);
            return null;
        }

        var candidates = collectionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == nameof(List<int>.Contains) && m.ReturnType == typeof(bool))
            .Select(m => (Method: m, Params: m.GetParameters()))
            .Where(x => x.Params.Length == 1 && CanDirectlyBindContainsParameter(x.Params[0].ParameterType, valueType))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var exact = candidates.FirstOrDefault(x => x.Params[0].ParameterType == valueType).Method;
        if (exact != null)
            return exact;

        var assignableReference = candidates.FirstOrDefault(x =>
                x.Params[0].ParameterType != typeof(object) &&
                !x.Params[0].ParameterType.IsValueType &&
                x.Params[0].ParameterType.IsAssignableFrom(valueType))
            .Method;
        if (assignableReference != null)
            return assignableReference;

        return candidates.FirstOrDefault(x => x.Params[0].ParameterType == typeof(object)).Method;
    }

    private static bool CanDirectlyBindContainsParameter(Type parameterType, Type valueType)
    {
        if (parameterType == valueType || parameterType == typeof(object))
            return true;

        return !parameterType.IsValueType && parameterType.IsAssignableFrom(valueType);
    }
}
