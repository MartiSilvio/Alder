namespace Alder.Generators.Model;

internal readonly record struct TypeArgumentModel(
    string TypeFullName,
    bool IsValueType,
    bool CanBeNull);
