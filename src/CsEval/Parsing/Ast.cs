using CsEval.Text;

namespace CsEval.Parsing;

internal abstract record Expr
{
    public TextSpan Span { get; init; }
    public abstract T Accept<T>(IExprVisitor<T> visitor);
}

internal interface IExprVisitor<out T>
{
    // Literals
    T VisitLiteral(LiteralExpr expr);

    // Identifiers & Access
    T VisitIdentifier(IdentifierExpr expr);
    T VisitMemberAccess(MemberAccessExpr expr);
    T VisitIndexAccess(IndexAccessExpr expr);
    T VisitSlice(SliceExpr expr);
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
    T VisitMemberCompoundAssign(MemberCompoundAssignExpr expr);
    T VisitIndexCompoundAssign(IndexCompoundAssignExpr expr);
    T VisitIncrementDecrement(IncrementDecrementExpr expr);
    T VisitMemberIncrement(MemberIncrementExpr expr);
    T VisitIndexIncrement(IndexIncrementExpr expr);
    T VisitMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr);
    T VisitIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr);
    T VisitIndexAssign(IndexAssignExpr expr);
    T VisitMemberAssign(MemberAssignExpr expr);

    // Null handling & Conditionals
    T VisitNullCoalesce(NullCoalesceExpr expr);
    T VisitConditional(ConditionalExpr expr);

    // Functions & Lambdas
    T VisitCall(CallExpr expr);
    T VisitNamedArgument(NamedArgumentExpr expr);
    T VisitOutArg(OutArgExpr expr);
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
    T VisitGoto(GotoExpr expr);
    T VisitGotoCase(GotoCaseExpr expr);
    T VisitGotoDefault(GotoDefaultExpr expr);
    T VisitLabel(LabelExpr expr);

    // Declarations
    T VisitVariableDecl(VariableDeclExpr expr);

    // Default expression
    T VisitDefault(DefaultExpr expr);

    // Nameof expression
    T VisitNameof(NameofExpr expr);

    // Typeof expression
    T VisitTypeof(TypeofExpr expr);

    // Sizeof expression
    T VisitSizeof(SizeofExpr expr);

    // Object creation expression
    T VisitObjectCreation(ObjectCreationExpr expr);

    // Typed array creation expression
    T VisitTypedArrayCreation(TypedArrayCreationExpr expr);

    // Typed array literal expression
    T VisitTypedArrayLiteral(TypedArrayLiteralExpr expr);

    // Throw expression
    T VisitThrow(ThrowExpr expr);

    // Tuple expression
    T VisitTuple(TupleExpr expr);

    // Deconstruction expression
    T VisitDeconstruction(DeconstructionExpr expr);

    // Exception handling
    T VisitTryCatchFinally(TryCatchFinallyExpr expr);
    T VisitThrowStatement(ThrowStatementExpr expr);

    // Resource management
    T VisitUsingStatement(UsingStatementExpr expr);

    // Synchronization
    T VisitLockStatement(LockStatementExpr expr);

    // Multi-dimensional array operations
    T VisitMultiDimIndexAccess(MultiDimIndexAccessExpr expr);
    T VisitMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr);
    T VisitMultiDimArrayInit(MultiDimArrayInitExpr expr);
    T VisitMultiDimIndexAssign(MultiDimIndexAssignExpr expr);

    // Checked/Unchecked
    T VisitChecked(CheckedExpr expr);

    // Index/Range
    T VisitIndexFromEnd(IndexFromEndExpr expr);

    // Polyglot Extended Features
    T VisitRange(RangeExpr expr);
    T VisitPipeline(PipelineExpr expr);
    T VisitChainedComparison(ChainedComparisonExpr expr);
}

#region Literals

// Literals: 42, "hello", true, null
// IsConstant: true for parsed literal tokens, enabling ECMA-334 §10.2.11
// implicit constant expression conversions in binary numeric promotion.
internal sealed record LiteralExpr(object? Value, bool IsConstant = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLiteral(this);
}

#endregion

#region Identifiers & Access

// Identifier: foo, bar
internal sealed record IdentifierExpr(Token Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIdentifier(this);
}

// Member access: obj.Property, obj?.Property
internal sealed record MemberAccessExpr(Expr Object, Token Name, bool NullSafe) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAccess(this);
}

