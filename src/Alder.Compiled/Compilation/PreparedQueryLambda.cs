using System.Linq.Expressions;
using Alder.Binding;

namespace Alder.Compiled.Compilation;

internal sealed record PreparedQueryLambda(
    BoundExpr BoundBody,
    ParameterExpression[] Parameters,
    Dictionary<string, object?> CapturedVariables);
