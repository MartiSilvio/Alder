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
    string DeclarationTypeFullName,
    bool IsOut = false,
    bool IsParams = false);

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
    bool ReturnsVoid)
{
    public bool HasOutParameters => Parameters.Any(static p => p.IsOut);
    public bool HasParams => Parameters.Length > 0 && Parameters[Parameters.Length - 1].IsParams;
    public int FixedParameterCount => HasParams ? Parameters.Length - 1 : Parameters.Length;
}
