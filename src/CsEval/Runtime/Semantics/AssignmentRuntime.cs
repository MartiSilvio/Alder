using CsEval.Diagnostics;
using CsEval.Parsing;

namespace CsEval.Runtime;

internal static class AssignmentRuntime
{
    public static void CheckAllowIndexSet(CsEvalOptions options, object? index)
    {
        if (!options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");
    }

    public static object? ValidateVariableAssignment(string name, object? value, CsEvalContext context)
    {
        if (context.TryGetVariableType(name, out var variableType) && variableType != null && value != null)
        {
            try
            {
                return TypeHelpers.ValidateAssignment(variableType, value, name);
            }
            catch (CsEvalException ex) when (ex.ErrorCode == DiagnosticCode.CS0266)
            {
                var sourceType = value.GetType();
                var targetType = Nullable.GetUnderlyingType(variableType) ?? variableType;
                var sourceNumeric = TypeHelpers.IsArithmetic(sourceType) || sourceType == typeof(char);
                var targetNumeric = TypeHelpers.IsArithmetic(targetType) || targetType == typeof(char);
                if (!sourceNumeric || !targetNumeric)
                    throw;

                return TypeHelpers.RuntimeCast(value, sourceType, targetType);
            }
        }

        return value;
    }

    public static object? DefineVariable(
        string name,
        object? value,
        Type? declaredType,
        CsEvalContext context,
        bool isReadOnly = false)
    {
        if (declaredType != null)
            value = TypeHelpers.ValidateAndCoerceType(declaredType, value, name);

        var variableType = declaredType ?? value?.GetType() ?? typeof(object);
        context.DefineNew(name, value, variableType, isReadOnly);
        return value;
    }

    public static object GetNumericOne(object? value) => value switch
    {
        int => 1,
        long => 1L,
        double => 1.0,
        float => 1.0f,
        decimal => 1m,
        short => 1,
        byte => 1,
        sbyte => 1,
        ushort => 1,
        uint => 1u,
        ulong => 1ul,
        _ => 1
    };

    public static object? ApplyCompoundAssign(
        string name,
        TokenType compoundOperator,
        object? rightValue,
        CsEvalContext context,
        CsEvalOptions options,
        bool isChecked)
    {
        ExecutionRuntime.CheckAllowAssignment(options, $"{name} {TokenLexemes.GetCanonical(compoundOperator)} ...");
        var currentValue = context.Get(name);
        var result = ApplyBinaryOperator(ResolveCompoundBaseOperator(compoundOperator), currentValue, rightValue, options, context, isChecked);
        result = ValidateCompoundAssignment(name, result, rightValue, context);
        context.Set(name, result);
        return result;
    }

    public static object? ApplyIncrementDecrement(
        string name,
        bool isIncrement,
        bool isPrefix,
        CsEvalContext context,
        CsEvalOptions options,
        bool isChecked)
    {
        var incrementLexeme = TokenLexemes.GetCanonical(isIncrement ? TokenType.PlusPlus : TokenType.MinusMinus);
        ExecutionRuntime.CheckAllowAssignment(options, incrementLexeme + name);
        var currentValue = context.Get(name);
        var one = GetNumericOne(currentValue);
        var newValue = isIncrement
            ? Operators.Add(currentValue, one, options, context, isChecked)
            : Operators.Subtract(currentValue, one, isChecked);
        context.Set(name, newValue);
        return isPrefix ? newValue : currentValue;
    }

    public static object? ApplyMemberAssign(
        object? target,
        string memberName,
        object? value,
        CsEvalOptions options,
        CsEvalContext context)
    {
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, memberName);
        MemberAccess.SetMember(target, memberName, value, options, context);
        return value;
    }

    public static object? ApplyIndexAssign(
        object? target,
        object? index,
        object? value,
        CsEvalOptions options,
        CsEvalContext context)
    {
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        CheckAllowIndexSet(options, index);
        MemberAccess.SetIndex(target, index, value, options, context);
        return value;
    }

