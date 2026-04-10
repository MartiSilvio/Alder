using System.ComponentModel;

namespace Alder.Aot;

/// <summary>
/// Infrastructure methods called by source-generated dispatch code. Not intended for direct use.
/// Generated code calls these methods because the underlying runtime types are internal.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedCodeHelpers
{
    /// <summary>Invokes an Alder lambda with the given arguments.</summary>
    public static object? InvokeLambda(object lambda, object?[] args)
    {
        var lv = (Runtime.LambdaValue)lambda;
        return Runtime.MethodInvoker.InvokeLambda(lv, args, lv.Closure);
    }

    /// <summary>Converts a lambda or method reference to the specified delegate type.</summary>
    public static object? TryConvertDelegate(object value, System.Type delegateType)
    {
        return Runtime.LambdaDelegateConverter.TryConvert(value, delegateType);
    }

    /// <summary>
    /// Casts a lambda invocation result to <typeparamref name="T"/>,
    /// including Task&lt;object?&gt; to Task&lt;T&gt; mapping for async lambdas.
    /// </summary>
    public static T CastResult<T>(object? value)
    {
        return Runtime.LambdaDelegateFactory.CastResult<T>(value);
    }

    /// <summary>Gets the element type of an <see cref="IEnumerable{T}"/> implementation.</summary>
    public static System.Type? GetEnumerableElementType(System.Type type)
    {
        return Runtime.TypeHelpers.GetEnumerableElementType(type);
    }
}
