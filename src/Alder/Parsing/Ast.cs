using Alder.Text;

namespace Alder.Parsing;

/// <summary>
/// Base type for parsed syntax nodes used by the Alder front-end.
/// The parser produces these records before binding lowers them to the typed bound tree.
/// </summary>
internal abstract record Expr
{
    public TextSpan Span { get; init; }
    public abstract T Accept<T>(IExprVisitor<T> visitor);
}

/// <summary>
/// Visitor contract for parsed syntax nodes.
/// Parsing, binding, rewriting, and diagnostics all share this surface instead of pattern matching every node by hand.
/// </summary>
internal interface IExprVisitor<out T>
{
    T VisitLiteral(LiteralExpr expr);

    T VisitIdentifier(IdentifierExpr expr);
    T VisitMemberAccess(MemberAccessExpr expr);
    T VisitIndexAccess(IndexAccessExpr expr);
    T VisitSlice(SliceExpr expr);
    T VisitTypeReference(TypeReferenceExpr expr);

    T VisitUnary(UnaryExpr expr);
    T VisitBinary(BinaryExpr expr);
    T VisitLogical(LogicalExpr expr);
    T VisitCast(CastExpr expr);
    T VisitIsPattern(IsPatternExpr expr);
    T VisitAs(AsExpr expr);

    T VisitSwitchExpression(SwitchExpressionExpr expr);

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

    T VisitNullCoalesce(NullCoalesceExpr expr);
    T VisitConditional(ConditionalExpr expr);

    T VisitCall(CallExpr expr);
    T VisitNamedArgument(NamedArgumentExpr expr);
    T VisitOutArg(OutArgExpr expr);
    T VisitLambda(LambdaExpr expr);

    T VisitCollectionExpr(CollectionExpr expr);
    T VisitImplicitArrayCreation(ImplicitArrayCreationExpr expr);
    T VisitObjectLiteral(ObjectLiteralExpr expr);
    T VisitWith(WithExpr expr);
    T VisitSpread(SpreadExpr expr);

    T VisitInterpolatedString(InterpolatedStringExpr expr);
    T VisitNew(NewExpr expr);

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
    T VisitYieldReturn(YieldReturnExpr expr);
    T VisitYieldBreak(YieldBreakExpr expr);

    T VisitVariableDecl(VariableDeclExpr expr);

    T VisitDefault(DefaultExpr expr);

    T VisitNameof(NameofExpr expr);

    T VisitTypeof(TypeofExpr expr);

    T VisitSizeof(SizeofExpr expr);

    T VisitObjectCreation(ObjectCreationExpr expr);

    T VisitTypedArrayCreation(TypedArrayCreationExpr expr);

    T VisitTypedArrayLiteral(TypedArrayLiteralExpr expr);

    T VisitThrow(ThrowExpr expr);

    T VisitAwait(AwaitExpr expr);

    T VisitTuple(TupleExpr expr);

    T VisitDeconstruction(DeconstructionExpr expr);

    T VisitTryCatchFinally(TryCatchFinallyExpr expr);
    T VisitThrowStatement(ThrowStatementExpr expr);

    T VisitUsingStatement(UsingStatementExpr expr);

    T VisitLockStatement(LockStatementExpr expr);

    T VisitMultiDimIndexAccess(MultiDimIndexAccessExpr expr);
    T VisitMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr);
    T VisitMultiDimArrayInit(MultiDimArrayInitExpr expr);
    T VisitMultiDimIndexAssign(MultiDimIndexAssignExpr expr);

    T VisitChecked(CheckedExpr expr);

    T VisitIndexFromEnd(IndexFromEndExpr expr);

    T VisitRange(RangeExpr expr);
    T VisitPipeline(PipelineExpr expr);
    T VisitChainedComparison(ChainedComparisonExpr expr);
}

// IsConstant distinguishes source literals from runtime values so later stages can apply
// ECMA-334 §10.2.11 implicit constant-expression conversions.
internal sealed record LiteralExpr(object? Value, bool IsConstant = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLiteral(this);
}

internal sealed record IdentifierExpr(Token Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIdentifier(this);
}

internal sealed record MemberAccessExpr(Expr Object, Token Name, bool NullSafe) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAccess(this);
}

internal sealed record IndexAccessExpr(Expr Object, Expr Index, bool NullSafe = false) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAccess(this);
}

