using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.InterpolatedString)]
internal static class InterpolatedStringEmitter
{
    public static LinqExpression Emit(BoundInterpolatedStringExpr node, EmissionContext ctx)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var variables = new List<ParameterExpression> { sbVar };
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(StringBuilderCtor))
        };

        for (var i = 0; i < node.Parts.Length; i++)
        {
            switch (node.Parts[i])
            {
                case BoundInterpolatedTextPart textPart:
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod, LinqExpression.Constant(textPart.Text)));
                    break;

                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var valueVar = LinqExpression.Variable(typeof(object), $"interpValue{i}");
                    variables.Add(valueVar);
                    var format =
                        "{0" +
                        (expressionPart.AlignmentSpecifier != null ? "," + expressionPart.AlignmentSpecifier : string.Empty) +
                        (expressionPart.FormatSpecifier != null ? ":" + expressionPart.FormatSpecifier : string.Empty) +
                        "}";

                    statements.Add(LinqExpression.Assign(valueVar, ctx.EmitBoxed(expressionPart.Expression)));
                    statements.Add(LinqExpression.Call(
                        sbVar,
                        StringBuilderAppendMethod,
                        expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null
                            ? LinqExpression.Call(StringFormatMethod, LinqExpression.Constant(format), valueVar)
                            : LinqExpression.Condition(
                                LinqExpression.Equal(valueVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Constant(string.Empty),
                                LinqExpression.Call(valueVar, ObjectToStringMethod))));
                    break;
                }

                default:
                    throw new Binding.BindingNotSupportedException(
                        $"Bound interpolated part '{node.Parts[i].GetType().Name}' is not implemented");
            }
        }

        statements.Add(LinqExpression.Call(sbVar, StringBuilderToStringMethod));
        return LinqExpression.Block(typeof(string), variables, statements);
    }
}
