using System.Collections.Immutable;

namespace CsEval.Generators.Model;

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
    string TypeFullName);

internal readonly record struct ConstructorModel(
    ImmutableArray<ParameterModel> Parameters);

internal readonly record struct IndexerModel(
    string KeyTypeFullName,
    string ValueTypeFullName,
    bool CanRead,
    bool CanWrite);
