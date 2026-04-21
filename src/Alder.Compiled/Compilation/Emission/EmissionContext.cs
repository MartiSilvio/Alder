using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission;

internal enum ResolvedDispatchMode
{
    Direct,
    RuntimeDispatch
}

internal sealed partial class EmissionContext
{
    public ParameterExpression ContextParam { get; }
    public ParameterExpression ConfigParam { get; }
    public ParameterExpression ConstraintStateParam { get; }
    public ParameterExpression CancellationTokenParam { get; }
    public ResolvedDispatchMode ResolvedDispatchMode { get; }

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
        ResolvedDispatchMode resolvedDispatchMode,
        ParameterExpression cancellationTokenParam)
    {
        ContextParam = contextParam;
        ConfigParam = configParam;
        ConstraintStateParam = constraintStateParam;
        CancellationTokenParam = cancellationTokenParam;
        ResolvedDispatchMode = resolvedDispatchMode;
    }

    public Expression Emit(BoundExpr expr) => Dispatch(expr);

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
