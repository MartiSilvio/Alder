using Alder.Runtime;
using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private BoundBlockExpr BindBlock(BlockExpr block, BindingContext context)
    {
        var blockScope = context.CreateChildScope();
        var statements = block.Statements
            .Select(statement => Bind(statement, blockScope))
            .ToImmutableArray();
        var returnExpr = block.ReturnExpr != null ? Bind(block.ReturnExpr, blockScope) : null;
        var staticType = returnExpr?.StaticType ?? BoundType.Unknown;
        return new BoundBlockExpr(statements, returnExpr, staticType);
    }

    private BoundIfStatementExpr BindIfStatement(IfStatementExpr ifStatement, BindingContext context)
    {
        var condition = Bind(ifStatement.Condition, context);
        var thenScope = context.CreateChildScope();
        var thenStatements = ifStatement.ThenStatements
            .Select(statement => Bind(statement, thenScope))
            .ToImmutableArray();

        ImmutableArray<BoundExpr> elseStatements = ImmutableArray<BoundExpr>.Empty;
        if (ifStatement.ElseStatements is { Count: > 0 } elseSource)
        {
            var elseScope = context.CreateChildScope();
            elseStatements = [
                ..elseSource
                    .Select(statement => Bind(statement, elseScope))
            ];
        }

        return new BoundIfStatementExpr(condition, thenStatements, elseStatements, BoundType.Void);
    }

    private BoundVariableDeclExpr BindVariableDecl(VariableDeclExpr variableDecl, BindingContext context)
    {
        var declaredType = variableDecl.DeclaredType != null
            ? context.RuntimeContext.TypeResolver.ResolveType(variableDecl.DeclaredType.Value.Lexeme)
            : null;
        var initializer = variableDecl.Initializer is CollectionExpr collectionExpr && declaredType != null
            ? BindCollectionExpr(collectionExpr, context, declaredType)
            : Bind(variableDecl.Initializer, context);
        var staticType = declaredType != null ? new BoundType(declaredType) : initializer.StaticType;
        var localId = context.DeclareLocal(variableDecl.Name.Lexeme, staticType, variableDecl.IsConst);
        return new BoundVariableDeclExpr(
            variableDecl.Name.Lexeme,
            initializer,
            declaredType,
            staticType,
            IsConst: variableDecl.IsConst,
            LocalId: localId);
    }

    private BoundWhileExpr BindWhile(WhileStatementExpr whileStatement, BindingContext context)
    {
        var condition = Bind(whileStatement.Condition, context);
        var bodyScope = context.CreateChildScope();
        var body = whileStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundWhileExpr(condition, body, BoundType.Void);
    }

    private BoundForExpr BindFor(ForStatementExpr forStatement, BindingContext context)
    {
        var loopScope = context.CreateChildScope();
        var initializers = forStatement.Initializers
            .Select(initializer => Bind(initializer, loopScope))
            .ToImmutableArray();
        var condition = forStatement.Condition != null
            ? Bind(forStatement.Condition, loopScope)
            : null;
        var increments = forStatement.Increments
            .Select(increment => Bind(increment, loopScope))
            .ToImmutableArray();

        var bodyScope = loopScope.CreateChildScope();
        var body = forStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForExpr(initializers, condition, increments, body, BoundType.Void);
    }

    private BoundDoWhileExpr BindDoWhile(DoWhileStatementExpr doWhileStatement, BindingContext context)
    {
        var bodyScope = context.CreateChildScope();
        var body = doWhileStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        var condition = Bind(doWhileStatement.Condition, context);
        return new BoundDoWhileExpr(body, condition, BoundType.Void);
    }

    private BoundForEachExpr BindForEach(ForEachStatementExpr forEachStatement, BindingContext context)
    {
        var collection = Bind(forEachStatement.Collection, context);
        var elementType = InferElementType(collection.StaticType.ClrType);
        var bodyScope = context.CreateChildScope();
        var foreachLocalId = bodyScope.DeclareLocal(forEachStatement.VariableName.Lexeme, new BoundType(elementType));
        var body = forEachStatement.Body
            .Select(statement => Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForEachExpr(forEachStatement.VariableName.Lexeme, collection, body, elementType, BoundType.Void, foreachLocalId);
    }

    private static Type InferElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType.GetGenericArguments()[0];

        return typeof(object);
    }

    private BoundUsingStatementExpr BindUsingStatement(UsingStatementExpr usingStatement, BindingContext context)
    {
        var resource = Bind(usingStatement.ResourceDeclaration, context);
        var body = Bind(usingStatement.Body, context.CreateChildScope());
        return new BoundUsingStatementExpr(resource, body, BoundType.Void);
    }

    private BoundLockStatementExpr BindLockStatement(LockStatementExpr lockStatement, BindingContext context)
    {
        var lockObject = Bind(lockStatement.LockObject, context);
        var body = Bind(lockStatement.Body, context.CreateChildScope());
        return new BoundLockStatementExpr(lockObject, body, BoundType.Void);
    }

    private BoundTryCatchFinallyExpr BindTryCatchFinally(TryCatchFinallyExpr tryCatchFinally, BindingContext context)
    {
        var tryScope = context.CreateChildScope();
        var tryBody = tryCatchFinally.TryBody
            .Select(statement => Bind(statement, tryScope))
            .ToImmutableArray();

        var catches = tryCatchFinally.CatchClauses
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
                    ? Bind(catchClause.WhenGuard, catchScope)
                    : null;

                var body = catchClause.Body
                    .Select(statement => Bind(statement, catchScope))
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
        if (tryCatchFinally.FinallyBody != null)
        {
            var finallyScope = context.CreateChildScope();
            finallyBody = [
                ..tryCatchFinally.FinallyBody
                    .Select(statement => Bind(statement, finallyScope))
            ];
        }

        return new BoundTryCatchFinallyExpr(tryBody, catches, finallyBody, BoundType.Void);
    }

    private BoundThrowExpr BindThrowExpr(ThrowExpr throwExpr, BindingContext context)
    {
        var expression = Bind(throwExpr.Expression, context);
        return new BoundThrowExpr(expression, BoundType.Void);
    }

    private BoundSwitchStatementExpr BindSwitchStatement(SwitchStatementExpr switchStatement, BindingContext context)
    {
        var expression = Bind(switchStatement.Expression, context);
        var cases = switchStatement.Cases
            .Select(switchCase =>
            {
                var caseScope = context.CreateChildScope();
                var guard = switchCase.WhenGuard != null
                    ? Bind(switchCase.WhenGuard, caseScope)
                    : null;
                var statements = switchCase.Statements
                    .Select(statement => Bind(statement, caseScope))
                    .ToImmutableArray();
                return new BoundSwitchCase(switchCase.CasePattern, guard, statements);
            })
            .ToImmutableArray();
        return new BoundSwitchStatementExpr(expression, cases, BoundType.Void);
    }

    private BoundSwitchExpressionExpr BindSwitchExpression(SwitchExpressionExpr switchExpression, BindingContext context)
    {
        var expression = Bind(switchExpression.Expression, context);
        var arms = switchExpression.Arms
            .Select(arm =>
            {
                var armScope = context.CreateChildScope();
                var whenGuard = arm.WhenGuard != null ? Bind(arm.WhenGuard, armScope) : null;
                var value = Bind(arm.Value, armScope);
                return new BoundSwitchExpressionArm(arm.Pattern, whenGuard, value);
            })
            .ToImmutableArray();

        var staticType = typeof(object);
        if (arms.Length > 0)
        {
            staticType = arms[0].Value.StaticType.ClrType;
            for (var i = 1; i < arms.Length; i++)
                staticType = GetCommonType(staticType, arms[i].Value.StaticType.ClrType);
        }

        return new BoundSwitchExpressionExpr(expression, arms, new BoundType(staticType));
    }

    private BoundReturnExpr BindReturn(ReturnExpr returnExpr, BindingContext context)
    {
        var value = returnExpr.Value != null ? Bind(returnExpr.Value, context) : null;
        return new BoundReturnExpr(value, value?.StaticType ?? BoundType.Void);
    }
}
