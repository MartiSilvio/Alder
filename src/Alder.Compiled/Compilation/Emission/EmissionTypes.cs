using System.Linq.Expressions;

namespace Alder.Compiled.Compilation.Emission;

internal sealed record PromotedLocal(string Name, ParameterExpression Variable, Type VariableType);

internal sealed record HoistedIdentifier(Type Type, ParameterExpression Variable);
