using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

internal sealed class ExpressionBinaryCompiler
{
    private readonly ExpressionCompilerUnit _owner;

    internal ExpressionBinaryCompiler(ExpressionCompilerUnit owner)
    {
        _owner = owner;
    }

    internal LinqExpression EmitBinaryOpCall(OperatorRegistry.BinaryOpInfo info, LinqExpression left, LinqExpression right)
    {
        left = EnsureObjectExpression(left);
        right = EnsureObjectExpression(right);

        var checkedConst = LinqExpression.Constant(_owner.Context.IsChecked);
        LinqExpression call = info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, left, right),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, left, right, _owner.Context.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, left, right, _owner.Context.OptionsParam, _owner.Context.CurrentContext),
            OperatorRegistry.BinaryOpSignature.TwoArgsChecked =>
                LinqExpression.Call(info.Method, left, right, checkedConst),
            OperatorRegistry.BinaryOpSignature.WithOptionsChecked =>
                LinqExpression.Call(info.Method, left, right, _owner.Context.OptionsParam, checkedConst),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContextChecked =>
                LinqExpression.Call(info.Method, left, right, _owner.Context.OptionsParam, _owner.Context.CurrentContext, checkedConst),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
        };

        LinqExpression result = call;

        if (info.NegateBooleanResult)
        {
            var boolCall = result.Type == typeof(bool)
                ? result
                : LinqExpression.Convert(result, typeof(bool));
            result = LinqExpression.Not(boolCall);
        }

        return result.Type == typeof(object)
            ? result
            : LinqExpression.Convert(result, typeof(object));
    }

    private static LinqExpression EnsureObjectExpression(LinqExpression expression) =>
        expression.Type == typeof(object)
            ? expression
            : LinqExpression.Convert(expression, typeof(object));

    internal LinqExpression CompileBinary(BinaryExpr b)
    {
        if (_owner.DirectEmit.TryFoldPureConstantExpression(b, out var folded))
            return folded;

        if (_owner.DirectEmit.TryEmitDirectBinary(b) is { } direct)
            return direct;

        var left = _owner.Compile(b.Left);
        var right = _owner.Compile(b.Right);

        // ECMA-334 §10.2.11: Implicit constant expression conversions.
        ApplyConstantPromotion(b, ref left, ref right);

        var opInfo = OperatorRegistry.GetBinaryOperator(b.Op.Type);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator {b.Op.Type}");

        return EmitBinaryOpCall(opInfo.Value, left, right);
    }

    /// <summary>
    /// ECMA-334 §10.2.11: At IL-compile time, pre-promote constant literal operands.
    /// Since literal values are known at compile time, we can replace the compiled
    /// LinqExpression.Constant with a promoted-type constant (e.g., int 3 -> uint 3).
    /// </summary>
    private static void ApplyConstantPromotion(BinaryExpr b, ref LinqExpression left, ref LinqExpression right)
    {
        var leftLiteral = b.Left as LiteralExpr;
        var rightLiteral = b.Right as LiteralExpr;

        bool leftIsConstant = leftLiteral is { IsConstant: true };
        bool rightIsConstant = rightLiteral is { IsConstant: true };

        if (!leftIsConstant && !rightIsConstant)
            return;

        // We need both operand values to call TryConstantPromotion.
        // Both sides must be non-null literals for this compile-time optimization.
        object? leftVal = leftLiteral?.Value;
        object? rightVal = rightLiteral?.Value;

        if (leftVal == null || rightVal == null) return;

        var promoted = NumericDispatch.TryConstantPromotion(
            leftVal, leftIsConstant, rightVal, rightIsConstant);

        if (promoted != null)
        {
            left = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Left, promoted.Value.Left.GetType()),
                typeof(object));
            right = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Right, promoted.Value.Right.GetType()),
                typeof(object));
        }
    }

    internal LinqExpression CompileLogical(LogicalExpr l)
    {
        var opLexeme = l.Op.Lexeme;
        var leftTypeName = _owner.Context.TypeInferrer.Infer(l.Left).Name;
        var rightTypeName = _owner.Context.TypeInferrer.Infer(l.Right).Name;

        var leftTruthy = CompileLogicalOperandAsBoolean(l.Left, opLexeme, rightTypeName);
        var rightTruthy = CompileLogicalOperandAsBoolean(l.Right, opLexeme, leftTypeName);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    private LinqExpression CompileLogicalOperandAsBoolean(
        Expr operand,
        string opLexeme,
        string otherOperandTypeName)
    {
        var (compiled, knownType) = _owner.CompileTyped(operand);

        if (knownType == typeof(bool) && compiled.Type == typeof(bool))
            return compiled;

        var boxed = compiled.Type == typeof(object)
            ? compiled
            : LinqExpression.Convert(compiled, typeof(object));

        return LinqExpression.Call(
            CompilerReflectionCache.RequireBooleanForLogicalOperatorMethod,
            boxed,
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(otherOperandTypeName));
    }

    internal LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(CompilerReflectionCache.RequireBooleanMethod, _owner.Compile(c.Condition));
        var thenBranch = _owner.Compile(c.ThenBranch);
        var elseBranch = _owner.Compile(c.ElseBranch);

        // Get static types for promotion check (ECMA-334 §12.18)
        var thenType = _owner.Context.TypeInferrer.Infer(c.ThenBranch);
        var elseType = _owner.Context.TypeInferrer.Infer(c.ElseBranch);

        var result = LinqExpression.Condition(condition, thenBranch, elseBranch);

        // Apply type promotion at compile time if both branches are numeric with different types
        if (thenType != typeof(object) && elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) && TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var promotionType = NumericDispatch.GetResultType(thenType, elseType);
            var promoteMethod = typeof(NumericDispatch).GetMethod(nameof(NumericDispatch.PromoteToType))!;
            return LinqExpression.Call(promoteMethod, result, LinqExpression.Constant(promotionType, typeof(Type)));
        }

        return result;
    }

    internal LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = _owner.Compile(n.Left);
        var right = _owner.Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }
}
