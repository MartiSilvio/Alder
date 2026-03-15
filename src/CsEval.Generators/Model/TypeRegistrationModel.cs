using System.Collections.Immutable;

namespace CsEval.Generators.Model;

internal readonly record struct TypeRegistrationModel(
    string TypeFullName,
    string TypeMinimalName,
    string MetadataClassName,
    bool IsClosedGeneric,
    ImmutableArray<PropertyModel> Properties,
    ImmutableArray<FieldModel> Fields,
    ImmutableArray<ConstructorModel> Constructors,
    ImmutableArray<IndexerModel> Indexers,
    ImmutableArray<MethodModel> Methods);
