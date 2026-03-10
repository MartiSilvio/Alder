using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace CsEval.Runtime;

internal static class RuntimeGenericFactory
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type CloseGenericType(
        Type openGenericType,
        Type[] typeArguments)
        => openGenericType.MakeGenericType(typeArguments);

    public static MethodInfo CloseGenericMethod(MethodInfo genericMethod, Type[] typeArguments)
        => genericMethod.MakeGenericMethod(typeArguments);

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
