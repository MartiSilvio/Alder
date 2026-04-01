using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DynamicCall)]
internal static class DynamicCallEvaluator
{
    public static object? Evaluate(BoundDynamicCallExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx);

        return EvaluateDynamicCallDirect(node, null, ctx);
    }

    internal static object? EvaluateDynamicCallDirect(BoundDynamicCallExpr invoke, object? evaluatedTarget, EvaluationContext ctx)
    {
        var (args, outBindings) = ResolvedCallEvaluator.EvaluateArgumentsWithOutBindings(invoke.Arguments, ctx);

        IReadOnlyList<string>? typeArguments = invoke.TypeArguments.IsDefaultOrEmpty
            ? null
            : invoke.TypeArguments;

        if (invoke.Callee is BoundIdentifierExpr identifier)
        {
            var result = IdentifierRuntime.InvokeIdentifierCall(
                identifier.Name, args, ctx.Context, ctx.Config, typeArguments, ctx.CancellationToken);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Config.Security);
            return result;
        }

        if (invoke.Callee is BoundMemberAccessBase memberAccess)
        {
            var target = evaluatedTarget ?? ctx.Evaluate(memberAccess.Target);
            var result = MethodInvoker.InvokeMemberCall(
                target, memberAccess.MemberName, args, memberAccess.NullSafe,
                ctx.Context, ctx.Config, typeArguments, ctx.CancellationToken);
            ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx);
            ExecutionRuntime.CheckCollectionSize(result, ctx.Config.Security);
            return result;
        }

        var callee = ctx.Evaluate(invoke.Callee);
        var invokeCallResult = MethodInvoker.InvokeCall(
            callee, args, ctx.Context, ctx.Config, typeArguments, ctx.CancellationToken);
        ResolvedCallEvaluator.DefineOutVariablesIfAny(args, outBindings, ctx);
        ExecutionRuntime.CheckCollectionSize(invokeCallResult, ctx.Config.Security);
        return invokeCallResult;
    }
}
