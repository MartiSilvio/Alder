using System.Collections.Immutable;
using System.Linq;

namespace Alder.Generators.Model;

internal readonly record struct PropertyModel(
    string Name,
    string TypeFullName,
    bool CanRead,
    bool CanWrite,
    bool IsStatic);

internal readonly record struct FieldModel(
    string Name,
    string TypeFullName,
    bool IsReadOnly,
    bool IsStatic);

internal readonly record struct ParameterModel(
    string Name,
    string TypeFullName,
    bool IsParams = false,
    bool IsDelegate = false,
    DelegateSignature? DelegateInfo = null);

/// <summary>
/// Decomposed delegate signature extracted from Roslyn symbols at generation time.
/// Eliminates string parsing of type names in emitters.
/// </summary>
internal readonly record struct DelegateSignature(
    ImmutableArray<string> ParamTypes,
    string ReturnType,
    bool IsAction);

internal readonly record struct ConstructorModel(
    ImmutableArray<ParameterModel> Parameters);

internal readonly record struct IndexerModel(
    string KeyTypeFullName,
    string ValueTypeFullName,
    bool CanRead,
    bool CanWrite);

internal readonly record struct MethodModel(
    string Name,
    string ReturnTypeFullName,
    ImmutableArray<ParameterModel> Parameters,
    bool IsStatic,
    bool ReturnsVoid,
    ImmutableArray<string> GenericTypeArgs = default)
{
    public bool HasParams => Parameters.Length > 0 && Parameters[Parameters.Length - 1].IsParams;
    public int FixedParameterCount => HasParams ? Parameters.Length - 1 : Parameters.Length;
    public bool IsGenericInstantiation => !GenericTypeArgs.IsDefaultOrEmpty;

    /// <summary>
    /// True when any generic type arg is object (the canonical reference type).
    /// These entries exist to root shared-generic canonical forms for NativeAOT
    /// but must NOT be used for dispatch because Func covariance causes type loss.
    /// </summary>
    public bool IsCanonicalRoot => IsGenericInstantiation &&
        GenericTypeArgs.Any(a => a == "global::System.Object" || a == "object");
}
