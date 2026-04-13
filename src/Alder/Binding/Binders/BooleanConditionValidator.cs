using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Binding.Binders;

/// <summary>
/// Validates that a bound expression used as a boolean condition converts to <c>bool</c>.
/// Used by if/while/do-while/for/conditional-expression binders to enforce §12.22 (boolean expressions).
/// </summary>
internal static class BooleanConditionValidator
{
    public static void Validate(BoundExpr condition)
    {
        if (condition.StaticType is BoundUnknownType or BoundVoidType)
            return;

        var source = condition.StaticType.ClrType;
        if (source == typeof(bool) || source == typeof(bool?))
            return;

        // §12.22: the condition must be convertible to bool. The predefined conversion lattice
        // does not include object→bool; it is an explicit unboxing conversion only.
        if (TypeHelpers.CanImplicitlyConvert(source, typeof(bool)))
            return;

        var sourceName = source.Name;
        if (TypeHelpers.HasExplicitConversion(source, typeof(bool)))
            throw new AlderException(DiagnosticDescriptors.ExplicitConversionExists, sourceName, "bool");

        throw new AlderException(DiagnosticDescriptors.NoImplicitConversion, sourceName, "bool");
    }
}