// Slice syntax belongs to the extended language surface and binds onto Alder's runtime slice helpers.
internal sealed record SliceExpr(Expr Target, Expr? Start, Expr? End, Expr? Step = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSlice(this);
}

internal sealed record TypeReferenceExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeReference(this);
}

internal sealed record UnaryExpr(Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitUnary(this);
}

internal sealed record BinaryExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBinary(this);
}

internal sealed record LogicalExpr(Expr Left, Token Op, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLogical(this);
}

internal sealed record CastExpr(Token TargetType, Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCast(this);
}

// ECMA-334 §11.2 models every is-expression in terms of pattern matching.
internal sealed record IsPatternExpr(Expr Expression, Pattern Pattern) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIsPattern(this);
}

internal sealed record AsExpr(Expr Expression, Token TargetType) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAs(this);
}

internal sealed record AssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAssign(this);
}

internal sealed record NullCoalesceAssignExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesceAssign(this);
}

internal sealed record CompoundAssignExpr(Token Name, Token Op, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCompoundAssign(this);
}

// ECMA-334 §12.21.4 defines compound assignment as a read-modify-write on the selected member.
internal sealed record MemberCompoundAssignExpr(Expr Object, string MemberName, TokenType Operator, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberCompoundAssign(this);
}

// ECMA-334 §12.21.4 defines compound assignment as a read-modify-write on the selected element.
internal sealed record IndexCompoundAssignExpr(Expr Object, Expr Index, TokenType Operator, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexCompoundAssign(this);
}

internal sealed record IncrementDecrementExpr(Token Name, Token Op, bool IsPrefix) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIncrementDecrement(this);
}

// ECMA-334 §12.8.15 and §12.9.6 govern member increment and decrement semantics.
internal sealed record MemberIncrementExpr(Expr Object, string MemberName, bool IsPrefix, bool IsIncrement) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberIncrement(this);
}

// ECMA-334 §12.8.15 and §12.9.6 govern indexer increment and decrement semantics.
internal sealed record IndexIncrementExpr(Expr Object, Expr Index, bool IsPrefix, bool IsIncrement) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexIncrement(this);
}

// ECMA-334 §12.21.5 defines null-coalescing assignment on members.
internal sealed record MemberNullCoalesceAssignExpr(Expr Object, string MemberName, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberNullCoalesceAssign(this);
}

// ECMA-334 §12.21.5 defines null-coalescing assignment on element access.
internal sealed record IndexNullCoalesceAssignExpr(Expr Object, Expr Index, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexNullCoalesceAssign(this);
}

internal sealed record IndexAssignExpr(Expr Object, Expr Index, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexAssign(this);
}

internal sealed record MemberAssignExpr(Expr Object, Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMemberAssign(this);
}

internal sealed record NullCoalesceExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNullCoalesce(this);
}

internal sealed record ConditionalExpr(Expr Condition, Expr ThenBranch, Expr ElseBranch) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitConditional(this);
}

// TypeArguments contains the type names for generic method calls (e.g., ["int", "string"])
internal sealed record CallExpr(Expr Callee, List<Expr> Arguments, List<string>? TypeArguments = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCall(this);
}

internal sealed record NamedArgumentExpr(Token Name, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNamedArgument(this);
}

// ECMA-334 §12.6.2 - Argument lists, output parameters
internal sealed record OutArgExpr(string VariableName, string? TypeName, bool IsDiscard) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitOutArg(this);
}

// ECMA-334 §12.19 - Anonymous function expressions
internal sealed record LambdaParameter(string? TypeName, Token Name);

internal sealed record LambdaExpr(List<LambdaParameter> Parameters, Expr Body, bool IsAsync = false, string? ReturnTypeName = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLambda(this);
}

// C# 12 collection expression: [1, 2, 3]
internal sealed record CollectionExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitCollectionExpr(this);
}

// ECMA-334 §12.8.16.5
internal sealed record ImplicitArrayCreationExpr(List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitImplicitArrayCreation(this);
}

// Alder extension (ExpandoObject, not C# anonymous types)
internal sealed record ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectLiteral(this);
}

// §12.18
internal sealed record WithExpr(Expr Object, List<(Token Key, Expr Value)> Initializers) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitWith(this);
}

// Alder extension
internal sealed record SpreadExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSpread(this);
}

internal sealed record InterpolatedStringExpr(List<InterpolatedPart> Parts) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitInterpolatedString(this);
}

