using System.Collections.Immutable;

namespace CsEval.Generators.Model;

internal readonly record struct ContextModel(
    string Namespace,
    string ClassName,
    ImmutableArray<TypeRegistrationModel> Registrations);
