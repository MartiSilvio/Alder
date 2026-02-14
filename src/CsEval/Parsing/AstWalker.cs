namespace CsEval.Parsing;

/// <summary>
/// Base class for AST visitors that need to traverse all nodes.
/// Handles traversal automatically - subclasses just override to add behavior.
/// </summary>
public abstract class AstWalker<T> : IExprVisitor<T>
{
    protected abstract T DefaultValue { get; }

    /// <summary>
    /// Called before visiting children. Override to add pre-processing.
    /// </summary>
    protected virtual void OnEnter(Expr expr) { }

    /// <summary>
    /// Called after visiting children. Override to compute result from children.
    /// </summary>
    protected virtual T OnLeave(Expr expr) => DefaultValue;

    protected T Visit(Expr expr) => expr.Accept(this);

    // Literals
    public virtual T VisitLiteral(LiteralExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Identifiers & Access
    public virtual T VisitIdentifier(IdentifierExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    public virtual T VisitMemberAccess(MemberAccessExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        return OnLeave(expr);
    }

    public virtual T VisitIndexAccess(IndexAccessExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Index);
        return OnLeave(expr);
    }

    public virtual T VisitTypeReference(TypeReferenceExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Operators
    public virtual T VisitUnary(UnaryExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Right);
        return OnLeave(expr);
    }

    public virtual T VisitBinary(BinaryExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Left);
        Visit(expr.Right);
        return OnLeave(expr);
    }

