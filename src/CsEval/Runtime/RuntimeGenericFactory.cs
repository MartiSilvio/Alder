using System.Diagnostics.CodeAnalysis;

namespace CsEval.Runtime;

internal static class RuntimeGenericFactory
{
    [RequiresDynamicCode("Calls MakeGenericType with runtime type arguments")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type CloseGenericType(
        Type openGenericType,
        Type[] typeArguments)
        => openGenericType.MakeGenericType(typeArguments);

    [RequiresDynamicCode("Calls MakeGenericMethod with runtime type arguments")]
    public static MethodInfo CloseGenericMethod(MethodInfo genericMethod, Type[] typeArguments)
        => genericMethod.MakeGenericMethod(typeArguments);

    [RequiresDynamicCode("Calls MakeGenericType with runtime type arguments")]
    public static bool TryCloseGenericType(
        Type openGenericType,
        Type[] typeArguments,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? closedType)
    {
        try
        {
            closedType = CloseGenericType(openGenericType, typeArguments);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            closedType = null;
            return false;
        }
    }

    [RequiresDynamicCode("Calls MakeGenericMethod with runtime type arguments")]
    public static bool TryCloseGenericMethod(MethodInfo genericMethod, Type[] typeArguments, out MethodInfo? closedMethod)
    {
        try
        {
            closedMethod = CloseGenericMethod(genericMethod, typeArguments);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            closedMethod = null;
            return false;
        }
    }
}
