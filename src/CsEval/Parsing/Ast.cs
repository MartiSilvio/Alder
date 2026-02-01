namespace CsEval.Parsing;

public abstract record Expr
{
    public abstract T Accept<T>(IExprVisitor<T> visitor);
}

public interface IExprVisitor<out T>
{
    // Literals
    T VisitLiteral(LiteralExpr expr);

    // Identifiers & Access
    T VisitIdentifier(IdentifierExpr expr);
    T VisitMemberAccess(MemberAccessExpr expr);
    T VisitIndexAccess(IndexAccessExpr expr);

    // Operators
    T VisitUnary(UnaryExpr expr);
    T VisitBinary(BinaryExpr expr);
    T VisitLogical(LogicalExpr expr);
    T VisitCast(CastExpr expr);
    T VisitIs(IsExpr expr);
    T VisitAs(AsExpr expr);

    // Assignment
    T VisitAssign(AssignExpr expr);
    T VisitNullCoalesceAssign(NullCoalesceAssignExpr expr);
    T VisitCompoundAssign(CompoundAssignExpr expr);
    T VisitIncrementDecrement(IncrementDecrementExpr expr);
    T VisitIndexAssign(IndexAssignExpr expr);
    T VisitMemberAssign(MemberAssignExpr expr);

    // Null handling & Conditionals
    T VisitNullCoalesce(NullCoalesceExpr expr);
    T VisitConditional(ConditionalExpr expr);

    // Functions & Lambdas
    T VisitCall(CallExpr expr);
    T VisitNamedArgument(NamedArgumentExpr expr);
    T VisitLambda(LambdaExpr expr);

    // Collections & Literals
    T VisitArrayLiteral(ArrayLiteralExpr expr);
    T VisitObjectLiteral(ObjectLiteralExpr expr);
    T VisitSpread(SpreadExpr expr);
    T VisitInterpolatedString(InterpolatedStringExpr expr);
    T VisitNew(NewExpr expr);

    // Control Flow
    T VisitBlock(BlockExpr expr);
    T VisitIfStatement(IfStatementExpr expr);
    T VisitWhile(WhileStatementExpr expr);
    T VisitFor(ForStatementExpr expr);
    T VisitDoWhile(DoWhileStatementExpr expr);
    T VisitForEach(ForEachStatementExpr expr);
    T VisitBreak(BreakExpr expr);
    T VisitContinue(ContinueExpr expr);
    T VisitReturn(ReturnExpr expr);
    T VisitSwitch(SwitchStatementExpr expr);

    // Declarations
    T VisitVariableDecl(VariableDeclExpr expr);

    // Grouping
    T VisitGrouping(GroupingExpr expr);
}

#region Literals

// Literals: 42, "hello", true, null
public sealed record LiteralExpr(object? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLiteral(this);
}

#endregion

#region Identifiers & Access

// Identifier: foo, bar
public sealed record IdentifierExpr(Token Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIdentifier(this);
}

// Member access: obj.Property, obj?.Property
public sealed record MemberAccessExpr(Expr Object, Token Name, bool NullSafe) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAccess(this);
}

// Index access: arr[0], dict["key"]
public sealed record IndexAccessExpr(Expr Object, Expr Index) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAccess(this);
}

#endregion

#region Operators

// Unary: -x, !x, ~x
public sealed record UnaryExpr(Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitUnary(this);
}

// Binary: x + y, x * y, x == y, etc.
public sealed record BinaryExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBinary(this);
}

// Logical: x && y, x || y
public sealed record LogicalExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLogical(this);
}

// Cast: (int)x, (double)y
public sealed record CastExpr(Token TargetType, Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCast(this);
}

// Is: x is string, x is null, x is not null, x is string s
// TargetType is null for "is null" / "is not null" patterns
// VariableName is set for declaration patterns like "x is string s"
public sealed record IsExpr(Expr Expression, Token? TargetType, bool IsNegated, Token? VariableName = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIs(this);
}

// As: x as string (safe cast)
public sealed record AsExpr(Expr Expression, Token TargetType) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAs(this);
}

#endregion

#region Assignment

// Assignment: x = y
public sealed record AssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAssign(this);
}

