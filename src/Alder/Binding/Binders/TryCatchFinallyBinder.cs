using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TryCatchFinallyExpr))]
internal static class TryCatchFinallyBinder
{
    public static BoundExpr Bind(TryCatchFinallyExpr expr, BindingContext context, BinderContext binder)
    {
        var tryScope = context.CreateChildScope();
        var tryBody = expr.TryBody
            .Select(statement => binder.Bind(statement, tryScope))
            .ToImmutableArray();

        var catches = expr.CatchClauses
            .Select(catchClause =>
            {
                var catchScope = context.CreateChildScope();
                int? catchLocalId = null;
                if (catchClause.VariableName != null)
                {
                    var exceptionType = catchClause.ExceptionTypeName != null
                        ? context.RuntimeContext.TypeResolver.TryResolveType(catchClause.ExceptionTypeName) ?? typeof(Exception)
                        : typeof(Exception);
                    catchLocalId = catchScope.DeclareLocal(catchClause.VariableName.Value.Lexeme, new BoundType(exceptionType));
                }

                var whenGuard = catchClause.WhenGuard != null
                    ? binder.Bind(catchClause.WhenGuard, catchScope)
                    : null;

                var body = catchClause.Body
                    .Select(statement => binder.Bind(statement, catchScope))
                    .ToImmutableArray();

                return new BoundCatchClause(
                    catchClause.ExceptionTypeName,
                    catchClause.VariableName?.Lexeme,
                    whenGuard,
                    body,
                    catchLocalId);
            })
            .ToImmutableArray();

        ImmutableArray<BoundExpr> finallyBody = ImmutableArray<BoundExpr>.Empty;
        if (expr.FinallyBody != null)
        {
            var finallyScope = context.CreateChildScope();
            finallyBody = [
                ..expr.FinallyBody
                    .Select(statement => binder.Bind(statement, finallyScope))
            ];
        }

        return new BoundTryCatchFinallyExpr(tryBody, catches, finallyBody, BoundType.Void);
    }
}
