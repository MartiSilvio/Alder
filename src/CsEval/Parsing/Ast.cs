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
    T VisitTypeReference(TypeReferenceExpr expr);

    // Operators
    T VisitUnary(UnaryExpr expr);
    T VisitBinary(BinaryExpr expr);
    T VisitLogical(LogicalExpr expr);
    T VisitCast(CastExpr expr);
    T VisitIsPattern(IsPatternExpr expr);
    T VisitAs(AsExpr expr);

    // Pattern Matching
    T VisitSwitchExpression(SwitchExpressionExpr expr);

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

    // Collection Literals and Spread
    T VisitArrayLiteral(ArrayLiteralExpr expr);
    T VisitObjectLiteral(ObjectLiteralExpr expr);
    T VisitSpread(SpreadExpr expr);

    // Collections & Literals
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

    // Default expression
    T VisitDefault(DefaultExpr expr);

    // Nameof expression
    T VisitNameof(NameofExpr expr);

    // Typeof expression
    T VisitTypeof(TypeofExpr expr);

    // Object creation expression
    T VisitObjectCreation(ObjectCreationExpr expr);

    // Throw expression
    T VisitThrow(ThrowExpr expr);

    // Tuple expression
    T VisitTuple(TupleExpr expr);

    // Deconstruction expression
    T VisitDeconstruction(DeconstructionExpr expr);

    // Exception handling
    T VisitTryCatchFinally(TryCatchFinallyExpr expr);
    T VisitThrowStatement(ThrowStatementExpr expr);
}

#region Literals

// Literals: 42, "hello", true, null
// IsConstant: true for parsed literal tokens, enabling ECMA-334 §10.2.11
// implicit constant expression conversions in binary numeric promotion.
public sealed record LiteralExpr(object? Value, bool IsConstant = false) : Expr
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

// Index access: arr[0], dict["key"], arr?[0] (null-safe)
public sealed record IndexAccessExpr(Expr Object, Expr Index, bool NullSafe = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAccess(this);
}

// Type reference for static member access: double.NaN, int.MaxValue
public sealed record TypeReferenceExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeReference(this);
}

#endregion

#region Operators

// Unary: -x, +x, !x, ~x
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

// Is pattern: x is <pattern> -- unified pattern matching via Pattern hierarchy
// ECMA-334 §11.2: all 'is' expressions use patterns
public sealed record IsPatternExpr(Expr Expression, Pattern Pattern) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIsPattern(this);
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

// Function/method call: func(args), obj.Method(args), obj.Method<T>(args)
// TypeArguments contains the type names for generic method calls (e.g., ["int", "string"])
public sealed record CallExpr(Expr Callee, List<Expr> Arguments, List<string>? TypeArguments = null) : Expr
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

#region Collection Literals and Spread

// Array literal: [1, 2, 3]
public sealed record ArrayLiteralExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
}

// Object literal: new { Name = "John", Age = 30 } - CsEval extension (ExpandoObject, not C# anonymous types)
public sealed record ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectLiteral(this);
}

// Spread expression: ...expr (used in arrays and objects) - CsEval extension
public sealed record SpreadExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSpread(this);
}

#endregion

#region Collections & Literals

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

#region Default Expression

// Default expression: default(T) or default
// TypeToken is null for bare default literal (C# 7.1+)
public sealed record DefaultExpr(Token? TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDefault(this);
}

#endregion

#region Typeof Expression

// Typeof expression: typeof(int), typeof(string), typeof(void)
// ECMA-334 §12.8.17 - The typeof operator
public sealed record TypeofExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeof(this);
}

#endregion

#region Object Creation Expression

// Object creation expression: new Exception("msg"), new ArgumentException("msg", "param")
// ECMA-334 §12.8.16.2 - Object creation expressions
public sealed record ObjectCreationExpr(string TypeName, List<Expr> Arguments) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectCreation(this);
}

#endregion

