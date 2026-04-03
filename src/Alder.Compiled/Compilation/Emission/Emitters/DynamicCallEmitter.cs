using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class DynamicCallEmitter : INodeEmitter<BoundDynamicCallExpr>
{
    public LinqExpression Emit(BoundDynamicCallExpr node, EmissionContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEmitter.EmitPostfixChain(chain.Value, ctx);
        return ResolvedCallEmitter.EmitCollectionSizeCheck(
            EmitInvokeCore(node.Callee, node.Arguments, node.TypeArguments, null, ctx), ctx);
    }

    internal static LinqExpression EmitInvokeCore(
        BoundExpr callee,
        ImmutableArray<BoundExpr> arguments,
        ImmutableArray<string> typeArguments,
        LinqExpression? emittedCalleeTarget,
        EmissionContext ctx)
    {
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            arguments.Select(argument => ctx.EmitBoxed(argument)));
        var emittedTypeArguments = EmitTypeArguments(typeArguments);
        var outBindings = EmitHelpers.CollectOutBindings(arguments);

        LinqExpression invokeExpr;
        if (callee is BoundIdentifierExpr identifier &&
            !(identifier.LocalId is { } localId && ctx.TryGetPromoted(localId, out _)))
        {
            invokeExpr = LinqExpression.Call(
                InvokeIdentifierCallMethod,
                LinqExpression.Constant(identifier.Name),
                argsVar,
                ctx.ContextParam,
                emittedTypeArguments,
                ctx.CancellationTokenParam);
        }
        else if (callee is BoundMemberAccessBase memberAccess)
        {
            invokeExpr = LinqExpression.Call(
                InvokeMemberCallMethod,
                EmitHelpers.AsObject(emittedCalleeTarget ?? ctx.Emit(memberAccess.Target)),
                LinqExpression.Constant(memberAccess.MemberName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                ctx.ContextParam,
                emittedTypeArguments,
                ctx.CancellationTokenParam);
        }
        else
        {
            invokeExpr = LinqExpression.Call(
                InvokeCallMethod,
                ctx.EmitBoxed(callee),
                argsVar,
                ctx.ContextParam,
                emittedTypeArguments,
                ctx.CancellationTokenParam);
        }

        if (outBindings.Length == 0)
        {
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                invokeExpr);
        }

        var resultVar = LinqExpression.Variable(typeof(object), "invokeResult");
        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, invokeExpr),
            LinqExpression.Call(
                DefineOutVariablesMethod,
                argsVar,
                LinqExpression.Constant(outBindings, typeof(IReadOnlyList<OutVariableBinding>)),
                ctx.ContextParam),
            resultVar);
    }

    private static LinqExpression EmitTypeArguments(ImmutableArray<string> typeArguments)
    {
        if (typeArguments.IsDefaultOrEmpty)
            return LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        return LinqExpression.Constant(typeArguments.ToArray(), typeof(IReadOnlyList<string>));
    }
}