    public virtual T VisitLogical(LogicalExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Left);
        Visit(expr.Right);
        return OnLeave(expr);
    }

    public virtual T VisitCast(CastExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        return OnLeave(expr);
    }

    public virtual T VisitIsPattern(IsPatternExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        WalkPattern(expr.Pattern);
        return OnLeave(expr);
    }

    public virtual T VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        foreach (var arm in expr.Arms)
        {
            WalkPattern(arm.Pattern);
            if (arm.WhenGuard != null)
                Visit(arm.WhenGuard);
            Visit(arm.Value);
        }
        return OnLeave(expr);
    }

    /// <summary>
    /// Recursively visits any Expr nodes within a Pattern tree.
    /// </summary>
    protected void WalkPattern(Pattern pattern)
    {
        switch (pattern)
        {
            case ConstantPattern cp:
                Visit(cp.Value);
                break;
            case RelationalPattern rp:
                Visit(rp.Operand);
                break;
            case AndPattern ap:
                WalkPattern(ap.Left);
                WalkPattern(ap.Right);
                break;
            case OrPattern op:
                WalkPattern(op.Left);
                WalkPattern(op.Right);
                break;
            case NotPattern np:
                WalkPattern(np.Operand);
                break;
            case ParenthesizedPattern pp:
                WalkPattern(pp.Inner);
                break;
            case PropertyPattern prop:
                foreach (var (_, subPattern) in prop.Properties)
                    WalkPattern(subPattern);
                break;
            // TypePattern, VarPattern, DiscardPattern have no Expr children
        }
    }

    public virtual T VisitAs(AsExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        return OnLeave(expr);
    }

    // Assignment
    public virtual T VisitAssign(AssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitCompoundAssign(CompoundAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitMemberCompoundAssign(MemberCompoundAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitIndexCompoundAssign(IndexCompoundAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Index);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitIncrementDecrement(IncrementDecrementExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    public virtual T VisitMemberIncrement(MemberIncrementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        return OnLeave(expr);
    }

    public virtual T VisitIndexIncrement(IndexIncrementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Index);
        return OnLeave(expr);
    }

    public virtual T VisitMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Index);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitIndexAssign(IndexAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Index);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitMemberAssign(MemberAssignExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Object);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    // Null handling & Conditionals
    public virtual T VisitNullCoalesce(NullCoalesceExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Left);
        Visit(expr.Right);
        return OnLeave(expr);
    }

    public virtual T VisitConditional(ConditionalExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Condition);
        Visit(expr.ThenBranch);
        Visit(expr.ElseBranch);
        return OnLeave(expr);
    }

    // Functions & Lambdas
    public virtual T VisitCall(CallExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Callee);
        foreach (var arg in expr.Arguments)
            Visit(arg);
        return OnLeave(expr);
    }

    public virtual T VisitNamedArgument(NamedArgumentExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitLambda(LambdaExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Body);
        return OnLeave(expr);
    }

    public virtual T VisitArrayLiteral(ArrayLiteralExpr expr)
    {
        OnEnter(expr);
        foreach (var elem in expr.Elements)
            Visit(elem);
        return OnLeave(expr);
    }

    public virtual T VisitObjectLiteral(ObjectLiteralExpr expr)
    {
        OnEnter(expr);
        foreach (var (_, value) in expr.Properties)
            Visit(value);
        return OnLeave(expr);
    }

    public virtual T VisitSpread(SpreadExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        return OnLeave(expr);
    }

    public virtual T VisitInterpolatedString(InterpolatedStringExpr expr)
    {
        OnEnter(expr);
        foreach (var part in expr.Parts)
        {
            if (part is ExpressionPart ep)
                Visit(ep.Expression);
        }
        return OnLeave(expr);
    }

    public virtual T VisitNew(NewExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Initializer);
        return OnLeave(expr);
    }

    // Control Flow
    public virtual T VisitBlock(BlockExpr expr)
    {
        OnEnter(expr);
        foreach (var stmt in expr.Statements)
            Visit(stmt);
        if (expr.ReturnExpr != null)
            Visit(expr.ReturnExpr);
        return OnLeave(expr);
    }

    public virtual T VisitIfStatement(IfStatementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Condition);
        foreach (var stmt in expr.ThenStatements)
            Visit(stmt);
        if (expr.ElseStatements != null)
        {
            foreach (var stmt in expr.ElseStatements)
                Visit(stmt);
        }
        return OnLeave(expr);
    }

    public virtual T VisitWhile(WhileStatementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Condition);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        return OnLeave(expr);
    }

    public virtual T VisitFor(ForStatementExpr expr)
    {
        OnEnter(expr);
        foreach (var init in expr.Initializers)
            Visit(init);
        if (expr.Condition != null)
            Visit(expr.Condition);
        foreach (var inc in expr.Increments)
            Visit(inc);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        return OnLeave(expr);
    }

    public virtual T VisitDoWhile(DoWhileStatementExpr expr)
    {
        OnEnter(expr);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        Visit(expr.Condition);
        return OnLeave(expr);
    }

    public virtual T VisitForEach(ForEachStatementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Collection);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        return OnLeave(expr);
    }

    public virtual T VisitBreak(BreakExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    public virtual T VisitContinue(ContinueExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    public virtual T VisitReturn(ReturnExpr expr)
    {
        OnEnter(expr);
        if (expr.Value != null)
            Visit(expr.Value);
        return OnLeave(expr);
    }

    public virtual T VisitSwitch(SwitchStatementExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        foreach (var caseExpr in expr.Cases)
        {
            if (caseExpr.WhenGuard != null)
                Visit(caseExpr.WhenGuard);
            foreach (var stmt in caseExpr.Statements)
                Visit(stmt);
        }
        return OnLeave(expr);
    }

    // Declarations
    public virtual T VisitVariableDecl(VariableDeclExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Initializer);
        return OnLeave(expr);
    }

    // Grouping
    public virtual T VisitGrouping(GroupingExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        return OnLeave(expr);
    }

    // Default Expression
    public virtual T VisitDefault(DefaultExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Nameof Expression
    public virtual T VisitNameof(NameofExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Typeof Expression
    public virtual T VisitTypeof(TypeofExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Sizeof Expression
    public virtual T VisitSizeof(SizeofExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }

    // Throw Expression
    public virtual T VisitThrow(ThrowExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Expression);
        return OnLeave(expr);
    }

    // Object Creation Expression
    public virtual T VisitObjectCreation(ObjectCreationExpr expr)
    {
        OnEnter(expr);
        foreach (var arg in expr.Arguments)
            Visit(arg);
        return OnLeave(expr);
    }

    // Typed Array Creation Expression
    public virtual T VisitTypedArrayCreation(TypedArrayCreationExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Size);
        return OnLeave(expr);
    }

    public virtual T VisitTypedArrayLiteral(TypedArrayLiteralExpr expr)
    {
        OnEnter(expr);
        Visit(expr.Elements);
        return OnLeave(expr);
    }

    // Tuple Expression
    public virtual T VisitTuple(TupleExpr expr)
    {
        OnEnter(expr);
        foreach (var element in expr.Elements)
            Visit(element.Expression);
        return OnLeave(expr);
    }

    // Deconstruction Expression
    public virtual T VisitDeconstruction(DeconstructionExpr expr)
    {
        OnEnter(expr);
        Visit(expr.ValueExpression);
        return OnLeave(expr);
    }

    // Exception Handling
    public virtual T VisitTryCatchFinally(TryCatchFinallyExpr expr)
    {
        OnEnter(expr);
        foreach (var stmt in expr.TryBody)
            Visit(stmt);
        foreach (var catchClause in expr.CatchClauses)
        {
            if (catchClause.WhenGuard != null)
                Visit(catchClause.WhenGuard);
            foreach (var stmt in catchClause.Body)
                Visit(stmt);
        }
        if (expr.FinallyBody != null)
        {
            foreach (var stmt in expr.FinallyBody)
                Visit(stmt);
        }
        return OnLeave(expr);
    }

    public virtual T VisitThrowStatement(ThrowStatementExpr expr)
    {
        OnEnter(expr);
        return OnLeave(expr);
    }
}
