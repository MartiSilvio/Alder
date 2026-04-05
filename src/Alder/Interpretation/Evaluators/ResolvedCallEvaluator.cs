using System.Collections.Immutable;
using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ResolvedCall)]
internal static class ResolvedCallEvaluator
{
    public static object? Evaluate(BoundResolvedCallExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value, ctx, ct);

        return EvaluateResolvedCallDirect(node, null, ctx, ct);
    }

    internal static object? EvaluateResolvedCallDirect(BoundResolvedCallExpr call, object? evaluatedTarget, EvaluationContext ctx, CancellationToken ct)
    {
        if (call is { IsExtensionCall: true, Callee: BoundMethodGroupExpr extMethodGroup })
        {
            if (extMethodGroup.NullSafe)
            {
                var nullCheckTarget = evaluatedTarget ?? ctx.Evaluate(call.Arguments[0], ct);
                if (nullCheckTarget == null) return null;
            }

            object?[] extensionArgs;
            if (evaluatedTarget != null)
            {
                extensionArgs = new object?[call.Arguments.Length];
                extensionArgs[0] = CoerceExtensionReceiver(evaluatedTarget);
                for (var i = 1; i < call.Arguments.Length; i++)
                    extensionArgs[i] = ctx.Evaluate(call.Arguments[i], ct);
            }
            else
            {
                extensionArgs = EvaluateArguments(call.Arguments, ctx, ct);
                if (extensionArgs.Length > 0 && extensionArgs[0] != null)
                    extensionArgs[0] = CoerceExtensionReceiver(extensionArgs[0]!);
            }

            return InvokeResolvedExtension(call.Resolution, extensionArgs, ctx, ct);
        }

        if (call.Callee is BoundMethodGroupExpr methodGroup)
        {
            var target = evaluatedTarget ?? (methodGroup.IsStatic ? null : ctx.Evaluate(methodGroup.Target, ct));
            if (methodGroup.NullSafe && target == null)
                return null;

            var plannedArgs = EvaluateArguments(call.Arguments, ctx, ct);
            return InvokeResolved(call.Resolution, target, plannedArgs, ctx, ct);
        }

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(call.Arguments, ctx, ct);

        var callee = ctx.Evaluate(call.Callee, ct);
        var invokeResult = MethodInvoker.InvokeCall(callee, args, ctx.Context, ct: ct);
        DefineOutVariablesIfAny(args, outBindings, ctx, ct);
        ExecutionRuntime.CheckCollectionSize(invokeResult, ctx.Context.Config.Security);
        return invokeResult;
    }

    internal static object? EvaluatePostfixChain(PostfixChain.Chain chain, EvaluationContext ctx, CancellationToken ct)
    {
        var result = ctx.Evaluate(chain.Root, ct);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
                result = EvaluateResolvedCallDirect(call, result, ctx, ct);
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = DynamicCallEvaluator.EvaluateDynamicCallDirect(invoke, result, ctx, ct);
            else
            {
                var ma = seg.MemberAccess;
                if (ma.NullSafe && result == null) return null;
                result = ResolveMemberAccessWithTarget(ma, result, ctx, ct);
            }
        }

        return result;
    }

    internal static object?[] EvaluateArguments(ImmutableArray<BoundExpr> arguments, EvaluationContext ctx, CancellationToken ct)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];

        for (var i = 0; i < argumentCount; i++)
            values[i] = ctx.Evaluate(arguments[i], ct);

        return values;
    }

    internal static (object?[] Values, OutVariableBinding[] OutBindings) EvaluateArgumentsWithOutBindings(
        ImmutableArray<BoundExpr> arguments, EvaluationContext ctx, CancellationToken ct)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];
        List<OutVariableBinding>? bindings = null;

        for (var i = 0; i < argumentCount; i++)
        {
            var argument = arguments[i];
            values[i] = ctx.Evaluate(argument, ct);
            if (argument is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return (values, bindings?.ToArray() ?? []);
    }

    internal static void DefineOutVariablesIfAny(object?[] args, OutVariableBinding[] outBindings, EvaluationContext ctx, CancellationToken ct)
    {
        if (outBindings.Length > 0)
            IdentifierRuntime.DefineOutVariables(args, outBindings, ctx.Context);
    }

    internal static object? ResolveMemberAccessWithTarget(BoundMemberAccessBase ma, object? target, EvaluationContext ctx, CancellationToken ct)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => ResolvePropertyAccess(prop, target, ctx, ct),
            BoundFieldAccessExpr field => ResolveFieldAccess(field, target, ctx, ct),
            BoundMethodGroupExpr mg => target == null
                ? throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", mg.MethodName)
                : new MethodRef(target, mg.MethodName),
            BoundDynamicMemberAccessExpr dyn => MemberAccess.GetMember(target, dyn.MemberName, dyn.NullSafe, ctx.Context),
            _ => throw new BindingNotSupportedException($"Unexpected member access type '{ma.GetType().Name}'")
        };
    }

    internal static object? ResolvePropertyAccess(BoundPropertyAccessExpr node, object? target, EvaluationContext ctx, CancellationToken ct)
    {
        if (target == null)
        {
            var declaringType = node.Property.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType &&
                declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (node.MemberName == nameof(Nullable<int>.HasValue)) return (object)false;
                if (node.MemberName == nameof(Nullable<int>.Value))
                    throw new InvalidOperationException("Nullable object must have a value.");
            }

            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", node.MemberName);
        }
        if (TypeHelpers.IsValueTupleType(node.Property.DeclaringType ?? node.Property.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, node.NullSafe, ctx.Context);
        return TypeHelpers.GuardReflectionLeak(
            ctx.Context.TypeMetadata.GetPropertyValue(node.Property, target), "property", node.MemberName);
    }

    internal static object? ResolveFieldAccess(BoundFieldAccessExpr node, object? target, EvaluationContext ctx, CancellationToken ct)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "field", node.MemberName);
        if (TypeHelpers.IsValueTupleType(node.Field.DeclaringType ?? node.Field.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, node.NullSafe, ctx.Context);
        return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(target), "field", node.MemberName);
    }

    private static object CoerceExtensionReceiver(object receiver) =>
        receiver is Range or InclusiveRange ? RangeHelpers.EnsureEnumerable(receiver) : receiver;

    private static object? InvokeResolved(ResolvedCall resolved, object? target, object?[] args, EvaluationContext ctx, CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, ct);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);
        ArgumentPreparer.CopyBackOutArgs(args, prepared, parameters);
        TypeHelpers.GuardReflectionLeak(result, "method", resolved.Method.Name);
        ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
        return result;
    }

    private static object? InvokeResolvedExtension(ResolvedCall resolved, object?[] args, EvaluationContext ctx, CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, ct);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, null, prepared);
        TypeHelpers.GuardReflectionLeak(result, "method", resolved.Method.Name);
        ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
        return result;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundResolvedCallExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return await EvaluatePostfixChainAsync(chain.Value, ctx, ct);

        return await EvaluateResolvedCallDirectAsync(node, null, ctx, ct);
    }

    internal static async ValueTask<object?> EvaluateResolvedCallDirectAsync(BoundResolvedCallExpr call, object? evaluatedTarget, EvaluationContext ctx, CancellationToken ct)
    {
        if (call is { IsExtensionCall: true, Callee: BoundMethodGroupExpr extMethodGroup })
        {
            if (extMethodGroup.NullSafe)
            {
                var nullCheckTarget = evaluatedTarget ?? await ctx.EvaluateAsync(call.Arguments[0], ct);
                if (nullCheckTarget == null) return null;
            }

            object?[] extensionArgs;
            if (evaluatedTarget != null)
            {
                extensionArgs = new object?[call.Arguments.Length];
                extensionArgs[0] = CoerceExtensionReceiver(evaluatedTarget);
                for (var i = 1; i < call.Arguments.Length; i++)
                    extensionArgs[i] = await ctx.EvaluateAsync(call.Arguments[i], ct);
            }
            else
            {
                extensionArgs = await EvaluateArgumentsAsync(call.Arguments, ctx, ct);
                if (extensionArgs.Length > 0 && extensionArgs[0] != null)
                    extensionArgs[0] = CoerceExtensionReceiver(extensionArgs[0]!);
            }

            return InvokeResolvedExtension(call.Resolution, extensionArgs, ctx, ct);
        }

        if (call.Callee is BoundMethodGroupExpr methodGroup)
        {
            var target = evaluatedTarget ?? (methodGroup.IsStatic ? null : await ctx.EvaluateAsync(methodGroup.Target, ct));
            if (methodGroup.NullSafe && target == null)
                return null;

            var plannedArgs = await EvaluateArgumentsAsync(call.Arguments, ctx, ct);
            return InvokeResolved(call.Resolution, target, plannedArgs, ctx, ct);
        }

        var (args, outBindings) = await EvaluateArgumentsWithOutBindingsAsync(call.Arguments, ctx, ct);
        var callee = await ctx.EvaluateAsync(call.Callee, ct);
        var invokeResult = MethodInvoker.InvokeCall(callee, args, ctx.Context, ct: ct);
        DefineOutVariablesIfAny(args, outBindings, ctx, ct);
        ExecutionRuntime.CheckCollectionSize(invokeResult, ctx.Context.Config.Security);
        return invokeResult;
    }

    internal static async ValueTask<object?> EvaluatePostfixChainAsync(PostfixChain.Chain chain, EvaluationContext ctx, CancellationToken ct)
    {
        var result = await ctx.EvaluateAsync(chain.Root, ct);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
                result = await EvaluateResolvedCallDirectAsync(call, result, ctx, ct);
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = await DynamicCallEvaluator.EvaluateDynamicCallDirectAsync(invoke, result, ctx, ct);
            else
            {
                var ma = seg.MemberAccess;
                if (ma.NullSafe && result == null) return null;
                result = ResolveMemberAccessWithTarget(ma, result, ctx, ct);
            }
        }

        return result;
    }

    internal static async ValueTask<object?[]> EvaluateArgumentsAsync(ImmutableArray<BoundExpr> arguments, EvaluationContext ctx, CancellationToken ct)
    {
        var values = new object?[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
            values[i] = await ctx.EvaluateAsync(arguments[i], ct);
        return values;
    }

    internal static async ValueTask<(object?[] Values, OutVariableBinding[] OutBindings)> EvaluateArgumentsWithOutBindingsAsync(
        ImmutableArray<BoundExpr> arguments, EvaluationContext ctx, CancellationToken ct)
    {
        var values = new object?[arguments.Length];
        List<OutVariableBinding>? bindings = null;

        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            values[i] = await ctx.EvaluateAsync(argument, ct);
            if (argument is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return (values, bindings?.ToArray() ?? []);
    }
}