internal abstract record InterpolatedPart;
internal sealed record TextPart(string Text) : InterpolatedPart;
internal sealed record ExpressionPart(Expr Expression, string? AlignmentSpecifier = null, string? FormatSpecifier = null) : InterpolatedPart;

internal sealed record NewExpr(Expr Initializer) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNew(this);
}

internal sealed record BlockExpr(List<Expr> Statements, Expr? ReturnExpr) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBlock(this);
}

internal sealed record IfStatementExpr(Expr Condition, List<Expr> ThenStatements, List<Expr>? ElseStatements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIfStatement(this);
}

internal sealed record WhileStatementExpr(Expr Condition, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitWhile(this);
}

internal sealed record ForStatementExpr(List<Expr> Initializers, Expr? Condition, List<Expr> Increments, List<Expr> Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitFor(this);
}

internal sealed record DoWhileStatementExpr(List<Expr> Body, Expr Condition) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDoWhile(this);
}

internal sealed record ForEachStatementExpr(Token VariableName, Expr Collection, List<Expr> Body, string? DeclaredTypeName = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitForEach(this);
}

internal sealed record BreakExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitBreak(this);
}

internal sealed record ContinueExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitContinue(this);
}

internal sealed record ReturnExpr(Expr? Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitReturn(this);
}

// CasePattern is null for default case
internal sealed record SwitchCaseExpr(Pattern? CasePattern, Expr? WhenGuard, List<Expr> Statements);

internal sealed record SwitchStatementExpr(Expr Expression, List<SwitchCaseExpr> Cases) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitch(this);
}

// ECMA-334 §13.10.4
internal sealed record GotoExpr(string Label) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGoto(this);
}

// ECMA-334 §13.10.4
internal sealed record GotoCaseExpr(Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGotoCase(this);
}

// ECMA-334 §13.10.4
internal sealed record GotoDefaultExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitGotoDefault(this);
}

// ECMA-334 §13.15
internal sealed record YieldReturnExpr(Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitYieldReturn(this);
}

// ECMA-334 §13.15
internal sealed record YieldBreakExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitYieldBreak(this);
}

internal sealed record LabelExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLabel(this);
}

// ECMA-334 §13.14
internal sealed record UsingStatementExpr(Expr ResourceDeclaration, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitUsingStatement(this);
}

// ECMA-334 §13.13
internal sealed record LockStatementExpr(Expr LockObject, Expr Body) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitLockStatement(this);
}

internal sealed record VariableDeclExpr(Token? DeclaredType, Token Name, Expr Initializer, bool IsConst = false, IReadOnlyList<string?>? TupleElementNames = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

// TypeToken is null for bare default literal (C# 7.1+)
internal sealed record DefaultExpr(Token? TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDefault(this);
}

// ECMA-334 §12.8.17
internal sealed record TypeofExpr(Token TypeToken) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypeof(this);
}

// ECMA-334 §23.6.9
internal sealed record SizeofExpr(string TypeName) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSizeof(this);
}

// ECMA-334 §12.8.16.3 - Object initializers / §12.8.16.6 - Collection initializers
// PropertyName != null: property initializer (Name = value)
// IndexerKey != null: indexer initializer ([key] = value)
// Elements != null: grouped element initializer ({ expr, expr } calls Add(expr, expr))
// All null: collection initializer element (value calls Add(value))
internal sealed record InitializerEntry(string? PropertyName, Expr? Value = null, Expr? IndexerKey = null, List<Expr>? Elements = null);

internal sealed record ObjectInitializer(List<InitializerEntry> Entries);

// ECMA-334 §12.8.16.2
// Initializer is optional, used for object/collection initializers: new X() { Prop = val }
internal sealed record ObjectCreationExpr(string TypeName, List<Expr> Arguments, ObjectInitializer? Initializer = null) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitObjectCreation(this);
}

// ECMA-334 §12.8.16.4
internal sealed record TypedArrayCreationExpr(string ElementTypeName, Expr Size) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypedArrayCreation(this);
}

// ECMA-334 §12.8.16.5
internal sealed record TypedArrayLiteralExpr(string ElementTypeName, List<Expr> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTypedArrayLiteral(this);
}

internal sealed record MultiDimIndexAccessExpr(Expr Object, List<Expr> Indices, bool NullSafe) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimIndexAccess(this);
}

internal sealed record MultiDimTypedArrayCreationExpr(string ElementTypeName, List<Expr> Sizes) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimTypedArrayCreation(this);
}

