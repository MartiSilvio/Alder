namespace CsEval.Runtime;

internal static class TypeNameFormatter
{
    internal const string Null = "null";

    internal static string Of(object? value) => value?.GetType().Name ?? Null;
}
