using Alder.Diagnostics;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new AlderException(DiagnosticDescriptors.NoImplicitConversion, TypeNameFormatter.Of(value), "bool");
    }

    public static bool RequireBooleanForLogicalOperator(object? value, string opLexeme, string otherOperandTypeName)
    {
        if (value is bool b)
            return b;

        throw new AlderException(
            DiagnosticDescriptors.BadBinaryOps,
            opLexeme,
            TypeNameFormatter.Of(value),
            otherOperandTypeName);
    }

    // ECMA-334 §12.13.5 + §12.14.2: Three-value logic for bool? && and bool? ||
    public static object? NullableBoolAnd(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == false || r == false) return BoxedConstants.False;
        if (l == true && r == true) return BoxedConstants.True;
        return null;
    }

    public static object? NullableBoolOr(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == true || r == true) return BoxedConstants.True;
        if (l == false && r == false) return BoxedConstants.False;
        return null;
    }
}