// Index access: arr[0], dict["key"], arr?[0] (null-safe)
internal sealed record IndexAccessExpr(Expr Object, Expr Index, bool NullSafe = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAccess(this);
}

// Slice access: list[1:4], arr[:3], str[2:], list[0:10:2] (Extended mode only)
internal sealed record SliceExpr(Expr Target, Expr? Start, Expr? End, Expr? Step = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSlice(this);
}

// Type reference for static member access: double.NaN, int.MaxValue
internal sealed record TypeReferenceExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeReference(this);
}

#endregion

#region Operators

// Unary: -x, +x, !x, ~x
internal sealed record UnaryExpr(Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitUnary(this);
}

// Binary: x + y, x * y, x == y, etc.
internal sealed record BinaryExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBinary(this);
}

// Logical: x && y, x || y
internal sealed record LogicalExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLogical(this);
}

// Cast: (int)x, (double)y
internal sealed record CastExpr(Token TargetType, Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCast(this);
}

// Is pattern: x is <pattern> -- unified pattern matching via Pattern hierarchy
// ECMA-334 §11.2: all 'is' expressions use patterns
internal sealed record IsPatternExpr(Expr Expression, Pattern Pattern) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIsPattern(this);
}

// As: x as string (safe cast)
internal sealed record AsExpr(Expr Expression, Token TargetType) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAs(this);
}

#endregion

#region Assignment

// Assignment: x = y
internal sealed record AssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAssign(this);
}

// Null coalesce assignment: x ??= y
internal sealed record NullCoalesceAssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesceAssign(this);
}

// Compound assignment: x += y, x -= y, x *= y, etc.
internal sealed record CompoundAssignExpr(Token Name, Token Op, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCompoundAssign(this);
}

// ECMA-334 §12.21.4 - Compound assignment on member access: obj.Prop += value
internal sealed record MemberCompoundAssignExpr(Expr Object, string MemberName, TokenType Operator, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberCompoundAssign(this);
}

// ECMA-334 §12.21.4 - Compound assignment on index access: arr[i] += value
internal sealed record IndexCompoundAssignExpr(Expr Object, Expr Index, TokenType Operator, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexCompoundAssign(this);
}

// Increment/Decrement: ++x, x++, --x, x--
internal sealed record IncrementDecrementExpr(Token Name, Token Op, bool IsPrefix) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIncrementDecrement(this);
}

// ECMA-334 §12.8.15/§12.9.6 - Increment/decrement on member access: obj.Count++, ++obj.Count
internal sealed record MemberIncrementExpr(Expr Object, string MemberName, bool IsPrefix, bool IsIncrement) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberIncrement(this);
}

// ECMA-334 §12.8.15/§12.9.6 - Increment/decrement on index access: arr[i]++, ++arr[i]
internal sealed record IndexIncrementExpr(Expr Object, Expr Index, bool IsPrefix, bool IsIncrement) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexIncrement(this);
}

// ECMA-334 §12.21.5 - Null-coalescing assignment on member access: obj.Prop ??= default
internal sealed record MemberNullCoalesceAssignExpr(Expr Object, string MemberName, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberNullCoalesceAssign(this);
}

// ECMA-334 §12.21.5 - Null-coalescing assignment on index access: dict[key] ??= default
internal sealed record IndexNullCoalesceAssignExpr(Expr Object, Expr Index, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexNullCoalesceAssign(this);
}

// Index assignment: arr[0] = value, dict["key"] = value
internal sealed record IndexAssignExpr(Expr Object, Expr Index, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAssign(this);
}

// Member assignment: obj.Property = value
internal sealed record MemberAssignExpr(Expr Object, Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAssign(this);
}

#endregion

#region Null Handling & Conditionals

// Null coalesce: x ?? y
internal sealed record NullCoalesceExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesce(this);
}

// Conditional: condition ? thenBranch : elseBranch
internal sealed record ConditionalExpr(Expr Condition, Expr ThenBranch, Expr ElseBranch) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitConditional(this);
}

#endregion

#region Functions & Lambdas

// Function/method call: func(args), obj.Method(args), obj.Method<T>(args)
// TypeArguments contains the type names for generic method calls (e.g., ["int", "string"])
internal sealed record CallExpr(Expr Callee, List<Expr> Arguments, List<string>? TypeArguments = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCall(this);
}