    public static object? ApplyMemberCompoundAssign(
        object? target,
        string memberName,
        TokenType compoundOperator,
        object? rightValue,
        CsEvalOptions options,
        CsEvalContext context,
        bool isChecked)
    {
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, memberName);
        var currentValue = MemberAccess.GetMember(target, memberName, options, nullSafe: false, context);
        var result = ApplyBinaryOperator(ResolveCompoundBaseOperator(compoundOperator), currentValue, rightValue, options, context, isChecked);
        MemberAccess.SetMember(target, memberName, result, options, context);
        return result;
    }

    public static object? ApplyIndexCompoundAssign(
        object? target,
        object? index,
        TokenType compoundOperator,
        object? rightValue,
        CsEvalOptions options,
        CsEvalContext context,
        bool isChecked)
    {
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var currentValue = MemberAccess.GetIndex(target, index, options, context);
        var result = ApplyBinaryOperator(ResolveCompoundBaseOperator(compoundOperator), currentValue, rightValue, options, context, isChecked);
        CheckAllowIndexSet(options, index);
        MemberAccess.SetIndex(target, index, result, options, context);
        return result;
    }

    public static object? ApplyMemberNullCoalesceAssign(
        object? target,
        string memberName,
        object? value,
        CsEvalOptions options,
        CsEvalContext context)
    {
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, memberName);
        var currentValue = MemberAccess.GetMember(target, memberName, options, nullSafe: false, context);
        if (currentValue != null)
            return currentValue;
        MemberAccess.SetMember(target, memberName, value, options, context);
        return value;
    }

    public static object? ApplyIndexNullCoalesceAssign(
        object? target,
        object? index,
        object? value,
        CsEvalOptions options,
        CsEvalContext context)
    {
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var currentValue = MemberAccess.GetIndex(target, index, options, context);
        if (currentValue != null)
            return currentValue;
        CheckAllowIndexSet(options, index);
        MemberAccess.SetIndex(target, index, value, options, context);
        return value;
    }

    public static object? ApplyMemberIncrement(
        object? target,
        string memberName,
        bool isIncrement,
        bool isPrefix,
        CsEvalOptions options,
        CsEvalContext context,
        bool isChecked)
    {
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, memberName);
        var currentValue = MemberAccess.GetMember(target, memberName, options, nullSafe: false, context);
        var one = GetNumericOne(currentValue);
        var newValue = isIncrement
            ? Operators.Add(currentValue, one, options, context, isChecked)
            : Operators.Subtract(currentValue, one, isChecked);
        MemberAccess.SetMember(target, memberName, newValue, options, context);
        return isPrefix ? newValue : currentValue;
    }

    public static object? ApplyIndexIncrement(
        object? target,
        object? index,
        bool isIncrement,
        bool isPrefix,
        CsEvalOptions options,
        CsEvalContext context,
        bool isChecked)
    {
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var currentValue = MemberAccess.GetIndex(target, index, options, context);
        var one = GetNumericOne(currentValue);
        var newValue = isIncrement
            ? Operators.Add(currentValue, one, options, context, isChecked)
            : Operators.Subtract(currentValue, one, isChecked);
        CheckAllowIndexSet(options, index);
        MemberAccess.SetIndex(target, index, newValue, options, context);
        return isPrefix ? newValue : currentValue;
    }

    private static TokenType ResolveCompoundBaseOperator(TokenType compoundOperator)
    {
        if (OperatorRegistry.CompoundToBaseOperator.TryGetValue(compoundOperator, out var baseOperator))
            return baseOperator;

        throw new CsEvalException($"Unknown compound assignment operator '{TokenLexemes.GetCanonical(compoundOperator)}'");
    }

    private static object? ApplyBinaryOperator(
        TokenType operatorType,
        object? left,
        object? right,
        CsEvalOptions options,
        CsEvalContext context,
        bool isChecked)
    {
        return operatorType switch
        {
            TokenType.Plus => Operators.Add(left, right, options, context, isChecked),
            TokenType.Minus => Operators.Subtract(left, right, isChecked),
            TokenType.Star => Operators.Multiply(left, right, options, isChecked),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.EqualEqualEqual => Operators.Equals(left, right),
            TokenType.BangEqualEqual => Operators.NotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, options),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, options),
            TokenType.Greater => Operators.GreaterThan(left, right, options),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, options),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            TokenType.GreaterGreaterGreater => Operators.UnsignedRightShift(left, right),
            TokenType.StarStar => Operators.Power(left, right),
            _ => throw new CsEvalException($"Unsupported compound assignment base operator '{operatorType}'")
        };
    }

    public static object? ValidateCompoundAssignment(string name, object? result, object? rightValue, CsEvalContext context)
    {
        if (!context.TryGetVariableType(name, out var varType) || varType == null || result == null)
            return result;

        var resultType = result.GetType();
        var rightType = rightValue?.GetType();

        if (resultType == varType)
            return result;

        var underlyingVarType = Nullable.GetUnderlyingType(varType) ?? varType;
        var rhsConvertible = rightType == null || rightType == varType ||
                             TypeHelpers.CanImplicitlyConvert(rightType, underlyingVarType) ||
                             IsConstantExpressionConvertible(rightValue, rightType, underlyingVarType);
        var resultConvertible = TypeHelpers.CanImplicitlyConvert(resultType, underlyingVarType);

        if (!resultConvertible && !rhsConvertible)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, resultType.Name, varType.Name);
        }

        var targetType = Nullable.GetUnderlyingType(varType) ?? varType;
        try
        {
            return TypeHelpers.RuntimeCast(result, resultType, targetType);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, resultType.Name, varType.Name);
        }
    }

    private static bool IsConstantExpressionConvertible(object? value, Type? sourceType, Type targetType)
    {
        if (value == null || sourceType != typeof(int))
            return false;

        var intVal = (int)value;

        if (targetType == typeof(sbyte)) return intVal is >= sbyte.MinValue and <= sbyte.MaxValue;
        if (targetType == typeof(byte)) return intVal is >= byte.MinValue and <= byte.MaxValue;
        if (targetType == typeof(short)) return intVal is >= short.MinValue and <= short.MaxValue;
        if (targetType == typeof(ushort)) return intVal is >= ushort.MinValue and <= ushort.MaxValue;
        if (targetType == typeof(uint)) return intVal >= 0;
        if (targetType == typeof(ulong)) return intVal >= 0;
        if (targetType == typeof(char)) return intVal is >= char.MinValue and <= char.MaxValue;

        return false;
    }
}
