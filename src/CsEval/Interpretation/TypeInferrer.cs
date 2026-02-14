using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

/// <summary>
/// Infers static types for AST nodes to enable proper unboxing semantics.
/// In C#, casting from 'object' requires unboxing to the exact boxed type.
/// This visitor computes compile-time types so the evaluator can detect invalid unboxing.
/// </summary>
public sealed class TypeInferrer : AstWalker<Type>
{
    /// <summary>
    /// Sentinel type for throw expressions -- throw never produces a value,
    /// so in a conditional the result type is the other branch's type.
    /// </summary>
    internal static readonly Type ThrowSentinel = typeof(ThrowSentinelMarker);
    private sealed class ThrowSentinelMarker { }

    /// <summary>
    /// Sentinel type for null literals -- null is compatible with any reference type,
    /// so in a conditional the result type is the non-null branch's type.
    /// </summary>
    internal static readonly Type NullSentinel = typeof(NullSentinelMarker);
    private sealed class NullSentinelMarker { }

    private readonly CsEvalContext _context;
    private readonly Dictionary<Expr, Type> _types = new();

    // Local scope for tracking variable types declared during inference
    private readonly Stack<Dictionary<string, Type>> _scopes = new();
    private Dictionary<string, Type> CurrentScope => _scopes.Count > 0 ? _scopes.Peek() : _globalScope;
    private readonly Dictionary<string, Type> _globalScope = new();

    protected override Type DefaultValue => typeof(object);

    public TypeInferrer(CsEvalContext context)
    {
        _context = context;
    }

    public IReadOnlyDictionary<Expr, Type> Types => _types;

    /// <summary>
    /// Runs type inference on the entire AST to populate all variable types.
    /// Must be called before using GetInferredType() to get accurate static types.
    /// </summary>
    public void InferAll(Expr root) => Visit(root);

    /// <summary>
    /// Gets the inferred type for an expression. Returns object if not yet inferred.
    /// </summary>
    public Type GetInferredType(Expr expr) =>
        _types.TryGetValue(expr, out var type) ? type : typeof(object);

    /// <summary>
    /// Alias for GetInferredType - gets the static type of an expression.
    /// </summary>
    public Type Infer(Expr expr) => GetInferredType(expr);

    private void PushScope() => _scopes.Push(new Dictionary<string, Type>());
    private void PopScope() => _scopes.Pop();

