namespace CsEval.Parsing;

public abstract record Expr
{
    public abstract T Accept<T>(IExprVisitor<T> visitor);
}

public interface IExprVisitor<out T>
{
    T VisitLiteral(LiteralExpr expr);
    T VisitUnary(UnaryExpr expr);
    T VisitBinary(BinaryExpr expr);
    T VisitLogical(LogicalExpr expr);
    T VisitGrouping(GroupingExpr expr);
    T VisitIdentifier(IdentifierExpr expr);
    T VisitMemberAccess(MemberAccessExpr expr);
    T VisitIndexAccess(IndexAccessExpr expr);
    T VisitCall(CallExpr expr);
    T VisitLambda(LambdaExpr expr);
    T VisitConditional(ConditionalExpr expr);
    T VisitNullCoalesce(NullCoalesceExpr expr);
    T VisitNullCoalesceAssign(NullCoalesceAssignExpr expr);
    T VisitInterpolatedString(InterpolatedStringExpr expr);
    T VisitArrayLiteral(ArrayLiteralExpr expr);
    T VisitObjectLiteral(ObjectLiteralExpr expr);
    T VisitBlock(BlockExpr expr);
    T VisitVariableDecl(VariableDeclExpr expr);
    T VisitNew(NewExpr expr);
    T VisitIfStatement(IfStatementExpr expr);
    T VisitReturn(ReturnExpr expr);
    T VisitSpread(SpreadExpr expr);
}

// Literals
public sealed record LiteralExpr(object? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLiteral(this);
}

// Unary: -x, !x
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

// Grouping: (expr)
public sealed record GroupingExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGrouping(this);
}

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

// Function/method call: func(args), obj.Method(args)
public sealed record CallExpr(Expr Callee, List<Expr> Arguments) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCall(this);
}

// Lambda: (x) => x * 2, (a, b) => a + b
public sealed record LambdaExpr(List<Token> Parameters, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLambda(this);
}

// Conditional: condition ? thenBranch : elseBranch
public sealed record ConditionalExpr(Expr Condition, Expr ThenBranch, Expr ElseBranch) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitConditional(this);
}

// Null coalesce: x ?? y
public sealed record NullCoalesceExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesce(this);
}

// Null coalesce assignment: x ??= y
public sealed record NullCoalesceAssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesceAssign(this);
}

// Interpolated string: $"Hello {name}"
public sealed record InterpolatedStringExpr(List<InterpolatedPart> Parts) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitInterpolatedString(this);
}

public abstract record InterpolatedPart;

public sealed record TextPart(string Text) : InterpolatedPart;

public sealed record ExpressionPart(Expr Expression) : InterpolatedPart;

// Array literal: [1, 2, 3]
public sealed record ArrayLiteralExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
}

// Object literal: { Name: "John", Age: 30 } or { Name = "John", Age = 30 }
public sealed record ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectLiteral(this);
}

// Block: { var x = 1; var y = 2; return x + y; } or { x = 1; y = 2; x + y }
public sealed record BlockExpr(List<Expr> Statements, Expr? ReturnExpr) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBlock(this);
}

// Variable declaration: var x = 5 or int x = 5
public sealed record VariableDeclExpr(Token? DeclaredType, Token Name, Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

// New expression: new { Name = "John" }
public sealed record NewExpr(Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNew(this);
}

// If statement: if (cond) { ... } or if (cond) return x;
public sealed record IfStatementExpr(Expr Condition, List<Expr> ThenStatements, List<Expr>? ElseStatements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIfStatement(this);
}

// Return statement: return expr;
public sealed record ReturnExpr(Expr? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitReturn(this);
}

// Spread expression: ...expr (used in arrays and objects)
public sealed record SpreadExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSpread(this);
}