// Null coalesce assignment: x ??= y
public sealed record NullCoalesceAssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesceAssign(this);
}

// Compound assignment: x += y, x -= y, x *= y, etc.
public sealed record CompoundAssignExpr(Token Name, Token Op, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCompoundAssign(this);
}

// Increment/Decrement: ++x, x++, --x, x--
public sealed record IncrementDecrementExpr(Token Name, Token Op, bool IsPrefix) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIncrementDecrement(this);
}

// Index assignment: arr[0] = value, dict["key"] = value
public sealed record IndexAssignExpr(Expr Object, Expr Index, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAssign(this);
}

// Member assignment: obj.Property = value
public sealed record MemberAssignExpr(Expr Object, Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAssign(this);
}

#endregion

#region Null Handling & Conditionals

// Null coalesce: x ?? y
public sealed record NullCoalesceExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesce(this);
}

// Conditional: condition ? thenBranch : elseBranch
public sealed record ConditionalExpr(Expr Condition, Expr ThenBranch, Expr ElseBranch) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitConditional(this);
}

#endregion

#region Functions & Lambdas

// Function/method call: func(args), obj.Method(args)
public sealed record CallExpr(Expr Callee, List<Expr> Arguments) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCall(this);
}

// Named argument: name: value (used in method calls)
public sealed record NamedArgumentExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNamedArgument(this);
}

// Lambda: (x) => x * 2, (a, b) => a + b
public sealed record LambdaExpr(List<Token> Parameters, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLambda(this);
}

#endregion

#region Collections & Literals

// Array literal: [1, 2, 3]
public sealed record ArrayLiteralExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
}

// Object literal: new { Name = "John", Age = 30 }
public sealed record ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectLiteral(this);
}

// Spread expression: ...expr (used in arrays and objects)
public sealed record SpreadExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSpread(this);
}

// Interpolated string: $"Hello {name}"
public sealed record InterpolatedStringExpr(List<InterpolatedPart> Parts) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitInterpolatedString(this);
}

public abstract record InterpolatedPart;
public sealed record TextPart(string Text) : InterpolatedPart;
public sealed record ExpressionPart(Expr Expression) : InterpolatedPart;

// New expression: new { Name = "John" }
public sealed record NewExpr(Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNew(this);
}

#endregion

#region Control Flow

// Block: { var x = 1; var y = 2; return x + y; }
public sealed record BlockExpr(List<Expr> Statements, Expr? ReturnExpr) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBlock(this);
}

// If statement: if (cond) { ... } else { ... }
public sealed record IfStatementExpr(Expr Condition, List<Expr> ThenStatements, List<Expr>? ElseStatements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIfStatement(this);
}

// While statement: while (cond) { ... }
public sealed record WhileStatementExpr(Expr Condition, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitWhile(this);
}

// For statement: for (init; cond; incr) { ... }
public sealed record ForStatementExpr(Expr? Initializer, Expr? Condition, Expr? Increment, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitFor(this);
}

// Do-while statement: do { ... } while (cond);
public sealed record DoWhileStatementExpr(List<Expr> Body, Expr Condition) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDoWhile(this);
}

// Foreach statement: foreach (var item in collection) { ... }
public sealed record ForEachStatementExpr(Token VariableName, Expr Collection, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitForEach(this);
}

// Break statement: break;
public sealed record BreakExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBreak(this);
}

// Continue statement: continue;
public sealed record ContinueExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitContinue(this);
}

// Return statement: return expr;
public sealed record ReturnExpr(Expr? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitReturn(this);
}

// Switch case: case pattern: statements or default: statements
// Pattern is null for default case
public sealed record SwitchCaseExpr(Expr? Pattern, List<Expr> Statements);

// Switch statement: switch (expr) { case 1: ... default: ... }
public sealed record SwitchStatementExpr(Expr Expression, List<SwitchCaseExpr> Cases) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitch(this);
}

#endregion

#region Declarations

// Variable declaration: var x = 5 or int x = 5
public sealed record VariableDeclExpr(Token? DeclaredType, Token Name, Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

#endregion

#region Grouping

// Grouping: (expr)
public sealed record GroupingExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGrouping(this);
}

#endregion
