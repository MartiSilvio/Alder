using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.InterpolatedString)]
internal static class InterpolatedStringEvaluator
{
    public static object? Evaluate(BoundInterpolatedStringExpr node, EvaluationContext ctx)
    {
        var sb = new StringBuilder();
        foreach (var part in node.Parts)
        {
            switch (part)
            {
                case BoundInterpolatedTextPart text:
                    sb.Append(text.Text);
                    break;
                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var value = ctx.Evaluate(expressionPart.Expression);
                    if (expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null)
                    {
                        var format = "{0";
                        if (expressionPart.AlignmentSpecifier != null)
                            format += "," + expressionPart.AlignmentSpecifier;
                        if (expressionPart.FormatSpecifier != null)
                            format += ":" + expressionPart.FormatSpecifier;
                        format += "}";
                        sb.Append(string.Format(format, value));
                    }
                    else
                    {
                        sb.Append(value?.ToString() ?? string.Empty);
                    }

                    break;
                }
                default:
                    throw new BindingNotSupportedException(
                        $"Bound interpolated part '{part.GetType().Name}' is not implemented");
            }
        }

        return sb.ToString();
    }
}
