using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class NullCoalesceAssignEmitter : INodeEmitter<BoundNullCoalesceAssignExpr>
{
    public LinqExpression Emit(BoundNullCoalesceAssignExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            if (!TypeHelpers.IsNullableType(promoted.VariableType))
            {
                return LinqExpression.Block(
                    typeof(object),
                    LinqExpression.Throw(
                        LinqExpression.Constant(
                            new AlderException(
                                DiagnosticDescriptors.BadBinaryOps,
                                TokenLexemes.GetCanonical(TokenType.QuestionQuestionEqual),
                                promoted.VariableType.Name,
                                promoted.VariableType.Name))),
                    LinqExpression.Constant(null, typeof(object)));
            }

            var storageType = promoted.Variable.Type;
            var assignedVar = LinqExpression.Variable(storageType, "coalesceAssigned");
            var nullConst = storageType.IsValueType
                ? LinqExpression.Constant(null, typeof(Nullable<>).MakeGenericType(Nullable.GetUnderlyingType(storageType)!))
                : LinqExpression.Constant(null, storageType);
            return LinqExpression.Block(
                storageType,
                [assignedVar],
                LinqExpression.Condition(
                    LinqExpression.NotEqual(promoted.Variable, nullConst),
                    promoted.Variable,
                    LinqExpression.Block(
                        storageType,
                        LinqExpression.Assign(assignedVar, ctx.EmitAs(node.Value, storageType)),
                        LinqExpression.Assign(promoted.Variable, assignedVar),
                        assignedVar)));
        }

        var currentVar = LinqExpression.Variable(typeof(object), "coalesceCurrent");
        var nonPromotedAssigned = LinqExpression.Variable(typeof(object), "coalesceAssigned");
        return LinqExpression.Block(
            typeof(object),
            [currentVar, nonPromotedAssigned],
            LinqExpression.Call(
                CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(node.Name),
                ctx.ContextParam),
            LinqExpression.Assign(
                currentVar,
                LinqExpression.Call(ctx.ContextParam, ContextGetMethod, LinqExpression.Constant(node.Name))),
            LinqExpression.Condition(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                currentVar,
                LinqExpression.Block(
                    LinqExpression.Assign(nonPromotedAssigned, ctx.EmitBoxed(node.Value)),
                    LinqExpression.Call(
                        ctx.ContextParam,
                        ContextSetMethod,
                        LinqExpression.Constant(node.Name),
                        nonPromotedAssigned),
                    nonPromotedAssigned)));
    }
}
