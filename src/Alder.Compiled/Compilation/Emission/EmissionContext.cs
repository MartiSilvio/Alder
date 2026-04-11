using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission;

internal sealed class EmissionContext
{
    public ParameterExpression ContextParam { get; }
    public ParameterExpression ConfigParam { get; }
    public ParameterExpression ConstraintStateParam { get; }
    public ParameterExpression CancellationTokenParam { get; }
    public bool PreferResolvedRuntimeDispatch { get; }

    private readonly Dictionary<BoundNodeKind, Func<BoundExpr, EmissionContext, Expression>> _emitters = new();

    internal Dictionary<int, PromotedLocal>? PromotedLocals { get; set; }
    internal Dictionary<string, HoistedIdentifier>? HoistedIdentifiers { get; set; }
    internal ParameterExpression SignalParam { get; set; } = null!;
    internal bool IsChecked { get; set; }
    internal int LoopDepth { get; set; }
    internal int SwitchDepth { get; set; }
    internal int CatchDepth { get; set; }

    internal EmissionContext(
        ParameterExpression contextParam,
        ParameterExpression configParam,
        ParameterExpression constraintStateParam,
        ParameterExpression cancellationTokenParam,
        bool preferResolvedRuntimeDispatch)
    {
        ContextParam = contextParam;
        ConfigParam = configParam;
        ConstraintStateParam = constraintStateParam;
        CancellationTokenParam = cancellationTokenParam;
        PreferResolvedRuntimeDispatch = preferResolvedRuntimeDispatch;
    }

    internal void Register<TNode>(BoundNodeKind kind, INodeEmitter<TNode> emitter) where TNode : BoundExpr
    {
        _emitters[kind] = (expr, ctx) => emitter.Emit((TNode)expr, ctx);
    }

    public Expression Emit(BoundExpr expr)
    {
        if (_emitters.TryGetValue(expr.Kind, out var emitter))
            return emitter(expr, this);

        throw new BindingNotSupportedException(
            $"Bound compiled emission not implemented for '{expr.GetType().Name}'");
    }

    public Expression EmitAs(BoundExpr expr, Type targetType)
    {
        var result = Emit(expr);
        return result.Type == targetType
            ? result
            : Expression.Convert(result, targetType);
    }

    public Expression EmitBoxed(BoundExpr expr) => EmitAs(expr, typeof(object));

    private readonly Stack<Dictionary<string, ParameterExpression>> _lambdaScopes = new();

    internal void PushLambdaScope(
        ImmutableArray<BoundTypedLambdaParameter> parameters,
        ParameterExpression[] expressions)
    {
        var scope = new Dictionary<string, ParameterExpression>();
        for (var i = 0; i < parameters.Length; i++)
            scope[parameters[i].Name] = expressions[i];
        _lambdaScopes.Push(scope);
    }

    internal void PopLambdaScope() => _lambdaScopes.Pop();

    internal bool TryGetLambdaParameter(string name, out ParameterExpression param)
    {
        foreach (var scope in _lambdaScopes)
        {
            if (scope.TryGetValue(name, out param!))
                return true;
        }
        param = null!;
        return false;
    }

    internal Func<BoundExpr, Expression?>? TryEmitPostfixChain { get; set; }

    public Expression EmitBoolCondition(BoundExpr condition)
    {
        var emitted = Emit(condition);
        if (condition.StaticType.ClrType == typeof(bool) && emitted.Type == typeof(bool))
            return emitted;
        return Expression.Call(RequireBooleanMethod, EmitHelpers.AsObject(emitted));
    }

    internal bool TryGetPromoted(int? localId, out PromotedLocal promoted)
    {
        if (localId is { } id && PromotedLocals != null && PromotedLocals.TryGetValue(id, out promoted!))
            return true;
        promoted = null!;
        return false;
    }

    internal Expression ResolveTypeByName(string typeName)
    {
        return Expression.Call(
            Expression.Call(ContextParam, GetTypeResolverProperty),
            ResolveTypeMethod,
            Expression.Constant(typeName));
    }
}
