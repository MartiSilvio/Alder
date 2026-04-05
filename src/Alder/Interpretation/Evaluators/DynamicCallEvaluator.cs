using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DynamicCall)]
internal static class DynamicCallEvaluator
{
    public static object? Evaluate(BoundDynamicCallExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx, ct);

        return EvaluateDynamicCallDirect(node, null, ctx, ct);
    }

    internal static object? EvaluateDynamicCallDirect(BoundDynamicCallExpr invoke, object? evaluatedTarget, EvaluationContext ctx, CancellationToken ct)
    {
        var (args, outBindings) = ResolvedCallEvaluator.EvaluateArgumentsWithOutBindings(invoke.Arguments, ctx, ct);
        return DispatchDynamicCall(invoke, evaluatedTarget, args, outBindings, ctx, ct);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundDynamicCallExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return await ResolvedCallEvaluator.EvaluatePostfixChainAsync(chain.Value, ctx, ct);

        return await EvaluateDynamicCallDirectAsync(node, null, ctx, ct);
    }

    internal static async ValueTask<object?> EvaluateDynamicCallDirectAsync(BoundDynamicCallExpr invoke, object? evaluatedTarget, EvaluationContext ctx, CancellationToken ct)
    {
        var (args, outBindings) = await ResolvedCallEvaluator.EvaluateArgumentsWithOutBindingsAsync(invoke.Arguments, ctx, ct);
        return await DispatchDynamicCallAsync(invoke, evaluatedTarget, args, outBindings, ctx, ct);
    }

    private static object? DispatchDynamicCall(
        BoundDynamicCallExpr invoke, object? evaluatedTarget,
        object?[] args, OutVariableBinding[] outBindings, EvaluationContext ctx, CancellationToken ct)
    {
        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name, args, ctx.Context, typeArguments, ct);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
            TypeHelpers.GuardReflectionLeak(result, "method", identifier.Name);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessBase memberAccess)
        {
            var target = evaluatedTarget ?? ctx.Evaluate(memberAccess.Target, ct);
            var result = MethodInvoker.InvokeMemberCall(
                target, memberAccess.MemberName, args, memberAccess.NullSafe,
                ctx.Context, typeArguments, ct);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
            TypeHelpers.GuardReflectionLeak(result, "method", memberAccess.MemberName);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
            return result;
        }

        var callee = ctx.Evaluate(invoke.Callee, ct);
        var invokeCallResult = MethodInvoker.InvokeCall(
            callee, args, ctx.Context, typeArguments, ct);
        ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
        TypeHelpers.GuardReflectionLeak(invokeCallResult, "method invocation");
        ExecutionRuntime.CheckCollectionSize(invokeCallResult, ctx.Context.Config.Security);
        return invokeCallResult;
    }

    private static async ValueTask<object?> DispatchDynamicCallAsync(
        BoundDynamicCallExpr invoke, object? evaluatedTarget,
        object?[] args, OutVariableBinding[] outBindings, EvaluationContext ctx, CancellationToken ct)
    {
        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name, args, ctx.Context, typeArguments, ct);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
            TypeHelpers.GuardReflectionLeak(result, "method", identifier.Name);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessBase memberAccess)
        {
            var target = evaluatedTarget ?? await ctx.EvaluateAsync(memberAccess.Target, ct);
            var result = MethodInvoker.InvokeMemberCall(
                target, memberAccess.MemberName, args, memberAccess.NullSafe,
                ctx.Context, typeArguments, ct);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
            TypeHelpers.GuardReflectionLeak(result, "method", memberAccess.MemberName);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Context.Config.Security);
            return result;
        }

        var callee = await ctx.EvaluateAsync(invoke.Callee, ct);
        var invokeCallResult = MethodInvoker.InvokeCall(
            callee, args, ctx.Context, typeArguments, ct);
        ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx, ct);
        TypeHelpers.GuardReflectionLeak(invokeCallResult, "method invocation");
        ExecutionRuntime.CheckCollectionSize(invokeCallResult, ctx.Context.Config.Security);
        return invokeCallResult;
    }
}