// Named argument: name: value (used in method calls)
internal sealed record NamedArgumentExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNamedArgument(this);
}

// Out argument: out var x, out int x, out _ (used in method calls)
// ECMA-334 §12.6.2 - Argument lists, output parameters
internal sealed record OutArgExpr(string VariableName, string? TypeName, bool IsDiscard) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitOutArg(this);
}

// Lambda parameter: optional type annotation with name
// ECMA-334 §12.19 - Anonymous function expressions
internal sealed record LambdaParameter(string? TypeName, Token Name);

// Lambda: (x) => x * 2, (int a, int b) => a + b
internal sealed record LambdaExpr(List<LambdaParameter> Parameters, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLambda(this);
}

#endregion

#region Collection Literals and Spread

// Array literal: [1, 2, 3]
internal sealed record ArrayLiteralExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
}

// Object literal: new { Name = "John", Age = 30 } - CsEval extension (ExpandoObject, not C# anonymous types)
internal sealed record ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectLiteral(this);
}

// Spread expression: ..expr (used in arrays and objects) - CsEval extension
internal sealed record SpreadExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSpread(this);
}

#endregion

#region Collections & Literals

// Interpolated string: $"Hello {name}"
internal sealed record InterpolatedStringExpr(List<InterpolatedPart> Parts) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitInterpolatedString(this);
}

internal abstract record InterpolatedPart;
internal sealed record TextPart(string Text) : InterpolatedPart;
internal sealed record ExpressionPart(Expr Expression, string? AlignmentSpecifier = null, string? FormatSpecifier = null) : InterpolatedPart;

// New expression: new { Name = "John" }
internal sealed record NewExpr(Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNew(this);
}

#endregion

#region Control Flow

// Block: { var x = 1; var y = 2; return x + y; }
internal sealed record BlockExpr(List<Expr> Statements, Expr? ReturnExpr) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBlock(this);
}

// If statement: if (cond) { ... } else { ... }
internal sealed record IfStatementExpr(Expr Condition, List<Expr> ThenStatements, List<Expr>? ElseStatements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIfStatement(this);
}

// While statement: while (cond) { ... }
internal sealed record WhileStatementExpr(Expr Condition, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitWhile(this);
}

// For statement: for (init; cond; incr) { ... }
internal sealed record ForStatementExpr(List<Expr> Initializers, Expr? Condition, List<Expr> Increments, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitFor(this);
}

// Do-while statement: do { ... } while (cond);
internal sealed record DoWhileStatementExpr(List<Expr> Body, Expr Condition) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDoWhile(this);
}

// Foreach statement: foreach (var item in collection) { ... }
internal sealed record ForEachStatementExpr(Token VariableName, Expr Collection, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitForEach(this);
}

// Break statement: break;
internal sealed record BreakExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBreak(this);
}

// Continue statement: continue;
internal sealed record ContinueExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitContinue(this);
}

// Return statement: return expr;
internal sealed record ReturnExpr(Expr? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitReturn(this);
}

// Switch case: case pattern [when guard]: statements or default: statements
// CasePattern is null for default case
internal sealed record SwitchCaseExpr(Pattern? CasePattern, Expr? WhenGuard, List<Expr> Statements);

// Switch statement: switch (expr) { case 1: ... default: ... }
internal sealed record SwitchStatementExpr(Expr Expression, List<SwitchCaseExpr> Cases) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitch(this);
}

// ECMA-334 §13.10.4: goto label;
internal sealed record GotoExpr(string Label) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGoto(this);
}

// ECMA-334 §13.10.4: goto case expr; (in switch)
internal sealed record GotoCaseExpr(Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGotoCase(this);
}

// ECMA-334 §13.10.4: goto default; (in switch)
internal sealed record GotoDefaultExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGotoDefault(this);
}

// Label statement: label: (parsed as part of the containing block)
internal sealed record LabelExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLabel(this);
}

// ECMA-334 section 13.14 - using statement
internal sealed record UsingStatementExpr(Expr ResourceDeclaration, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitUsingStatement(this);
}

// ECMA-334 section 13.13 - lock statement
internal sealed record LockStatementExpr(Expr LockObject, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLockStatement(this);
}

