using System.Collections.Immutable;

namespace Alder.Generators.Model;

internal readonly record struct TypeRegistrationModel(
    string TypeFullName,
    string MetadataClassName,
    bool IsClosedGeneric,
    ImmutableArray<PropertyModel> Properties,
    ImmutableArray<FieldModel> Fields,
    ImmutableArray<ConstructorModel> Constructors,
    ImmutableArray<IndexerModel> Indexers,
    ImmutableArray<MethodModel> Methods);
