using System.ComponentModel;

namespace Alder.Aot;

/// <summary>
/// Helper methods used by generated AOT dispatch code.
/// They exist so generated sources can reach internal runtime functionality through a stable public surface.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedCodeHelpers
{
    /// <summary>Invokes an Alder lambda value with the supplied arguments.</summary>
    public static object? InvokeLambda(object lambda, object?[] args)
    {
        var lv = (Runtime.LambdaValue)lambda;
        return Runtime.MethodInvoker.InvokeLambda(lv, args, lv.Closure);
    }

    /// <summary>Attempts to convert a lambda or method reference to a concrete delegate type.</summary>
    public static object? TryConvertDelegate(object value, System.Type delegateType)
    {
        return Runtime.LambdaDelegateConverter.TryConvert(value, delegateType);
    }

    /// <summary>
    /// Casts a lambda invocation result to <typeparamref name="T"/>.
    /// This also handles async result adaptation such as <c>Task&lt;object?&gt;</c> to <c>Task&lt;T&gt;</c>.
    /// </summary>
    public static T CastResult<T>(object? value)
    {
        return Runtime.LambdaDelegateFactory.CastResult<T>(value);
    }

    /// <summary>Returns the element type for an <see cref="IEnumerable{T}"/> implementation when one exists.</summary>
    public static System.Type? GetEnumerableElementType(System.Type type)
    {
        return Runtime.TypeHelpers.GetEnumerableElementType(type);
    }
}