    private bool TryGetVariableType(string name, out Type? type)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out type!))
                return true;
        }

        if (_globalScope.TryGetValue(name, out type!))
            return true;

        return _context.TryGetVariableType(name, out type);
    }

    private void DefineVariable(string name, Type type) => CurrentScope[name] = type;

    private Type SetType(Expr expr, Type type)
    {
        _types[expr] = type;
        return type;
    }

    #region Type Computation Overrides

    public override Type VisitLiteral(LiteralExpr expr)
    {
        base.VisitLiteral(expr);
        if (expr.Value == null)
            return SetType(expr, NullSentinel);
        return SetType(expr, expr.Value.GetType());
    }

    public override Type VisitIdentifier(IdentifierExpr expr)
    {
        base.VisitIdentifier(expr);
        if (TryGetVariableType(expr.Name.Lexeme, out var varType) && varType != null)
            return SetType(expr, varType);
        return SetType(expr, typeof(object));
    }

    public override Type VisitTypeReference(TypeReferenceExpr expr)
    {
        base.VisitTypeReference(expr);
        // TypeReferenceExpr returns a Type object at runtime
        return SetType(expr, typeof(Type));
    }

    public override Type VisitUnary(UnaryExpr expr)
    {
        base.VisitUnary(expr);
        var rightType = GetInferredType(expr.Right);
        var type = expr.Op.Type == TokenType.Bang ? typeof(bool) : rightType;
        return SetType(expr, type);
    }

    public override Type VisitBinary(BinaryExpr expr)
    {
        base.VisitBinary(expr);

        if (expr.Op.Type is TokenType.EqualEqual or TokenType.BangEqual or
            TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual)
        {
            return SetType(expr, typeof(bool));
        }

        var leftType = GetInferredType(expr.Left);
        var rightType = GetInferredType(expr.Right);
        return SetType(expr, GetBinaryResultType(leftType, rightType));
    }

    public override Type VisitLogical(LogicalExpr expr)
    {
        base.VisitLogical(expr);
        return SetType(expr, typeof(bool));
    }

    public override Type VisitCast(CastExpr expr)
    {
        base.VisitCast(expr);
        var resolved = _context.TypeResolver.TryResolveType(expr.TargetType.Lexeme);
        return SetType(expr, resolved ?? typeof(object));
    }

    public override Type VisitIsPattern(IsPatternExpr expr)
    {
        base.VisitIsPattern(expr);
        return SetType(expr, typeof(bool));
    }

    public override Type VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        base.VisitSwitchExpression(expr);
        // Switch expression result type is determined at runtime
        return SetType(expr, typeof(object));
    }

    public override Type VisitAs(AsExpr expr)
    {
        base.VisitAs(expr);
        var resolved = _context.TypeResolver.TryResolveType(expr.TargetType.Lexeme);
        return SetType(expr, resolved ?? typeof(object));
    }

    public override Type VisitAssign(AssignExpr expr)
    {
        base.VisitAssign(expr);
        return SetType(expr, GetInferredType(expr.Value));
    }

    public override Type VisitNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        base.VisitNullCoalesceAssign(expr);
        return SetType(expr, GetInferredType(expr.Value));
    }

    public override Type VisitCompoundAssign(CompoundAssignExpr expr)
    {
        base.VisitCompoundAssign(expr);
        if (TryGetVariableType(expr.Name.Lexeme, out var varType) && varType != null)
            return SetType(expr, varType);
        return SetType(expr, GetInferredType(expr.Value));
    }

    public override Type VisitIncrementDecrement(IncrementDecrementExpr expr)
    {
        base.VisitIncrementDecrement(expr);
        if (TryGetVariableType(expr.Name.Lexeme, out var varType) && varType != null)
            return SetType(expr, varType);
        return SetType(expr, typeof(int));
    }

    public override Type VisitIndexAssign(IndexAssignExpr expr)
    {
        base.VisitIndexAssign(expr);
        return SetType(expr, GetInferredType(expr.Value));
    }

    public override Type VisitMemberAssign(MemberAssignExpr expr)
    {
        base.VisitMemberAssign(expr);
        return SetType(expr, GetInferredType(expr.Value));
    }

    public override Type VisitNullCoalesce(NullCoalesceExpr expr)
    {
        base.VisitNullCoalesce(expr);
        var leftType = GetInferredType(expr.Left);
        var rightType = GetInferredType(expr.Right);
        // If either side is a sentinel, prefer the non-sentinel type
        if (rightType == ThrowSentinel || rightType == NullSentinel)
            return SetType(expr, ResolveSentinel(leftType));
        return SetType(expr, rightType);
    }

    public override Type VisitConditional(ConditionalExpr expr)
    {
        base.VisitConditional(expr);
        var thenType = GetInferredType(expr.ThenBranch);
        var elseType = GetInferredType(expr.ElseBranch);
        return SetType(expr, GetCommonType(thenType, elseType));
    }

    public override Type VisitArrayLiteral(ArrayLiteralExpr expr)
    {
        base.VisitArrayLiteral(expr);
        return SetType(expr, typeof(object[]));
    }

    public override Type VisitObjectLiteral(ObjectLiteralExpr expr)
    {
        base.VisitObjectLiteral(expr);
        return SetType(expr, typeof(IDictionary<string, object?>));
    }

    public override Type VisitInterpolatedString(InterpolatedStringExpr expr)
    {
        base.VisitInterpolatedString(expr);
        return SetType(expr, typeof(string));
    }

    public override Type VisitNew(NewExpr expr)
    {
        base.VisitNew(expr);
        return SetType(expr, GetInferredType(expr.Initializer));
    }

    public override Type VisitBlock(BlockExpr expr)
    {
        PushScope();
        try
        {
            foreach (var stmt in expr.Statements)
                Visit(stmt);

            var type = expr.ReturnExpr != null ? Visit(expr.ReturnExpr) : typeof(object);
            return SetType(expr, type);
        }
        finally
        {
            PopScope();
        }
    }

    public override Type VisitIfStatement(IfStatementExpr expr)
    {
        Visit(expr.Condition);

        PushScope();
        foreach (var stmt in expr.ThenStatements)
            Visit(stmt);
        PopScope();

        if (expr.ElseStatements != null)
        {
            PushScope();
            foreach (var stmt in expr.ElseStatements)
                Visit(stmt);
            PopScope();
        }

        return SetType(expr, typeof(object));
    }

    public override Type VisitWhile(WhileStatementExpr expr)
    {
        Visit(expr.Condition);

        PushScope();
        foreach (var stmt in expr.Body)
            Visit(stmt);
        PopScope();

        return SetType(expr, typeof(object));
    }

    public override Type VisitFor(ForStatementExpr expr)
    {
        PushScope();
        foreach (var init in expr.Initializers)
            Visit(init);
        if (expr.Condition != null)
            Visit(expr.Condition);
        foreach (var inc in expr.Increments)
            Visit(inc);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        PopScope();

        return SetType(expr, typeof(object));
    }

    public override Type VisitDoWhile(DoWhileStatementExpr expr)
    {
        PushScope();
        foreach (var stmt in expr.Body)
            Visit(stmt);
        PopScope();

        Visit(expr.Condition);
        return SetType(expr, typeof(object));
    }

    public override Type VisitForEach(ForEachStatementExpr expr)
    {
        Visit(expr.Collection);

        PushScope();
        DefineVariable(expr.VariableName.Lexeme, typeof(object));
        foreach (var stmt in expr.Body)
            Visit(stmt);
        PopScope();

        return SetType(expr, typeof(object));
    }

    public override Type VisitReturn(ReturnExpr expr)
    {
        var type = expr.Value != null ? Visit(expr.Value) : typeof(object);
        return SetType(expr, type);
    }

    public override Type VisitSwitch(SwitchStatementExpr expr)
    {
        Visit(expr.Expression);

        foreach (var caseExpr in expr.Cases)
        {
            PushScope();
            if (caseExpr.WhenGuard != null)
                Visit(caseExpr.WhenGuard);
            foreach (var stmt in caseExpr.Statements)
                Visit(stmt);
            PopScope();
        }

        return SetType(expr, typeof(object));
    }

    public override Type VisitVariableDecl(VariableDeclExpr expr)
    {
        Visit(expr.Initializer);

        Type type = expr.DeclaredType != null
            ? _context.TypeResolver.TryResolveType(expr.DeclaredType.Value.Lexeme) ?? typeof(object)
            : ResolveSentinel(GetInferredType(expr.Initializer));

        DefineVariable(expr.Name.Lexeme, type);
        return SetType(expr, type);
    }

    public override Type VisitGrouping(GroupingExpr expr)
    {
        base.VisitGrouping(expr);
        return SetType(expr, GetInferredType(expr.Expression));
    }

    public override Type VisitTypeof(TypeofExpr expr)
    {
        base.VisitTypeof(expr);
        return SetType(expr, typeof(Type));
    }

    public override Type VisitSizeof(SizeofExpr expr)
    {
        base.VisitSizeof(expr);
        return SetType(expr, typeof(int));
    }

    public override Type VisitThrow(ThrowExpr expr)
    {
        base.VisitThrow(expr);
        // Throw expressions have no type (never produce a value).
        // Use ThrowSentinel so conditional operator can adopt the other branch's type.
        return SetType(expr, ThrowSentinel);
    }

    public override Type VisitObjectCreation(ObjectCreationExpr expr)
    {
        base.VisitObjectCreation(expr);
        var resolved = _context.TypeResolver.TryResolveType(expr.TypeName);
        return SetType(expr, resolved ?? typeof(object));
    }

    public override Type VisitTuple(TupleExpr expr)
    {
        base.VisitTuple(expr);
        // The exact ValueTuple generic type cannot be determined statically
        // since element types depend on runtime values. Use typeof(object) as placeholder.
        return SetType(expr, typeof(object));
    }

    public override Type VisitDeconstruction(DeconstructionExpr expr)
    {
        Visit(expr.ValueExpression);
        // Deconstruction is a statement-like expression; the individual variable types
        // are determined at runtime via ITuple element access.
        foreach (var name in expr.VariableNames)
            DefineVariable(name, typeof(object));
        return SetType(expr, typeof(object));
    }

    public override Type VisitTryCatchFinally(TryCatchFinallyExpr expr)
    {
        foreach (var stmt in expr.TryBody)
            Visit(stmt);
        foreach (var catchClause in expr.CatchClauses)
        {
            PushScope();
            if (catchClause is { VariableName: not null, ExceptionTypeName: not null })
            {
                var catchType = _context.TypeResolver.TryResolveType(catchClause.ExceptionTypeName) ?? typeof(Exception);
                DefineVariable(catchClause.VariableName.Value.Lexeme, catchType);
            }
            if (catchClause.WhenGuard != null)
                Visit(catchClause.WhenGuard);
            foreach (var stmt in catchClause.Body)
                Visit(stmt);
            PopScope();
        }
        if (expr.FinallyBody != null)
        {
            foreach (var stmt in expr.FinallyBody)
                Visit(stmt);
        }
        return SetType(expr, typeof(object));
    }

    public override Type VisitThrowStatement(ThrowStatementExpr expr)
    {
        return SetType(expr, ThrowSentinel);
    }

    public override Type VisitUsingStatement(UsingStatementExpr expr)
    {
        Visit(expr.ResourceDeclaration);
        PushScope();
        try
        {
            var bodyType = Visit(expr.Body);
            return SetType(expr, bodyType);
        }
        finally
        {
            PopScope();
        }
    }

    public override Type VisitLockStatement(LockStatementExpr expr)
    {
        Visit(expr.LockObject);
        PushScope();
        try
        {
            var bodyType = Visit(expr.Body);
            return SetType(expr, bodyType);
        }
        finally
        {
            PopScope();
        }
    }

    public override Type VisitMultiDimIndexAccess(MultiDimIndexAccessExpr expr)
    {
        base.VisitMultiDimIndexAccess(expr);
        return SetType(expr, typeof(object));
    }

    public override Type VisitMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr)
    {
        base.VisitMultiDimTypedArrayCreation(expr);
        return SetType(expr, typeof(Array));
    }

    public override Type VisitMultiDimIndexAssign(MultiDimIndexAssignExpr expr)
    {
        base.VisitMultiDimIndexAssign(expr);
        return SetType(expr, GetInferredType(expr.Value));
    }

    #endregion

    #region Type Helpers

    /// <summary>
    /// Maps sentinel types back to typeof(object) so they don't leak
    /// into variable declarations or other non-conditional contexts.
    /// </summary>
    private static Type ResolveSentinel(Type type) =>
        type == ThrowSentinel || type == NullSentinel ? typeof(object) : type;

    private static Type GetBinaryResultType(Type left, Type right)
    {
        // Handle string concatenation
        if (left == typeof(string) || right == typeof(string))
            return typeof(string);

        // Non-numeric types
        if (!TypeHelpers.IsArithmetic(left) || !TypeHelpers.IsArithmetic(right))
            return typeof(object);

        // Delegate to ECMA-334 rules
        return NumericDispatch.GetResultType(left, right);
    }

    private static Type GetCommonType(Type a, Type b)
    {
        if (a == b) return a;

        // Throw expression adopts the other branch's type
        if (a == ThrowSentinel) return b == NullSentinel ? typeof(object) : b;
        if (b == ThrowSentinel) return a == NullSentinel ? typeof(object) : a;

        // Null literal: result is the non-null type (for reference types)
        if (a == NullSentinel)
            return b == NullSentinel ? typeof(object) : b;
        if (b == NullSentinel)
            return a;

        // Numeric promotion (ECMA-334 §12.4.7.3)
        if (TypeHelpers.IsArithmetic(a) && TypeHelpers.IsArithmetic(b))
            return NumericDispatch.GetResultType(a, b);

        // Reference type hierarchy
        if (a.IsAssignableFrom(b)) return a;
        if (b.IsAssignableFrom(a)) return b;

        // String concatenation compatibility
        if (a == typeof(string) || b == typeof(string))
            return typeof(string);

        return typeof(object); // fallback
    }

    #endregion
}
