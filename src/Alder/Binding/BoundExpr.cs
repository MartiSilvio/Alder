using Alder.Text;

namespace Alder.Binding;

internal abstract record BoundExpr(BoundType StaticType)
{
    internal abstract BoundNodeKind Kind { get; }
    internal TextSpan Span { get; init; }
    internal bool HasErrors { get; init; }
    internal AlderDiagnostic? Diagnostic { get; init; }
    internal abstract void EnumerateChildren(Action<BoundExpr> visit);

    /// <summary>
    /// ECMA-334 §12.23: A constant expression is a literal, a unary +/- on a constant,
    /// a cast of a constant, or a binary operation on two constants.
    /// Used to gate §10.2.11 implicit constant expression conversions.
    /// Iterative to handle left-deep binary chains without stack overflow.
    /// </summary>
    internal static bool IsConstantExpression(BoundExpr expr)
    {
        while (true)
        {
            switch (expr.Kind)
            {
                case BoundNodeKind.Literal:
                    return true;

                case BoundNodeKind.UnaryOperator:
                    if (expr is not BoundNodes.BoundUnaryExpr unary) return false;
                    expr = unary.Operand;
                    continue;

                case BoundNodeKind.Conversion:
                    if (expr is not BoundNodes.BoundCastExpr cast) return false;
                    expr = cast.Expression;
                    continue;

                case BoundNodeKind.CheckedExpression:
                    if (expr is not BoundNodes.BoundCheckedExpr check) return false;
                    expr = check.Expression;
                    continue;

                case BoundNodeKind.BinaryOperator:
                    if (expr is not BoundNodes.BoundBinaryExpr binary) return false;
                    if (!IsConstantExpression(binary.Right)) return false;
                    expr = binary.Left;
                    continue;

                default:
                    return false;
            }
        }
    }
}

internal enum BoundNodeKind
{
    // Values match Roslyn BoundKind where an equivalent exists
    UnaryOperator = 27,
    IncrementOperator = 28,
    FromEndIndexExpression = 38,
    RangeExpression = 39,
    BinaryOperator = 40,
    CompoundAssignmentOperator = 43,
    AssignmentOperator = 44,
    DeconstructionAssignment = 45,
    NullCoalescingOperator = 46,
    NullCoalescingAssignmentOperator = 47,
    ConditionalOperator = 49,
    AsOperator = 73,
    Conversion = 75,
    Block = 85,
    VariableDeclaration = 88,
    ReturnStatement = 93,
    BreakStatement = 98,
    ContinueStatement = 99,
    SwitchStatement = 100,
    IfStatement = 102,
    DoStatement = 103,
    WhileStatement = 104,
    ForStatement = 105,
    ForEachStatement = 106,
    UsingStatement = 108,
    LockStatement = 110,
    TryStatement = 111,
    Literal = 113,
    GotoStatement = 124,
    Label = 125,
    SwitchExpression = 131,
    ObjectCreationExpression = 169,
    SpreadElement = 174,
    TupleLiteral = 175,
    Lambda = 201,
    InterpolatedString = 207,
    IsPatternExpression = 212,
    ThrowExpression = 229,

    // Alder-specific — member access
    PropertyAccess = 1_000,
    FieldAccess = 1_001,
    MethodGroup = 1_002,
    DynamicMemberAccess = 1_003,

    // Alder-specific — expressions
    Identifier = 1_010,
    LogicalOperator = 1_011,
    ChainedComparisonOperator = 1_012,
    CheckedExpression = 1_013,
    SliceExpression = 1_014,
    PipelineExpression = 1_015,

    // Alder-specific — invocations & arguments
    ResolvedCall = 1_020,
    DynamicCall = 1_021,
    NamedArgument = 1_022,
    OutArgument = 1_023,

    // Alder-specific — index access
    ResolvedIndexAccess = 1_030,
    DynamicIndexAccess = 1_031,
    ResolvedMultiDimIndexAccess = 1_032,
    DynamicMultiDimIndexAccess = 1_033,

    // Alder-specific — assignments
    MemberAssignment = 1_040,
    MemberCompoundAssignment = 1_041,
    MemberNullCoalesceAssignment = 1_042,
    MemberIncrement = 1_043,
    IndexAssignment = 1_044,
    IndexCompoundAssignment = 1_045,
    IndexNullCoalesceAssignment = 1_046,
    IndexIncrement = 1_047,
    MultiDimIndexAssignment = 1_048,

    // Alder-specific — literals & collections
    ObjectLiteral = 1_060,
    CollectionCreation = 1_061,
    ArrayAllocation = 1_062,
    MultiDimArrayInit = 1_064,

    // Alder-specific — control flow extensions
    GotoCaseStatement = 1_070,
    GotoDefaultStatement = 1_071,
}
