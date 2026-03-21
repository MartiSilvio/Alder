using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Plans;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

/// <summary>
/// Emits expression trees from core bound nodes.
/// This provides a shared semantic entrypoint for compiled mode while unsupported nodes
/// can still fall back to the existing AST compiler pipeline.
/// </summary>
internal sealed partial class BoundExpressionEmitter
{
    private readonly ParameterExpression _contextParam;
    private readonly ParameterExpression _optionsParam;
    private readonly ParameterExpression _ctParam;
    private bool _isChecked;
    private int _loopDepth;
    private int _switchDepth;
    private int _catchDepth;
    private Dictionary<string, HoistedIdentifier>? _hoistedIdentifiers;
    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression optionsParam,
        ParameterExpression ctParam)
    {
        _contextParam = contextParam;
        _optionsParam = optionsParam;
        _ctParam = ctParam;
    }

    public LinqExpression EmitRoot(BoundExpr expr)
    {
        var promotions = BuildLocalPromotionPlan(expr);
        var hoists = BuildIdentifierHoistPlan(expr);

        if (promotions.Count > 0)
        {
            _promotedLocals = promotions;
            foreach (var promoted in promotions.Values)
                hoists.Remove(promoted.Name);
        }

        if (hoists.Count > 0)
            _hoistedIdentifiers = hoists;

        try
        {
            var body = EmitUnwrapSignal(Emit(expr));

            if (_hoistedIdentifiers == null && _promotedLocals == null)
                return body;

            var allVariables = new List<ParameterExpression>();
            var prologueStatements = new List<LinqExpression>();

            if (_hoistedIdentifiers != null)
            {
                foreach (var (name, hoisted) in _hoistedIdentifiers)
                {
                    allVariables.Add(hoisted.Variable);
                    prologueStatements.Add(
                        LinqExpression.Assign(
                            hoisted.Variable,
                            LinqExpression.Call(
                                _contextParam,
                                GetVariableTypedMethodFor(hoisted.Type),
                                LinqExpression.Constant(name))));
                }
            }

            if (_promotedLocals != null)
            {
                foreach (var promoted in _promotedLocals.Values)
                    allVariables.Add(promoted.Variable);
            }

            prologueStatements.Add(body);
            return LinqExpression.Block(body.Type, allVariables, prologueStatements);
        }
        finally
        {
            _hoistedIdentifiers = null;
            _promotedLocals = null;
        }
    }

    private static LinqExpression EmitUnwrapSignal(LinqExpression body)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "rootResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "rootSignal");
        return LinqExpression.Block(
            typeof(object),
            [resultVar, signalVar],
            LinqExpression.Assign(resultVar, EmitHelpers.AsObject(body)),
            LinqExpression.IfThen(
                LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                LinqExpression.Block(
                    LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                    LinqExpression.Assign(resultVar, LinqExpression.Property(signalVar, ControlFlowValueProperty)))),
            resultVar);
    }

    private LinqExpression Emit(BoundExpr expr)
    {
        if (expr.HasErrors)
            throw new BindingNotSupportedException(
                expr.Diagnostic?.Message ?? "Cannot emit expression with binding errors");

        return expr.Kind switch
        {
            BoundNodeKind.Literal => EmitLiteral((BoundLiteralExpr)expr),
            BoundNodeKind.Identifier => EmitIdentifier((BoundIdentifierExpr)expr),
            BoundNodeKind.Conversion => EmitCast((BoundCastExpr)expr),
            BoundNodeKind.AsOperator => EmitAs((BoundAsExpr)expr),
            BoundNodeKind.IsPatternExpression => EmitIsPattern((BoundIsPatternExpr)expr),
            BoundNodeKind.UnaryOperator => EmitUnary((BoundUnaryExpr)expr),
            BoundNodeKind.BinaryOperator => EmitBinary((BoundBinaryExpr)expr),
            BoundNodeKind.LogicalOperator => EmitLogical((BoundLogicalExpr)expr),
            BoundNodeKind.NullCoalescingOperator => EmitNullCoalesce((BoundNullCoalesceExpr)expr),
            BoundNodeKind.ConditionalOperator => EmitConditional((BoundConditionalExpr)expr),
            BoundNodeKind.Block => EmitBlock((BoundBlockExpr)expr),
            BoundNodeKind.IfStatement => EmitIfStatement((BoundIfStatementExpr)expr),
            BoundNodeKind.WhileStatement => EmitWhile((BoundWhileExpr)expr),
            BoundNodeKind.ForStatement => EmitFor((BoundForExpr)expr),
            BoundNodeKind.DoStatement => EmitDoWhile((BoundDoWhileExpr)expr),
            BoundNodeKind.ForEachStatement => EmitForEach((BoundForEachExpr)expr),
            BoundNodeKind.UsingStatement => EmitUsingStatement((BoundUsingStatementExpr)expr),
            BoundNodeKind.LockStatement => EmitLockStatement((BoundLockStatementExpr)expr),
            BoundNodeKind.TryStatement => EmitTryCatchFinally((BoundTryCatchFinallyExpr)expr),
            BoundNodeKind.BreakStatement => EmitBreak((BoundBreakExpr)expr),
            BoundNodeKind.ContinueStatement => EmitContinue((BoundContinueExpr)expr),
            BoundNodeKind.GotoStatement => EmitGoto((BoundGotoExpr)expr),
            BoundNodeKind.GotoCaseStatement => EmitGotoCase((BoundGotoCaseExpr)expr),
            BoundNodeKind.GotoDefaultStatement => EmitGotoDefault(),
            BoundNodeKind.Label => LinqExpression.Constant(null, typeof(object)),
            BoundNodeKind.ThrowStatement => EmitThrowStatement((BoundThrowStatementExpr)expr),
            BoundNodeKind.ReturnStatement => EmitReturn((BoundReturnExpr)expr),
            BoundNodeKind.SwitchStatement => EmitSwitchStatement((BoundSwitchStatementExpr)expr),
            BoundNodeKind.SwitchExpression => EmitSwitchExpression((BoundSwitchExpressionExpr)expr),
            BoundNodeKind.CheckedExpression => EmitChecked((BoundCheckedExpr)expr),
            BoundNodeKind.ChainedComparisonOperator => EmitChainedComparison((BoundChainedComparisonExpr)expr),
            BoundNodeKind.RangeExpression => EmitRange((BoundRangeExpr)expr),
            BoundNodeKind.VariableDeclaration => EmitVariableDecl((BoundVariableDeclExpr)expr),
            BoundNodeKind.AssignmentOperator => EmitAssign((BoundAssignExpr)expr),
            BoundNodeKind.NullCoalescingAssignmentOperator => EmitNullCoalesceAssign((BoundNullCoalesceAssignExpr)expr),
            BoundNodeKind.CompoundAssignmentOperator => EmitCompoundAssign((BoundCompoundAssignExpr)expr),
            BoundNodeKind.IncrementOperator => EmitIncrementDecrement((BoundIncrementDecrementExpr)expr),
            BoundNodeKind.MemberAssignment => EmitMemberAssign((BoundMemberAssignExpr)expr),
            BoundNodeKind.IndexAssignment => EmitIndexAssign((BoundIndexAssignExpr)expr),
            BoundNodeKind.MemberCompoundAssignment => EmitMemberCompoundAssign((BoundMemberCompoundAssignExpr)expr),
            BoundNodeKind.IndexCompoundAssignment => EmitIndexCompoundAssign((BoundIndexCompoundAssignExpr)expr),
            BoundNodeKind.MemberNullCoalesceAssignment => EmitMemberNullCoalesceAssign((BoundMemberNullCoalesceAssignExpr)expr),
            BoundNodeKind.IndexNullCoalesceAssignment => EmitIndexNullCoalesceAssign((BoundIndexNullCoalesceAssignExpr)expr),
            BoundNodeKind.MemberIncrement => EmitMemberIncrement((BoundMemberIncrementExpr)expr),
            BoundNodeKind.IndexIncrement => EmitIndexIncrement((BoundIndexIncrementExpr)expr),
            BoundNodeKind.MemberAccess => EmitMemberAccess((BoundMemberAccessExpr)expr),
            BoundNodeKind.IndexerAccess => EmitIndexAccess((BoundIndexAccessExpr)expr),
            BoundNodeKind.ObjectCreationExpression => EmitObjectCreation((BoundObjectCreationExpr)expr),
            BoundNodeKind.TypedArrayCreation => EmitTypedArrayCreation((BoundTypedArrayCreationExpr)expr),
            BoundNodeKind.TypedArrayLiteral => EmitTypedArrayLiteral((BoundTypedArrayLiteralExpr)expr),
            BoundNodeKind.TupleLiteral => EmitTuple((BoundTupleExpr)expr),
            BoundNodeKind.DeconstructionAssignment => EmitDeconstruction((BoundDeconstructionExpr)expr),
            BoundNodeKind.MultiDimTypedArrayCreation => EmitMultiDimTypedArrayCreation((BoundMultiDimTypedArrayCreationExpr)expr),
            BoundNodeKind.MultiDimArrayInit => EmitMultiDimArrayInit((BoundMultiDimArrayInitExpr)expr),
            BoundNodeKind.MultiDimIndexAccess => EmitMultiDimIndexAccess((BoundMultiDimIndexAccessExpr)expr),
            BoundNodeKind.MultiDimIndexAssignment => EmitMultiDimIndexAssign((BoundMultiDimIndexAssignExpr)expr),
            BoundNodeKind.ThrowExpression => EmitThrow((BoundThrowExpr)expr),
            BoundNodeKind.FromEndIndexExpression => EmitIndexFromEnd((BoundIndexFromEndExpr)expr),
            BoundNodeKind.SliceExpression => EmitSlice((BoundSliceExpr)expr),
            BoundNodeKind.Call => EmitCall((BoundCallExpr)expr),
            BoundNodeKind.Invoke => EmitInvoke((BoundInvokeExpr)expr),
            BoundNodeKind.Lambda => EmitLambda((BoundLambdaExpr)expr),
            BoundNodeKind.PipelineExpression => EmitPipeline((BoundPipelineExpr)expr),
            BoundNodeKind.ArrayLiteral => EmitArrayLiteral((BoundArrayLiteralExpr)expr),
            BoundNodeKind.ObjectLiteral => EmitObjectLiteral((BoundObjectLiteralExpr)expr),
            BoundNodeKind.InterpolatedString => EmitInterpolatedString((BoundInterpolatedStringExpr)expr),
            BoundNodeKind.NamedArgument => EmitNamedArgument((BoundNamedArgumentExpr)expr),
            BoundNodeKind.OutArgument => EmitOutArg((BoundOutArgExpr)expr),
            BoundNodeKind.SpreadElement => EmitInvalidSpread(),
            _ => throw new BindingNotSupportedException(
                $"Bound compiled emission not implemented for '{expr.GetType().Name}'")
        };
    }

    private static Dictionary<string, HoistedIdentifier> BuildIdentifierHoistPlan(BoundExpr root)
    {
        var usage = new Dictionary<string, (Type Type, int Count)>(StringComparer.Ordinal);
        if (!CanHoistIdentifiers(root, usage))
            return new Dictionary<string, HoistedIdentifier>(StringComparer.Ordinal);

        var hoists = new Dictionary<string, HoistedIdentifier>(StringComparer.Ordinal);
        foreach (var (name, entry) in usage)
        {
            if (entry.Count <= 1)
                continue;

            hoists[name] = new HoistedIdentifier(
                entry.Type,
                LinqExpression.Variable(entry.Type, $"cached_{name.Replace('.', '_')}"));
        }

        return hoists;
    }

    private static bool CanHoistIdentifiers(BoundExpr expr, Dictionary<string, (Type Type, int Count)> usage)
    {
        while (true)
        {
            switch (expr.Kind)
            {
                case BoundNodeKind.Literal:
                    return true;

                case BoundNodeKind.Identifier:
                    var identifier = (BoundIdentifierExpr)expr;
                    if (identifier.StaticType == typeof(object)) return false;

                    if (usage.TryGetValue(identifier.Name, out var entry))
                        usage[identifier.Name] = (entry.Type, entry.Count + 1);
                    else
                        usage[identifier.Name] = (identifier.StaticType, 1);
                    return true;

                case BoundNodeKind.BinaryOperator:
                {
                    var current = (BoundBinaryExpr)expr;
                    while (current.Left is BoundBinaryExpr left)
                    {
                        if (!CanHoistIdentifiers(current.Right, usage)) return false;
                        current = left;
                    }
                    return CanHoistIdentifiers(current.Left, usage) && CanHoistIdentifiers(current.Right, usage);
                }

                case BoundNodeKind.LogicalOperator:
                {
                    var current = (BoundLogicalExpr)expr;
                    while (current.Left is BoundLogicalExpr left)
                    {
                        if (!CanHoistIdentifiers(current.Right, usage)) return false;
                        current = left;
                    }
                    return CanHoistIdentifiers(current.Left, usage) && CanHoistIdentifiers(current.Right, usage);
                }

                case BoundNodeKind.UnaryOperator:
                    expr = ((BoundUnaryExpr)expr).Operand;
                    continue;

                case BoundNodeKind.Conversion:
                    expr = ((BoundCastExpr)expr).Expression;
                    continue;

                case BoundNodeKind.AsOperator:
                    expr = ((BoundAsExpr)expr).Expression;
                    continue;

                case BoundNodeKind.IsPatternExpression:
                    expr = ((BoundIsPatternExpr)expr).Expression;
                    continue;

                case BoundNodeKind.CheckedExpression:
                    expr = ((BoundCheckedExpr)expr).Expression;
                    continue;

                case BoundNodeKind.NullCoalescingOperator:
                {
                    var current = (BoundNullCoalesceExpr)expr;
                    while (current.Left is BoundNullCoalesceExpr left)
                    {
                        if (!CanHoistIdentifiers(current.Right, usage)) return false;
                        current = left;
                    }
                    return CanHoistIdentifiers(current.Left, usage) && CanHoistIdentifiers(current.Right, usage);
                }

                case BoundNodeKind.ConditionalOperator:
                    var conditional = (BoundConditionalExpr)expr;
                    return CanHoistIdentifiers(conditional.Condition, usage) && CanHoistIdentifiers(conditional.ThenBranch, usage) && CanHoistIdentifiers(conditional.ElseBranch, usage);

                default:
                    return false;
            }
        }
    }

    private sealed record HoistedIdentifier(Type Type, ParameterExpression Variable);

    private LinqExpression ResolveTypeByName(string typeName)
    {
        return LinqExpression.Call(
            LinqExpression.Call(_contextParam, GetTypeResolverProperty),
            ResolveTypeMethod,
            LinqExpression.Constant(typeName));
    }
}