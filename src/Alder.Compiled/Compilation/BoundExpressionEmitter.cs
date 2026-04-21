using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compiled.Compilation.Emission;
using Alder.Compiled.Compilation.Emission.Emitters;
using Alder.Interpretation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

/// <summary>
/// Emits LINQ expression trees from bound nodes for the compiled backend.
/// Nodes that cannot be represented directly continue through the broader compilation pipeline.
/// </summary>
internal sealed partial class BoundExpressionEmitter
{
    private readonly ParameterExpression _contextParam;
    private readonly EmissionContext _emissionCtx;

    private Dictionary<string, HoistedIdentifier>? _hoistedIdentifiers;

    public BoundExpressionEmitter(
        ParameterExpression contextParam,
        ParameterExpression configParam,
        ParameterExpression constraintStateParam,
        ResolvedDispatchMode resolvedDispatchMode,
        ParameterExpression ctParam)
    {
        _contextParam = contextParam;
        _emissionCtx = new EmissionContext(
            contextParam,
            configParam,
            constraintStateParam,
            resolvedDispatchMode,
            ctParam);
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
            // §13.10.4: escaping goto must throw CS0159, not silently unwrap to its label name.
            var body = LinqExpression.Block(
                typeof(object),
                [resultVar],
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(emittedBody)),
                LinqExpression.IfThen(
                    LinqExpression.NotEqual(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                    LinqExpression.Assign(resultVar, LinqExpression.Call(ControlFlowUnwrapOrThrowMethod, signalParam))),
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
