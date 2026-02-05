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
        return SetType(expr, expr.Value?.GetType() ?? typeof(object));
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
        return SetType(expr, TypeHelpers.ResolveTypeName(expr.TargetType.Lexeme));
    }

    public override Type VisitIs(IsExpr expr)
    {
        base.VisitIs(expr);
        return SetType(expr, typeof(bool));
    }

    public override Type VisitAs(AsExpr expr)
    {
        base.VisitAs(expr);
        return SetType(expr, TypeHelpers.ResolveTypeName(expr.TargetType.Lexeme));
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
        return SetType(expr, GetInferredType(expr.Right));
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
        return SetType(expr, typeof(List<object?>));
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
        if (expr.Initializer != null)
            Visit(expr.Initializer);
        if (expr.Condition != null)
            Visit(expr.Condition);
        if (expr.Increment != null)
            Visit(expr.Increment);
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
            if (caseExpr.Pattern != null)
                Visit(caseExpr.Pattern);

            PushScope();
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
            ? TypeHelpers.ResolveTypeName(expr.DeclaredType.Value.Lexeme)
            : GetInferredType(expr.Initializer);

        DefineVariable(expr.Name.Lexeme, type);
        return SetType(expr, type);
    }

    public override Type VisitGrouping(GroupingExpr expr)
    {
        base.VisitGrouping(expr);
        return SetType(expr, GetInferredType(expr.Expression));
    }

    #endregion

    #region Type Helpers

    private static Type GetBinaryResultType(Type left, Type right)
    {
        // Handle string concatenation
        if (left == typeof(string) || right == typeof(string))
            return typeof(string);

        // Non-numeric types
        if (!IsNumericType(left) || !IsNumericType(right))
            return typeof(object);

        // Delegate to ECMA-334 rules
        return NumericDispatch.GetResultType(left, right);
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(decimal) || type == typeof(byte) ||
        type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong) || type == typeof(char);

    private static Type GetCommonType(Type a, Type b)
    {
        if (a == b) return a;
        if (a == typeof(object)) return b;
        if (b == typeof(object)) return a;
        return GetBinaryResultType(a, b);
    }

    #endregion
}
