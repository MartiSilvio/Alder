namespace CsEval.Runtime.Extensions;

internal static class ScopeFunctionExtensions
{
    public static TResult let<T, TResult>(this T value, Func<T, TResult> block) => block(value);

    public static T also<T>(this T value, Func<T, object?> block)
    {
        _ = block(value);
        return value;
    }

    public static T apply<T>(this T value, Func<T, object?> block)
    {
        _ = block(value);
        return value;
    }

    public static TResult run<T, TResult>(this T value, Func<T, TResult> block) => block(value);

    public static TResult with<T, TResult>(this T value, Func<T, TResult> block) => block(value);
}
