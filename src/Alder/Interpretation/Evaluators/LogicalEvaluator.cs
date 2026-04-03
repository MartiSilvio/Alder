using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.LogicalOperator)]
internal static class LogicalEvaluator
{
    public static object? Evaluate(BoundLogicalExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var left = ctx.Evaluate(node.Left, ct);
        var opLexeme = TokenLexemes.GetCanonical(node.Operator);

        // §12.14.2 + §12.13.5: nullable bool conditional operators
        if (node.Left.StaticType.ClrType == typeof(bool?) || node.Right.StaticType.ClrType == typeof(bool?))
        {
            return EvaluateNullableBoolLogical(left as bool?, node, ctx, ct);
        }

        if (left is not bool leftBool)
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                TypeNameFormatter.Of(left),
                GetLogicalExpressionTypeName(node.Right));
        }

        if (node.Operator == TokenType.PipePipe)
        {
            if (leftBool)
                return true;
        }
        else if (node.Operator == TokenType.AmpAmp)
        {
            if (!leftBool)
                return false;
        }
        else
        {
            throw new BindingNotSupportedException(
                $"Bound logical operator '{node.Operator}' is not implemented");
        }

        var right = ctx.Evaluate(node.Right, ct);
        if (right is not bool)
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        }

        return (bool)right;
    }

    // §12.13.5 + §12.14.2: Three-value logic for bool? && and bool? ||
    private static object? EvaluateNullableBoolLogical(bool? left, BoundLogicalExpr logical, EvaluationContext ctx, CancellationToken ct)
    {
        if (logical.Operator == TokenType.AmpAmp)
        {
            if (left == false) return BoxedConstants.False;
            var right = ctx.Evaluate(logical.Right, ct) as bool?;
            if (left == true) return right;
            return right == false ? BoxedConstants.False : null;
        }

        if (logical.Operator == TokenType.PipePipe)
        {
            if (left == true) return BoxedConstants.True;
            var right = ctx.Evaluate(logical.Right, ct) as bool?;
            if (left == false) return right;
            return right == true ? BoxedConstants.True : null;
        }

        throw new BindingNotSupportedException(
            $"Bound logical operator '{logical.Operator}' is not implemented");
    }

    public static async ValueTask<object?> EvaluateAsync(BoundLogicalExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var left = await ctx.EvaluateAsync(node.Left, ct);
        var opLexeme = TokenLexemes.GetCanonical(node.Operator);

        if (node.Left.StaticType.ClrType == typeof(bool?) || node.Right.StaticType.ClrType == typeof(bool?))
        {
            return await EvaluateNullableBoolLogicalAsync(left as bool?, node, ctx, ct);
        }

        if (left is not bool leftBool)
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                TypeNameFormatter.Of(left),
                GetLogicalExpressionTypeName(node.Right));
        }

        if (node.Operator == TokenType.PipePipe)
        {
            if (leftBool)
                return true;
        }
        else if (node.Operator == TokenType.AmpAmp)
        {
            if (!leftBool)
                return false;
        }
        else
        {
            throw new BindingNotSupportedException(
                $"Bound logical operator '{node.Operator}' is not implemented");
        }

        var right = await ctx.EvaluateAsync(node.Right, ct);
        if (right is not bool)
        {
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        }

        return (bool)right;
    }

    private static async ValueTask<object?> EvaluateNullableBoolLogicalAsync(bool? left, BoundLogicalExpr logical, EvaluationContext ctx, CancellationToken ct)
    {
        if (logical.Operator == TokenType.AmpAmp)
        {
            if (left == false) return BoxedConstants.False;
            var right = await ctx.EvaluateAsync(logical.Right, ct) as bool?;
            if (left == true) return right;
            return right == false ? BoxedConstants.False : null;
        }

        if (logical.Operator == TokenType.PipePipe)
        {
            if (left == true) return BoxedConstants.True;
            var right = await ctx.EvaluateAsync(logical.Right, ct) as bool?;
            if (left == false) return right;
            return right == true ? BoxedConstants.True : null;
        }

        throw new BindingNotSupportedException(
            $"Bound logical operator '{logical.Operator}' is not implemented");
    }

    private static string GetLogicalExpressionTypeName(BoundExpr expr)
    {
        if (expr is BoundLiteralExpr { Value: null })
            return TypeNameFormatter.Null;

        if (expr is BoundLiteralExpr { Value: { } value })
            return value.GetType().Name;

        return expr.StaticType.ClrType == typeof(object)
            ? "unknown"
            : expr.StaticType.ClrType.Name;
    }
}
