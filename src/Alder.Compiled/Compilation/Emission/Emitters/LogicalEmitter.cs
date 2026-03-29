using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class LogicalEmitter : INodeEmitter<BoundLogicalExpr>
{
    public LinqExpression Emit(BoundLogicalExpr node, EmissionContext ctx)
    {
        var leftCandidate = ctx.Emit(node.Left);
        var rightCandidate = ctx.Emit(node.Right);

        if (leftCandidate.Type == typeof(bool) && rightCandidate.Type == typeof(bool))
        {
            return node.Operator switch
            {
                TokenType.PipePipe => LinqExpression.OrElse(leftCandidate, rightCandidate),
                TokenType.AmpAmp => LinqExpression.AndAlso(leftCandidate, rightCandidate),
                _ => throw new BindingNotSupportedException($"Unsupported bound logical operator '{node.Operator}'")
            };
        }

        if (node.Left.StaticType.ClrType == typeof(bool?) || node.Right.StaticType.ClrType == typeof(bool?))
        {
            var method = node.Operator == TokenType.AmpAmp ? NullableBoolAndMethod : NullableBoolOrMethod;
            return LinqExpression.Call(method, EmitHelpers.AsObject(leftCandidate), EmitHelpers.AsObject(rightCandidate));
        }

        var opLexeme = TokenLexemes.GetCanonical(node.Operator);
        var leftBool = LinqExpression.Call(
            RequireBooleanForLogicalOperatorMethod,
            EmitHelpers.AsObject(leftCandidate),
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(EmitHelpers.GetBoundTypeName(node.Right)));
        var rightBoolAsObject = LinqExpression.Convert(
            LinqExpression.Call(
                RequireBooleanForLogicalOperatorMethod,
                EmitHelpers.AsObject(rightCandidate),
                LinqExpression.Constant(opLexeme),
                LinqExpression.Constant(EmitHelpers.GetBoundTypeName(node.Left))),
            typeof(object));

        return node.Operator switch
        {
            TokenType.PipePipe => LinqExpression.Condition(
                leftBool,
                LinqExpression.Constant(true, typeof(object)),
                rightBoolAsObject),
            TokenType.AmpAmp => LinqExpression.Condition(
                leftBool,
                rightBoolAsObject,
                LinqExpression.Constant(false, typeof(object))),
            _ => throw new BindingNotSupportedException($"Unsupported bound logical operator '{node.Operator}'")
        };
    }
}