internal sealed record MultiDimArrayInitExpr(string ElementTypeName, int Rank, List<Expr>? ExplicitSizes, List<Expr> FlatValues, int[] InferredDimensions) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimArrayInit(this);
}

internal sealed record MultiDimIndexAssignExpr(Expr Object, List<Expr> Indices, Expr Value) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitMultiDimIndexAssign(this);
}

// ECMA-334 §12.16
internal sealed record ThrowExpr(Expr Expression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrow(this);
}

// ECMA-334 §12.9.8
internal sealed record AwaitExpr(Expr Operand) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitAwait(this);
}

// ECMA-334 §13.11
internal sealed record CatchClause(
    string? ExceptionTypeName,
    Token? VariableName,
    Expr? WhenGuard,
    List<Expr> Body);

// ECMA-334 §13.11
internal sealed record TryCatchFinallyExpr(
    List<Expr> TryBody,
    List<CatchClause> CatchClauses,
    List<Expr>? FinallyBody) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTryCatchFinally(this);
}

// ECMA-334 §13.10.6 (rethrow)
internal sealed record ThrowStatementExpr : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitThrowStatement(this);
}

// Name is the final identifier (e.g., "Property" for obj.Property)
internal sealed record NameofExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitNameof(this);
}

internal record TupleElement(string? Name, Expr Expression);

// ECMA-334 §12.8.6
internal sealed record TupleExpr(List<TupleElement> Elements) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitTuple(this);
}

// ECMA-334 §12.7
internal sealed record DeconstructionExpr(List<string> VariableNames, Expr ValueExpression) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitDeconstruction(this);
}

internal sealed record CheckedExpr(Expr Expression, bool IsChecked) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitChecked(this);
}

internal sealed record RangeExpr(Expr? Start, Expr? End, bool ExclusiveEnd) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitRange(this);
}

// ECMA-334 §12.8.11: creates System.Index(expr, fromEnd: true)
internal sealed record IndexFromEndExpr(Expr Operand) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitIndexFromEnd(this);
}

internal sealed record PipelineExpr(Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitPipeline(this);
}

// Desugars to a < b && b < c
internal sealed record ChainedComparisonExpr(List<Expr> Operands, List<Token> Operators) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitChainedComparison(this);
}

// ECMA-334 §11.2 - Pattern matching
internal abstract record Pattern;

// ECMA-334 §11.2.3 - Constant pattern
internal sealed record ConstantPattern(Expr Value) : Pattern;

// ECMA-334 §11.2.2 - Declaration pattern
internal sealed record TypePattern(Token TypeToken, Token? VariableName) : Pattern;

// Relational pattern (not in ECMA-334 7th edition)
internal sealed record RelationalPattern(Token Operator, Expr Operand) : Pattern;

// Logical patterns (not in ECMA-334 7th edition)
internal sealed record AndPattern(Pattern Left, Pattern Right) : Pattern;
internal sealed record OrPattern(Pattern Left, Pattern Right) : Pattern;
internal sealed record NotPattern(Pattern Operand) : Pattern;

// Property pattern (not in ECMA-334 7th edition)
internal sealed record PropertyPattern(Token? TypeToken, List<(Token Name, Pattern Pattern)> Properties, Token? VariableName) : Pattern;

// ECMA-334 §11.2.4 - Var pattern (always matches, binds variable)
internal sealed record VarPattern(Token VariableName) : Pattern;

// Discard pattern (always matches, no binding)
internal sealed record DiscardPattern : Pattern;

internal sealed record ParenthesizedPattern(Pattern Inner) : Pattern;

// Positional (tuple) pattern: matches ITuple elements
internal sealed record PositionalPattern(List<Pattern> Subpatterns) : Pattern;

// C# 11 list pattern: matches countable/indexable collections
internal sealed record ListPattern(List<Pattern> Patterns) : Pattern;

// C# 11 slice pattern (used inside list patterns)
internal sealed record SlicePattern(Pattern? Subpattern) : Pattern;

internal sealed record SwitchArm(Pattern Pattern, Expr? WhenGuard, Expr Value);

// ECMA-334 §12.8.21
internal sealed record SwitchExpressionExpr(Expr Expression, List<SwitchArm> Arms) : Expr
{
    public override T Accept<T>(IExprVisitor<T> visitor) => visitor.VisitSwitchExpression(this);
}
