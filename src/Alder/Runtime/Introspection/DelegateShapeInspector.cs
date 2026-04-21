using System.Diagnostics.CodeAnalysis;

namespace Alder.Runtime.Introspection;

internal static class DelegateShapeInspector
{
    public static bool TryGetInvoke(Type delegateType, [NotNullWhen(true)] out MethodInfo? invokeMethod)
    {
        invokeMethod = RuntimeTypeIntrospection.FindMethod(delegateType, nameof(Action.Invoke), BindingFlags.Public | BindingFlags.Instance);
        return invokeMethod != null;
    }

    public static bool TryGetSignature(Type delegateType, out Type[] parameterTypes, out Type returnType)
    {
        if (!TryGetInvoke(delegateType, out var invoke))
        {
            parameterTypes = Array.Empty<Type>();
            returnType = typeof(void);
            return false;
        }

        var parameters = invoke.GetParameters();
        parameterTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            parameterTypes[i] = parameters[i].ParameterType;

        returnType = invoke.ReturnType;
        return true;
    }

    public static int GetInputParameterCountOrMinusOne(Type delegateType)
    {
        return TryGetInvoke(delegateType, out var invoke)
            ? invoke!.GetParameters().Length
            : -1;
    }
}
