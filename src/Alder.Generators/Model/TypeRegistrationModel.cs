using System.Collections.Immutable;

namespace Alder.Generators.Model;

internal readonly record struct TypeRegistrationModel(
    string TypeFullName,
    string? OriginalDefinitionNamespace,
    string? OriginalDefinitionMetadataName,
    ImmutableArray<TypeArgumentModel> TypeArguments,
    string MetadataClassName,
    bool IsValueType,
    ImmutableArray<PropertyModel> Properties,
    ImmutableArray<FieldModel> Fields,
    ImmutableArray<ConstructorModel> Constructors,
    ImmutableArray<IndexerModel> Indexers,
    ImmutableArray<MethodModel> Methods);
