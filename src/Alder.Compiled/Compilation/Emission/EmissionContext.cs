using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission;

internal sealed class EmissionContext
{
    public ParameterExpression ContextParam { get; }
    public ParameterExpression ConfigParam { get; }
    public ParameterExpression ConstraintStateParam { get; }
    public ParameterExpression CancellationTokenParam { get; }

    private readonly Func<BoundExpr, Expression> _legacyEmit;
    private readonly Dictionary<BoundNodeKind, Func<BoundExpr, EmissionContext, Expression>> _emitters = new();

    internal Dictionary<int, PromotedLocal>? PromotedLocals { get; set; }
    internal Dictionary<string, HoistedIdentifier>? HoistedIdentifiers { get; set; }
    internal bool IsChecked { get; set; }
    internal int LoopDepth { get; set; }
    internal int SwitchDepth { get; set; }
    internal int CatchDepth { get; set; }

    internal EmissionContext(
        ParameterExpression contextParam,
        ParameterExpression configParam,
        ParameterExpression constraintStateParam,
        ParameterExpression cancellationTokenParam,
        Func<BoundExpr, Expression> legacyEmit)
    {
        ContextParam = contextParam;
        ConfigParam = configParam;
        ConstraintStateParam = constraintStateParam;
        CancellationTokenParam = cancellationTokenParam;
        _legacyEmit = legacyEmit;
    }

    internal void Register<TNode>(BoundNodeKind kind, INodeEmitter<TNode> emitter) where TNode : BoundExpr
    {
        _emitters[kind] = (expr, ctx) => emitter.Emit((TNode)expr, ctx);
    }

    public Expression Emit(BoundExpr expr)
    {
        if (_emitters.TryGetValue(expr.Kind, out var emitter))
            return emitter(expr, this);
        return _legacyEmit(expr);
    }

    internal bool TryEmit(BoundExpr expr, out Expression result)
    {
        if (_emitters.TryGetValue(expr.Kind, out var emitter))
        {
            result = emitter(expr, this);
            return true;
        }
        result = default!;
        return false;
    }

    public Expression EmitAs(BoundExpr expr, Type targetType)
    {
        var result = Emit(expr);
        return result.Type == targetType
            ? result
            : Expression.Convert(result, targetType);
    }

    public Expression EmitBoxed(BoundExpr expr) => EmitAs(expr, typeof(object));

    internal Func<BoundMemberAccessBase, Expression?>? TryEmitPostfixChain { get; set; }

    public Expression EmitBoolCondition(BoundExpr condition)
    {
        var emitted = Emit(condition);
        if (condition.StaticType.ClrType == typeof(bool) && emitted.Type == typeof(bool))
            return emitted;
        return Expression.Call(BoundRuntimeMethodCache.RequireBooleanMethod, EmitHelpers.AsObject(emitted));
    }
}
