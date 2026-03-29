using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitBoolCondition(BoundExpr condition) => _emissionCtx.EmitBoolCondition(condition);

    internal static bool IsConvertSafe(Type sourceType, Type targetType) =>
        sourceType == targetType
        || targetType.IsAssignableFrom(sourceType)
        || (IsArithmeticFastPathType(sourceType) && IsArithmeticFastPathType(targetType));
}