#endregion

#region Declarations

// Variable declaration: var x = 5 / int x = 5 / const int x = 5
internal sealed record VariableDeclExpr(Token? DeclaredType, Token Name, Expr Initializer, bool IsConst = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

#endregion

#region Default Expression

// Default expression: default(T) or default
// TypeToken is null for bare default literal (C# 7.1+)
internal sealed record DefaultExpr(Token? TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDefault(this);
}

#endregion

#region Typeof Expression

// Typeof expression: typeof(int), typeof(string), typeof(void)
// ECMA-334 §12.8.17 - The typeof operator
internal sealed record TypeofExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeof(this);
}

#endregion

#region Sizeof Expression

// Sizeof expression: sizeof(int), sizeof(double)
// ECMA-334 §23.6.9 - The sizeof operator
internal sealed record SizeofExpr(string TypeName) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSizeof(this);
}

#endregion

#region Object Creation Expression

// Initializer entry: property assignment, indexer assignment, or collection element
// ECMA-334 §12.8.16.3 - Object initializers / §12.8.16.6 - Collection initializers
// PropertyName != null → property initializer: Name = value
// IndexerKey != null → indexer initializer: [key] = value
// Both null → collection initializer element: value
internal sealed record InitializerEntry(string? PropertyName, Expr Value, Expr? IndexerKey = null);

// Object/collection initializer block: { entries... }
internal sealed record ObjectInitializer(List<InitializerEntry> Entries);

// Object creation expression: new Exception("msg"), new ArgumentException("msg", "param")
// ECMA-334 §12.8.16.2 - Object creation expressions
// Initializer is optional - used for object/collection initializers: new X() { Prop = val }
internal sealed record ObjectCreationExpr(string TypeName, List<Expr> Arguments, ObjectInitializer? Initializer = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectCreation(this);
}

#endregion

#region Typed Array Creation Expression

// Typed array creation: new int[5], new bool[n]
// ECMA-334 §12.8.16.4 - Array creation expressions
internal sealed record TypedArrayCreationExpr(string ElementTypeName, Expr Size) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypedArrayCreation(this);
}

// Typed array literal: new int[] {1, 2, 3}, new string[] {"a", "b"}
// ECMA-334 §12.8.16.5 - Array initializers
internal sealed record TypedArrayLiteralExpr(string ElementTypeName, ArrayLiteralExpr Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypedArrayLiteral(this);
}

#endregion

#region Multi-dimensional Array Expressions

// Multi-dimensional index access: arr[i, j, k]
internal sealed record MultiDimIndexAccessExpr(Expr Object, List<Expr> Indices, bool NullSafe) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimIndexAccess(this);
}

// Multi-dimensional array creation: new int[3, 3]
internal sealed record MultiDimTypedArrayCreationExpr(string ElementTypeName, List<Expr> Sizes) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimTypedArrayCreation(this);
}

// Multi-dimensional array creation with initializer: new int[,] { {1,2}, {3,4} } or new int[2,3] { ... }
internal sealed record MultiDimArrayInitExpr(string ElementTypeName, int Rank, List<Expr>? ExplicitSizes, List<Expr> FlatValues, int[] InferredDimensions) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimArrayInit(this);
}

// Multi-dimensional index assignment: arr[i, j] = value
internal sealed record MultiDimIndexAssignExpr(Expr Object, List<Expr> Indices, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimIndexAssign(this);
}

#endregion

#region Throw Expression

// Throw expression: throw new Exception("msg")
// ECMA-334 §12.16 - Throw expression operator
internal sealed record ThrowExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrow(this);
}

#endregion

#region Exception Handling

// Catch clause data record (not an Expr -- similar to SwitchArm)
// ECMA-334 §13.11 - The try statement
internal sealed record CatchClause(
    string? ExceptionTypeName,
    Token? VariableName,
    Expr? WhenGuard,
    List<Expr> Body);

// Try/catch/finally statement: try { } catch (T e) when (guard) { } finally { }
// ECMA-334 §13.11 - The try statement
internal sealed record TryCatchFinallyExpr(
    List<Expr> TryBody,
    List<CatchClause> CatchClauses,
    List<Expr>? FinallyBody) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTryCatchFinally(this);
}

