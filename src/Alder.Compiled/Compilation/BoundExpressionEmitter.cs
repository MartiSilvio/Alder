using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compiled.Compilation.Emission;
using Alder.Compiled.Compilation.Emission.Emitters;
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
    private readonly ParameterExpression _configParam;
    private readonly ParameterExpression _constraintStateParam;
    private readonly ParameterExpression _ctParam;
    private readonly Emission.EmissionContext _emissionCtx;
    private bool _isChecked;
    private int _loopDepth;
    private int _switchDepth;
    private int _catchDepth;
    private Dictionary<string, Emission.HoistedIdentifier>? _hoistedIdentifiers;
    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression configParam,
        ParameterExpression constraintStateParam,
        ParameterExpression ctParam)
    {
        _contextParam = contextParam;
        _configParam = configParam;
        _constraintStateParam = constraintStateParam;
        _ctParam = ctParam;
        _emissionCtx = new EmissionContext(contextParam, configParam, constraintStateParam, ctParam, Emit, () => _isChecked) { GetCatchDepth = () => _catchDepth };
        _emissionCtx.Register(BoundNodeKind.Literal, new LiteralEmitter());
        _emissionCtx.Register(BoundNodeKind.Identifier, new IdentifierEmitter());
        _emissionCtx.Register(BoundNodeKind.Conversion, new CastEmitter());
        _emissionCtx.Register(BoundNodeKind.AsOperator, new AsEmitter());
        _emissionCtx.Register(BoundNodeKind.UnaryOperator, new UnaryEmitter());
        _emissionCtx.Register(BoundNodeKind.IsPatternExpression, new IsPatternEmitter());
        _emissionCtx.Register(BoundNodeKind.BinaryOperator, new BinaryEmitter());
        _emissionCtx.Register(BoundNodeKind.LogicalOperator, new LogicalEmitter());
        _emissionCtx.Register(BoundNodeKind.NullCoalescingOperator, new NullCoalesceEmitter());
        _emissionCtx.Register(BoundNodeKind.ConditionalOperator, new ConditionalEmitter());
        var memberEmitter = new MemberAccessEmitter();
        _emissionCtx.Register<BoundPropertyAccessExpr>(BoundNodeKind.PropertyAccess, memberEmitter);
        _emissionCtx.Register<BoundFieldAccessExpr>(BoundNodeKind.FieldAccess, memberEmitter);
        _emissionCtx.Register<BoundMethodGroupExpr>(BoundNodeKind.MethodGroup, memberEmitter);
        _emissionCtx.Register<BoundDynamicMemberAccessExpr>(BoundNodeKind.DynamicMemberAccess, memberEmitter);
        _emissionCtx.Register(BoundNodeKind.ObjectCreationExpression, new ObjectCreationEmitter());
        _emissionCtx.Register(BoundNodeKind.ArrayAllocation, new ArrayAllocEmitter());
        _emissionCtx.Register(BoundNodeKind.TupleLiteral, new TupleEmitter());
        _emissionCtx.Register(BoundNodeKind.ThrowExpression, new ThrowEmitter());
        var multiDimEmitter = new MultiDimEmitter();
        _emissionCtx.Register<BoundMultiDimArrayInitExpr>(BoundNodeKind.MultiDimArrayInit, multiDimEmitter);
        _emissionCtx.Register<BoundResolvedMultiDimIndexAccessExpr>(BoundNodeKind.ResolvedMultiDimIndexAccess, multiDimEmitter);
        _emissionCtx.Register<BoundDynamicMultiDimIndexAccessExpr>(BoundNodeKind.DynamicMultiDimIndexAccess, multiDimEmitter);
        _emissionCtx.Register<BoundMultiDimIndexAssignExpr>(BoundNodeKind.MultiDimIndexAssignment, multiDimEmitter);
        _emissionCtx.Register(BoundNodeKind.CollectionCreation, new CollectionCreationEmitter());
        _emissionCtx.Register(BoundNodeKind.ObjectLiteral, new ObjectLiteralEmitter());
        _emissionCtx.Register(BoundNodeKind.InterpolatedString, new InterpolatedStringEmitter());
        _emissionCtx.Register(BoundNodeKind.Lambda, new LambdaEmitter());
        _emissionCtx.Register(BoundNodeKind.PipelineExpression, new PipelineEmitter());
        _emissionCtx.Register(BoundNodeKind.NamedArgument, new NamedArgumentEmitter());
        _emissionCtx.Register(BoundNodeKind.OutArgument, new OutArgEmitter());
        _emissionCtx.Register(BoundNodeKind.DeconstructionAssignment, new DeconstructionEmitter());
        _emissionCtx.Register(BoundNodeKind.SpreadElement, new SpreadEmitter());
        _emissionCtx.Register(BoundNodeKind.CheckedExpression, new CheckedEmitter(() => _isChecked, v => _isChecked = v));
        _emissionCtx.Register(BoundNodeKind.ChainedComparisonOperator, new ChainedComparisonEmitter());
        _emissionCtx.Register(BoundNodeKind.RangeExpression, new RangeEmitter());
        _emissionCtx.Register(BoundNodeKind.FromEndIndexExpression, new IndexFromEndEmitter());
        _emissionCtx.Register(BoundNodeKind.SliceExpression, new SliceEmitter());
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

        _emissionCtx.PromotedLocals = _promotedLocals;
        _emissionCtx.HoistedIdentifiers = _hoistedIdentifiers;
        _emissionCtx.TryEmitPostfixChain = node =>
        {
            var chain = PostfixChain.TryCollect(node);
            return chain != null ? EmitPostfixChain(chain.Value) : null;
        };

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

        if (_emissionCtx.TryEmit(expr, out var result))
            return result;

        return expr.Kind switch
        {
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
            BoundNodeKind.ReturnStatement => EmitReturn((BoundReturnExpr)expr),
            BoundNodeKind.SwitchStatement => EmitSwitchStatement((BoundSwitchStatementExpr)expr),
            BoundNodeKind.SwitchExpression => EmitSwitchExpression((BoundSwitchExpressionExpr)expr),
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
            BoundNodeKind.ResolvedIndexAccess => EmitIndexAccess((BoundResolvedIndexAccessExpr)expr),
            BoundNodeKind.DynamicIndexAccess => EmitDynamicIndexAccess((BoundDynamicIndexAccessExpr)expr),
            BoundNodeKind.ResolvedCall => EmitCall((BoundResolvedCallExpr)expr),
            BoundNodeKind.DynamicCall => EmitInvoke((BoundDynamicCallExpr)expr),
            _ => throw new BindingNotSupportedException(
                $"Bound compiled emission not implemented for '{expr.GetType().Name}'")
        };
    }

    private static Dictionary<string, Emission.HoistedIdentifier> BuildIdentifierHoistPlan(BoundExpr root)
    {
        var usage = new Dictionary<string, (Type Type, int Count)>(StringComparer.Ordinal);
        if (!CanHoistIdentifiers(root, usage))
            return new Dictionary<string, Emission.HoistedIdentifier>(StringComparer.Ordinal);

        var hoists = new Dictionary<string, Emission.HoistedIdentifier>(StringComparer.Ordinal);
        foreach (var (name, entry) in usage)
        {
            if (entry.Count <= 1)
                continue;

            hoists[name] = new Emission.HoistedIdentifier(
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
                    if (identifier.StaticType.ClrType == typeof(object)) return false;

                    if (usage.TryGetValue(identifier.Name, out var entry))
                        usage[identifier.Name] = (entry.Type, entry.Count + 1);
                    else
                        usage[identifier.Name] = (identifier.StaticType.ClrType, 1);
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


    private LinqExpression ResolveTypeByName(string typeName)
    {
        return LinqExpression.Call(
            LinqExpression.Call(_contextParam, GetTypeResolverProperty),
            ResolveTypeMethod,
            LinqExpression.Constant(typeName));
    }
}