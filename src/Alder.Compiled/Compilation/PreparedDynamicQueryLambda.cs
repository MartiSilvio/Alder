using System.Linq.Expressions;
using Alder.Binding;

namespace Alder.Compiled.Compilation;

internal sealed record PreparedDynamicQueryLambda(
    DynamicQueryLambdaKind Kind,
    DynamicQueryResultShape ResultShape,
    Type ResultType,
    BoundExpr BoundBody,
    ParameterExpression[] Parameters,
    Dictionary<string, object?> CapturedVariables,
    LambdaExpression ExportedLambda);
