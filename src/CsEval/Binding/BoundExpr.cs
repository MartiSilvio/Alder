using CsEval.Text;

namespace CsEval.Binding;

internal abstract record BoundExpr(Type StaticType)
{
    internal abstract BoundNodeKind Kind { get; }
    internal TextSpan Span { get; set; }
    internal bool HasErrors { get; init; }
    internal CsEvalDiagnostic? Diagnostic { get; init; }
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
    ThrowStatement = 96,
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
    Call = 164,
    ObjectCreationExpression = 169,
    SpreadElement = 174,
    TupleLiteral = 175,
    IndexerAccess = 198,
    Lambda = 201,
    InterpolatedString = 207,
    IsPatternExpression = 212,
    ThrowExpression = 229,

    // CsEval-specific
    Identifier = 1_000,
    LogicalOperator = 1_001,
    ChainedComparisonOperator = 1_002,
    CheckedExpression = 1_003,
    MemberAccess = 1_004,
    MultiDimIndexAccess = 1_005,
    SliceExpression = 1_006,
    Invoke = 1_007,
    PipelineExpression = 1_008,
    NamedArgument = 1_009,
    OutArgument = 1_010,
    MemberAssignment = 1_011,
    MemberCompoundAssignment = 1_012,
    MemberNullCoalesceAssignment = 1_013,
    MemberIncrement = 1_014,
    IndexAssignment = 1_015,
    IndexCompoundAssignment = 1_016,
    IndexNullCoalesceAssignment = 1_017,
    IndexIncrement = 1_018,
    MultiDimIndexAssignment = 1_019,
    ObjectLiteral = 1_020,
    ArrayLiteral = 1_021,
    TypedArrayCreation = 1_022,
    TypedArrayLiteral = 1_023,
    MultiDimArrayInit = 1_024,
    MultiDimTypedArrayCreation = 1_025,
    GotoCaseStatement = 1_026,
    GotoDefaultStatement = 1_027,
}
