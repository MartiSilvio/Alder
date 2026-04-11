using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.TypedLambda)]
internal static class TypedLambdaEmitter
{
    private static readonly System.Reflection.MethodInfo ThrowIfCancellationRequestedMethod =
        typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;

    public static LinqExpression Emit(BoundTypedLambdaExpr node, EmissionContext ctx)
    {
        var lambdaParams = new ParameterExpression[node.Parameters.Length];
        for (var i = 0; i < node.Parameters.Length; i++)
            lambdaParams[i] = LinqExpression.Parameter(node.Parameters[i].Type, node.Parameters[i].Name);

        ctx.PushLambdaScope(node.Parameters, lambdaParams);
        try
        {
            var body = ctx.Emit(node.Body);

            var invokeMethod = node.DelegateType.GetMethod("Invoke")!;
            var returnType = invokeMethod.ReturnType;

            if (returnType == typeof(void))
            {
                if (body.Type != typeof(void))
                    body = LinqExpression.Block(typeof(void), body);
            }
            else if (body.Type != returnType)
            {
                body = EmitHelpers.EnsureTypedExpression(body, returnType);
            }

            // Inject cancellation check so LINQ predicates/selectors respect CancellationToken
            var cancellationCheck = LinqExpression.Call(
                ctx.CancellationTokenParam, ThrowIfCancellationRequestedMethod);
            body = LinqExpression.Block(body.Type, cancellationCheck, body);

            return LinqExpression.Lambda(node.DelegateType, body, lambdaParams);
        }
        finally
        {
            ctx.PopLambdaScope();
        }
    }
}
