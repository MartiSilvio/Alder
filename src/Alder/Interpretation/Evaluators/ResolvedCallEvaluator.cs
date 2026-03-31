using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ResolvedCall)]
internal static class ResolvedCallEvaluator
{
    public static object? Evaluate(BoundResolvedCallExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null)
            return EvaluatePostfixChain(chain.Value, ctx);

        return EvaluateResolvedCallDirect(node, null, ctx);
    }

    internal static object? EvaluateResolvedCallDirect(BoundResolvedCallExpr call, object? evaluatedTarget, EvaluationContext ctx)
    {
        if (call.Callee is BoundMethodGroupExpr methodGroup)
        {
            var target = evaluatedTarget ?? (methodGroup.IsStatic ? null : ctx.Evaluate(methodGroup.Target));
            if (methodGroup.NullSafe && target == null)
                return null;

            var plannedArgs = EvaluateArguments(call.Arguments, ctx);

            var resolved = call.Resolution;
            var parameters = MethodDispatchCache.GetParameters(resolved.Method);
            var prepared = ArgumentPreparer.Prepare(resolved, plannedArgs, parameters, ctx.CancellationToken);
            var plannedResult = MethodInvoker.InvokeMethodCore(resolved.Method, target, prepared);
            ArgumentPreparer.CopyBackOutArgs(plannedArgs, prepared, parameters);
            ExecutionRuntime.CheckCollectionSize(plannedResult, ctx.Config.Security);
            return plannedResult;
        }

        var (args, outBindings) = EvaluateArgumentsWithOutBindings(call.Arguments, ctx);

        var callee = ctx.Evaluate(call.Callee);
        var invokeResult = MethodInvoker.InvokeCall(callee, args, ctx.Context, ctx.Config, ct: ctx.CancellationToken);
        DefineOutVariablesIfAny(args, outBindings, ctx);
        ExecutionRuntime.CheckCollectionSize(invokeResult, ctx.Config.Security);
        return invokeResult;
    }

    internal static object? EvaluatePostfixChain(PostfixChain.Chain chain, EvaluationContext ctx)
    {
        var result = ctx.Evaluate(chain.Root);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
                result = EvaluateResolvedCallDirect(call, result, ctx);
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
                result = DynamicCallEvaluator.EvaluateDynamicCallDirect(invoke, result, ctx);
            else
            {
                var ma = seg.MemberAccess;
                if (ma.NullSafe && result == null) return null;
                result = ResolveMemberAccessWithTarget(ma, result, ctx);
            }
        }

        return result;
    }

    internal static object?[] EvaluateArguments(ImmutableArray<BoundExpr> arguments, EvaluationContext ctx)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];

        for (var i = 0; i < argumentCount; i++)
            values[i] = ctx.Evaluate(arguments[i]);

        return values;
    }

    internal static (object?[] Values, OutVariableBinding[] OutBindings) EvaluateArgumentsWithOutBindings(
        ImmutableArray<BoundExpr> arguments, EvaluationContext ctx)
    {
        var argumentCount = arguments.Length;
        var values = new object?[argumentCount];
        List<OutVariableBinding>? bindings = null;

        for (var i = 0; i < argumentCount; i++)
        {
            var argument = arguments[i];
            values[i] = ctx.Evaluate(argument);
            if (argument is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return (values, bindings?.ToArray() ?? []);
    }

    internal static void DefineOutVariablesIfAny(object?[] args, OutVariableBinding[] outBindings, EvaluationContext ctx)
    {
        if (outBindings.Length > 0)
            IdentifierRuntime.DefineOutVariables(args, outBindings, ctx.Context);
    }

    internal static object? ResolveMemberAccessWithTarget(BoundMemberAccessBase ma, object? target, EvaluationContext ctx)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => ResolvePropertyAccess(prop, target, ctx),
            BoundFieldAccessExpr field => ResolveFieldAccess(field, target, ctx),
            BoundMethodGroupExpr mg => target == null
                ? throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "method", mg.MethodName)
                : new MethodRef(target, mg.MethodName),
            BoundDynamicMemberAccessExpr dyn => MemberAccess.GetMember(target, dyn.MemberName, ctx.Config, dyn.NullSafe, ctx.Context),
            _ => throw new BindingNotSupportedException($"Unexpected member access type '{ma.GetType().Name}'")
        };
    }

    internal static object? ResolvePropertyAccess(BoundPropertyAccessExpr node, object? target, EvaluationContext ctx)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", node.MemberName);
        if (TypeHelpers.IsValueTupleType(node.Property.DeclaringType ?? node.Property.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, ctx.Config, node.NullSafe, ctx.Context);
        return TypeHelpers.GuardReflectionLeak(
            ctx.Context.TypeMetadata.GetPropertyValue(node.Property, target), "property", node.MemberName);
    }

    internal static object? ResolveFieldAccess(BoundFieldAccessExpr node, object? target, EvaluationContext ctx)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "field", node.MemberName);
        if (TypeHelpers.IsValueTupleType(node.Field.DeclaringType ?? node.Field.ReflectedType!))
            return MemberAccess.GetMember(target, node.MemberName, ctx.Config, node.NullSafe, ctx.Context);
        return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(target), "field", node.MemberName);
    }
}