// Parameterless throw statement: throw;
// ECMA-334 §13.10.6 - The throw statement (rethrow)
internal sealed record ThrowStatementExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrowStatement(this);
}

#endregion

#region Nameof Expression

// Nameof expression: nameof(identifier), nameof(obj.Property)
// Name is the final identifier name (e.g., "Property" for obj.Property)
internal sealed record NameofExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNameof(this);
}

#endregion

#region Tuple Expression

// Tuple element: optionally named expression within a tuple (e.g., "x: 1" or just "1")
internal record TupleElement(string? Name, Expr Expression);

// Tuple expression: (expr1, expr2, ...) creates a System.ValueTuple
// ECMA-334 §12.8.6 - Tuple expressions
internal sealed record TupleExpr(List<TupleElement> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTuple(this);
}

#endregion

#region Deconstruction Expression

// Deconstruction expression: var (x, y) = tupleExpr
// ECMA-334 §12.7 - Deconstruction
internal sealed record DeconstructionExpr(List<string> VariableNames, Expr ValueExpression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDeconstruction(this);
}

#endregion

#region Checked/Unchecked Expression

internal sealed record CheckedExpr(Expr Expression, bool IsChecked) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitChecked(this);
}

#endregion

#region Polyglot Extended Features

// Range expression: start..end (exclusive, C# spec), start..=end (inclusive), start..<end (exclusive)
internal sealed record RangeExpr(Expr Start, Expr End, bool ExclusiveEnd) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitRange(this);
}

// Index from end expression: ^expr — creates System.Index(expr, fromEnd: true)
// ECMA-334 §12.8.11 — Used inside indexer arguments
internal sealed record IndexFromEndExpr(Expr Operand) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexFromEnd(this);
}

// Pipeline expression: expr |> func
internal sealed record PipelineExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitPipeline(this);
}

// Chained comparison: a < b < c desugars to a < b && b < c
internal sealed record ChainedComparisonExpr(List<Expr> Operands, List<Token> Operators) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitChainedComparison(this);
}

#endregion

#region Pattern Matching

// ECMA-334 §11.2 - Pattern matching
// Abstract base for all pattern types in the pattern matching hierarchy
internal abstract record Pattern;

// ECMA-334 §11.2.1 - Constant pattern: matches a constant value (null, true, false, literals)
internal sealed record ConstantPattern(Expr Value) : Pattern;

// ECMA-334 §11.2.2 - Type pattern: matches a type, optionally binding to a variable
internal sealed record TypePattern(Token TypeToken, Token? VariableName) : Pattern;

// ECMA-334 §11.2.3 - Relational pattern: <, <=, >, >= against a constant
internal sealed record RelationalPattern(Token Operator, Expr Operand) : Pattern;

// ECMA-334 §11.2.5 - Logical patterns: and, or, not combinators
internal sealed record AndPattern(Pattern Left, Pattern Right) : Pattern;
internal sealed record OrPattern(Pattern Left, Pattern Right) : Pattern;
internal sealed record NotPattern(Pattern Operand) : Pattern;

// ECMA-334 §11.2.6 - Property pattern: { Name: pattern, ... }
internal sealed record PropertyPattern(Token? TypeToken, List<(Token Name, Pattern Pattern)> Properties, Token? VariableName) : Pattern;

// ECMA-334 §11.2.4 - Var pattern: var x (always matches, binds variable)
internal sealed record VarPattern(Token VariableName) : Pattern;

// Discard pattern: _ (always matches, no binding)
internal sealed record DiscardPattern : Pattern;

// Parenthesized pattern: (pattern) for grouping
internal sealed record ParenthesizedPattern(Pattern Inner) : Pattern;

// Positional (tuple) pattern: (pattern1, pattern2, ...) matches ITuple elements
internal sealed record PositionalPattern(List<Pattern> Subpatterns) : Pattern;

// Switch arm: pattern [when guard] => value
internal sealed record SwitchArm(Pattern Pattern, Expr? WhenGuard, Expr Value);

// Switch expression: expr switch { arm, arm, ... }
// ECMA-334 §12.8.21 - Switch expression
internal sealed record SwitchExpressionExpr(Expr Expression, List<SwitchArm> Arms) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitchExpression(this);
}

#endregion
