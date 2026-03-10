namespace CsEval.Runtime;

internal static class NumericPromotionRuntime
{
    public static (object? Left, object? Right) ApplyConstantNumericPromotion(
        object? left,
        bool leftIsConstant,
        object? right,
        bool rightIsConstant)
    {
        if (left != null && right != null &&
            TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right) &&
            (leftIsConstant || rightIsConstant))
        {
            var promoted = NumericDispatch.TryConstantPromotion(left, leftIsConstant, right, rightIsConstant);
            if (promoted != null)
                return (promoted.Value.Left, promoted.Value.Right);
        }

        return (left, right);
    }
}
