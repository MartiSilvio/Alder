using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
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

        // §13.11.2: the C# compiler reports CS0160 when a catch clause is unreachable because a
        // previous clause catches all exceptions of the same or a base type.
        var handledTypes = new List<Type>();
        var catches = expr.CatchClauses
            .Select(catchClause =>
            {
                var catchScope = context.CreateChildScope();
                int? catchLocalId = null;
                Type? exceptionType = null;
                if (catchClause.ExceptionTypeName != null)
                {
                    exceptionType = context.RuntimeContext.TypeResolver.TryResolveType(catchClause.ExceptionTypeName);
                    // §13.11: the catch clause type must derive from System.Exception.
                    if (exceptionType != null
                        && exceptionType != typeof(Exception)
                        && !typeof(Exception).IsAssignableFrom(exceptionType))
                    {
                        throw new AlderException(DiagnosticDescriptors.ThrowExpressionMustBeException);
                    }
                }

                var effectiveType = exceptionType ?? typeof(Exception);

                // §13.11.2 / CS0160: a catch with no `when` filter shadows every identical-or-
                // derived catch that follows. Guarded prior catches remain reachable.
                if (catchClause.WhenGuard == null)
                {
                    foreach (var prior in handledTypes)
                    {
                        if (prior.IsAssignableFrom(effectiveType))
                            throw new AlderException(DiagnosticDescriptors.GeneralCatchAlreadyHandled, prior.Name);
                    }
                    handledTypes.Add(effectiveType);
                }

                if (catchClause.VariableName != null)
                {
                    catchLocalId = catchScope.DeclareLocal(
                        catchClause.VariableName.Value.Lexeme,
                        new BoundType(effectiveType));
                }

                var catchBinder = binder.WithAdditionalFlags(BinderFlags.InCatch);
                var whenGuard = catchClause.WhenGuard != null
                    ? catchBinder.Bind(catchClause.WhenGuard, catchScope)
                    : null;

                var body = catchClause.Body
                    .Select(statement => catchBinder.Bind(statement, catchScope))
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
            var finallyBinder = binder.WithAdditionalFlags(BinderFlags.InFinally);
            finallyBody = [
                ..expr.FinallyBody
                    .Select(statement => finallyBinder.Bind(statement, finallyScope))
            ];
        }

        return new BoundTryCatchFinallyExpr(tryBody, catches, finallyBody, BoundType.Void);
    }
}
