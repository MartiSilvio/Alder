using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compiled.Compilation.Emission;
using Alder.Compiled.Compilation.Emission.Emitters;
using Alder.Interpretation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

/// <summary>
/// Emits expression trees from core bound nodes. Unsupported nodes
/// fall back to the existing AST compiler pipeline.
/// </summary>
internal sealed partial class BoundExpressionEmitter
{
    private readonly ParameterExpression _contextParam;
    private readonly ParameterExpression _configParam;
    private readonly ParameterExpression _constraintStateParam;
    private readonly ParameterExpression _ctParam;
    private readonly EmissionContext _emissionCtx;

    private Dictionary<string, HoistedIdentifier>? _hoistedIdentifiers;

    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression configParam,
        ParameterExpression constraintStateParam,
        ParameterExpression ctParam,
        bool preferResolvedRuntimeDispatch)
    {
        _contextParam = contextParam;
        _configParam = configParam;
        _constraintStateParam = constraintStateParam;
        _ctParam = ctParam;
        _emissionCtx = new EmissionContext(
            contextParam,
            configParam,
            constraintStateParam,
            ctParam,
            preferResolvedRuntimeDispatch);
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
        _emissionCtx.Register<BoundResolvedMultiDimIndexAccessExpr>(BoundNodeKind.ResolvedMultiDimIndexAccess,
            multiDimEmitter);
        _emissionCtx.Register<BoundDynamicMultiDimIndexAccessExpr>(BoundNodeKind.DynamicMultiDimIndexAccess,
            multiDimEmitter);
        _emissionCtx.Register<BoundMultiDimIndexAssignExpr>(BoundNodeKind.MultiDimIndexAssignment, multiDimEmitter);
        _emissionCtx.Register(BoundNodeKind.CollectionCreation, new CollectionCreationEmitter());
        _emissionCtx.Register(BoundNodeKind.ObjectLiteral, new ObjectLiteralEmitter());
        _emissionCtx.Register(BoundNodeKind.WithExpression, new WithEmitter());
        _emissionCtx.Register(BoundNodeKind.InterpolatedString, new InterpolatedStringEmitter());
        _emissionCtx.Register(BoundNodeKind.Lambda, new LambdaEmitter());
        _emissionCtx.Register(BoundNodeKind.TypedLambda, new TypedLambdaEmitter());
        _emissionCtx.Register(BoundNodeKind.PipelineExpression, new PipelineEmitter());
        _emissionCtx.Register(BoundNodeKind.NamedArgument, new NamedArgumentEmitter());
        _emissionCtx.Register(BoundNodeKind.OutArgument, new OutArgEmitter());
        _emissionCtx.Register(BoundNodeKind.DeconstructionAssignment, new DeconstructionEmitter());
        _emissionCtx.Register(BoundNodeKind.SpreadElement, new SpreadEmitter());
        _emissionCtx.Register(BoundNodeKind.CheckedExpression, new CheckedEmitter());
        _emissionCtx.Register(BoundNodeKind.ChainedComparisonOperator, new ChainedComparisonEmitter());
        _emissionCtx.Register(BoundNodeKind.RangeExpression, new RangeEmitter());
        _emissionCtx.Register(BoundNodeKind.FromEndIndexExpression, new IndexFromEndEmitter());
        _emissionCtx.Register(BoundNodeKind.SliceExpression, new SliceEmitter());
        _emissionCtx.Register(BoundNodeKind.BreakStatement, new BreakEmitter());
        _emissionCtx.Register(BoundNodeKind.ContinueStatement, new ContinueEmitter());
        _emissionCtx.Register(BoundNodeKind.ReturnStatement, new ReturnEmitter());
        _emissionCtx.Register(BoundNodeKind.GotoStatement, new GotoEmitter());
        _emissionCtx.Register(BoundNodeKind.GotoCaseStatement, new GotoCaseEmitter());
        _emissionCtx.Register(BoundNodeKind.Block, new BlockEmitter());
        _emissionCtx.Register(BoundNodeKind.IfStatement, new IfEmitter());
        _emissionCtx.Register(BoundNodeKind.WhileStatement, new WhileEmitter());
        _emissionCtx.Register(BoundNodeKind.ForStatement, new ForEmitter());
        _emissionCtx.Register(BoundNodeKind.DoStatement, new DoWhileEmitter());
        _emissionCtx.Register(BoundNodeKind.ForEachStatement, new ForEachEmitter());
        _emissionCtx.Register(BoundNodeKind.UsingStatement, new UsingEmitter());
        _emissionCtx.Register(BoundNodeKind.LockStatement, new LockEmitter());
        _emissionCtx.Register(BoundNodeKind.TryStatement, new TryCatchEmitter());
        _emissionCtx.Register(BoundNodeKind.GotoDefaultStatement, new GotoDefaultEmitter());
        _emissionCtx.Register(BoundNodeKind.Label, new LabelEmitter());
        _emissionCtx.Register(BoundNodeKind.SwitchStatement, new SwitchStatementEmitter());
        _emissionCtx.Register(BoundNodeKind.SwitchExpression, new SwitchExpressionEmitter());
        _emissionCtx.Register(BoundNodeKind.VariableDeclaration, new VariableDeclEmitter());
        _emissionCtx.Register(BoundNodeKind.AssignmentOperator, new AssignEmitter());
        _emissionCtx.Register(BoundNodeKind.NullCoalescingAssignmentOperator, new NullCoalesceAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.CompoundAssignmentOperator, new CompoundAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.IncrementOperator, new IncrementDecrementEmitter());
        _emissionCtx.Register(BoundNodeKind.MemberAssignment, new MemberAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.IndexAssignment, new IndexAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.MemberCompoundAssignment, new MemberCompoundAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.IndexCompoundAssignment, new IndexCompoundAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.MemberNullCoalesceAssignment, new MemberNullCoalesceAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.IndexNullCoalesceAssignment, new IndexNullCoalesceAssignEmitter());
        _emissionCtx.Register(BoundNodeKind.MemberIncrement, new MemberIncrementEmitter());
        _emissionCtx.Register(BoundNodeKind.IndexIncrement, new IndexIncrementEmitter());
        _emissionCtx.Register(BoundNodeKind.ResolvedIndexAccess, new ResolvedIndexAccessEmitter());
        _emissionCtx.Register(BoundNodeKind.DynamicIndexAccess, new DynamicIndexAccessEmitter());
        _emissionCtx.Register(BoundNodeKind.ResolvedCall, new ResolvedCallEmitter());
        _emissionCtx.Register(BoundNodeKind.DynamicCall, new DynamicCallEmitter());
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

        var signalParam = LinqExpression.Variable(typeof(ControlFlowSignal), "signal");
        _emissionCtx.SignalParam = signalParam;
        _emissionCtx.PromotedLocals = _promotedLocals;
        _emissionCtx.HoistedIdentifiers = _hoistedIdentifiers;
        _emissionCtx.TryEmitPostfixChain = node =>
        {
            var chain = PostfixChain.TryCollect(node);
            return chain != null ? ResolvedCallEmitter.EmitPostfixChain(chain.Value, _emissionCtx) : null;
        };

        try
        {
            var emittedBody = Emit(expr);
            var resultVar = LinqExpression.Variable(typeof(object), "rootResult");
            var body = LinqExpression.Block(
                typeof(object),
                [resultVar],
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(emittedBody)),
                LinqExpression.IfThen(
                    LinqExpression.NotEqual(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                    LinqExpression.Assign(resultVar, LinqExpression.Property(signalParam, ControlFlowValueProperty))),
                resultVar);

            var allVariables = new List<ParameterExpression> { signalParam };
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

    private LinqExpression Emit(BoundExpr expr)
    {
        if (expr.HasErrors)
            throw new BindingNotSupportedException(
                expr.Diagnostic?.Message ?? "Cannot emit expression with binding errors");

        return _emissionCtx.Emit(expr);
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
                    return CanHoistIdentifiers(conditional.Condition, usage) &&
                           CanHoistIdentifiers(conditional.ThenBranch, usage) &&
                           CanHoistIdentifiers(conditional.ElseBranch, usage);

                default:
                    return false;
            }
        }
    }


}