#region Throw Expression

// Throw expression: throw new Exception("msg")
// ECMA-334 §12.16 - Throw expression operator
public sealed record ThrowExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrow(this);
}

#endregion

#region Exception Handling

// Catch clause data record (not an Expr -- similar to SwitchArm)
// ECMA-334 §13.11 - The try statement
public sealed record CatchClause(
    string? ExceptionTypeName,
    Token? VariableName,
    Expr? WhenGuard,
    List<Expr> Body);

// Try/catch/finally statement: try { } catch (T e) when (guard) { } finally { }
// ECMA-334 §13.11 - The try statement
public sealed record TryCatchFinallyExpr(
    List<Expr> TryBody,
    List<CatchClause> CatchClauses,
    List<Expr>? FinallyBody) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTryCatchFinally(this);
}

// Parameterless throw statement: throw;
// ECMA-334 §13.10.6 - The throw statement (rethrow)
public sealed record ThrowStatementExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrowStatement(this);
}

#endregion

#region Nameof Expression

// Nameof expression: nameof(identifier), nameof(obj.Property)
// Name is the final identifier name (e.g., "Property" for obj.Property)
public sealed record NameofExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNameof(this);
}

#endregion

#region Tuple Expression

// Tuple element: optionally named expression within a tuple (e.g., "x: 1" or just "1")
public record TupleElement(string? Name, Expr Expression);

// Tuple expression: (expr1, expr2, ...) creates a System.ValueTuple
// ECMA-334 §12.8.6 - Tuple expressions
public sealed record TupleExpr(List<TupleElement> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTuple(this);
}

#endregion

#region Deconstruction Expression

// Deconstruction expression: var (x, y) = tupleExpr
// ECMA-334 §12.7 - Deconstruction
public sealed record DeconstructionExpr(List<string> VariableNames, Expr ValueExpression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDeconstruction(this);
}

#endregion

#region Pattern Matching

// ECMA-334 §11.2 - Pattern matching
// Abstract base for all pattern types in the pattern matching hierarchy
public abstract record Pattern;

// ECMA-334 §11.2.1 - Constant pattern: matches a constant value (null, true, false, literals)
public sealed record ConstantPattern(Expr Value) : Pattern;

// ECMA-334 §11.2.2 - Type pattern: matches a type, optionally binding to a variable
public sealed record TypePattern(Token TypeToken, Token? VariableName) : Pattern;

// ECMA-334 §11.2.3 - Relational pattern: <, <=, >, >= against a constant
public sealed record RelationalPattern(Token Operator, Expr Operand) : Pattern;

// ECMA-334 §11.2.5 - Logical patterns: and, or, not combinators
public sealed record AndPattern(Pattern Left, Pattern Right) : Pattern;
public sealed record OrPattern(Pattern Left, Pattern Right) : Pattern;
public sealed record NotPattern(Pattern Operand) : Pattern;

// ECMA-334 §11.2.6 - Property pattern: { Name: pattern, ... }
public sealed record PropertyPattern(Token? TypeToken, List<(Token Name, Pattern Pattern)> Properties, Token? VariableName) : Pattern;

// ECMA-334 §11.2.4 - Var pattern: var x (always matches, binds variable)
public sealed record VarPattern(Token VariableName) : Pattern;

// Discard pattern: _ (always matches, no binding)
public sealed record DiscardPattern : Pattern;

// Parenthesized pattern: (pattern) for grouping
public sealed record ParenthesizedPattern(Pattern Inner) : Pattern;

// Switch arm: pattern [when guard] => value
public sealed record SwitchArm(Pattern Pattern, Expr? WhenGuard, Expr Value);

// Switch expression: expr switch { arm, arm, ... }
// ECMA-334 §12.8.21 - Switch expression
public sealed record SwitchExpressionExpr(Expr Expression, List<SwitchArm> Arms) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitchExpression(this);
}

#endregion
