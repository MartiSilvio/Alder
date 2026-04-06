using System.Collections.Immutable;

namespace Alder.Generators.Model;

/// <summary>
/// Represents a discovered extension method overload for AOT dispatch generation.
/// Extracted from Roslyn symbols at generation time — no hardcoded method names.
/// </summary>
internal readonly record struct ExtensionMethodModel(
    string MethodName,
    ExtensionMethodKind Kind,
    ImmutableArray<ExtensionParamModel> ExtraParams);

/// <summary>
/// A parameter beyond the first (this) parameter of an extension method.
/// </summary>
internal readonly record struct ExtensionParamModel(
    string TypePattern,
    bool IsDelegate,
    bool IsTypeParameter,
    DelegateSignature? DelegateInfo);

/// <summary>
/// Classification of extension method signatures for dispatch generation.
/// Derived from Roslyn symbol analysis, not hardcoded.
/// </summary>
internal enum ExtensionMethodKind
{
    /// <summary>No extra params: e.g., Sum(), ToList(), Distinct()</summary>
    NoArg,

    /// <summary>One delegate param: e.g., Where(Func&lt;T,bool&gt;), Select(Func&lt;T,TResult&gt;)</summary>
    SingleDelegate,

    /// <summary>One value param: e.g., Take(int), Contains(T), Append(T)</summary>
    SingleValue,

    /// <summary>One enumerable param: e.g., Concat(IEnumerable&lt;T&gt;)</summary>
    SingleEnumerable,

    /// <summary>Multiple params or complex signature — not generated, falls to reflection</summary>
    Complex,
}
