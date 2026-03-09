using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Binding;

internal sealed class Binder
{
    public BoundExpr Bind(Expr expr, BindingContext context)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(context);

        return expr switch
        {
            LiteralExpr literal => BoundLiteralExpr.FromValue(literal.Value),
            IdentifierExpr identifier => BindIdentifier(identifier, context),
            BinaryExpr binary => BindBinary(binary, context),
            _ => throw new CsEvalException(
                $"Binding for expression type '{expr.GetType().Name}' is not implemented")
        };
    }

    private static BoundIdentifierExpr BindIdentifier(IdentifierExpr identifier, BindingContext context)
    {
        var name = identifier.Name.Lexeme;
        context.TryGetVariableType(name, out var staticType);
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundBinaryExpr BindBinary(BinaryExpr binary, BindingContext context)
    {
        var left = Bind(binary.Left, context);
        var right = Bind(binary.Right, context);
        var resultType = InferBinaryResultType(binary.Op.Type, left.StaticType, right.StaticType);

        return new BoundBinaryExpr(binary.Op.Type, left, right, resultType);
    }

    private static Type InferBinaryResultType(TokenType op, Type leftType, Type rightType)
    {
        if (op == TokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
            return typeof(string);

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return leftType == rightType ? leftType : typeof(object);

        return typeof(object);
    }
}
