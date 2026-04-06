using System.ComponentModel;

namespace Alder.Aot;

/// <summary>
/// Public entry points for source-generated dispatch code. Not intended for direct use.
/// Generated TypeMetadata and DelegateFactory classes call these methods because the
/// underlying runtime types (LambdaValue, MethodInvoker) are internal.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedCodeHelpers
{
    /// <summary>
    /// Invokes an Alder lambda with the given arguments. Used by generated delegate factories.
    /// </summary>
    public static object? InvokeLambda(object lambda, object?[] args)
    {
        var lv = (Runtime.LambdaValue)lambda;
        return Runtime.MethodInvoker.InvokeLambda(lv, args, lv.Closure);
    }

    /// <summary>
    /// Attempts to convert a lambda/method-ref to the specified delegate type.
    /// Used by generated lambda-aware method dispatch.
    /// </summary>
    public static object? TryConvertDelegate(object value, System.Type delegateType)
    {
        return Runtime.LambdaDelegateConverter.TryConvert(value, delegateType);
    }

    /// <summary>
    /// Gets the element type of an IEnumerable&lt;T&gt;. Used by generated extension dispatch.
    /// </summary>
    public static System.Type? GetEnumerableElementType(System.Type type)
    {
        return Runtime.TypeHelpers.GetEnumerableElementType(type);
    }
}
