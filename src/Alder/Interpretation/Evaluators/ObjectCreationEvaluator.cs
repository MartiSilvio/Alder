using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

internal sealed class ObjectCreationEvaluator : INodeEvaluator<BoundObjectCreationExpr>
{
    public object? Evaluate(BoundObjectCreationExpr node, EvaluationContext ctx)
    {
        var args = new object?[node.Arguments.Length];
        for (var i = 0; i < node.Arguments.Length; i++)
            args[i] = ctx.Evaluate(node.Arguments[i]);

        var type = node.StaticType is BoundUnknownType
            ? ctx.Context.TypeResolver.ResolveType(node.TypeName)
            : node.StaticType.ClrType;
        var result = ConstructionRuntime.InvokeConstructor(type, args, ctx.Config);

        foreach (var entry in node.InitializerEntries)
        {
            var value = ctx.Evaluate(entry.Value);
            if (entry.PropertyName != null)
            {
                MemberAccess.SetMember(result!, entry.PropertyName, value, ctx.Config, ctx.Context);
            }
            else if (entry.IndexerKey != null)
            {
                var key = ctx.Evaluate(entry.IndexerKey);
                MemberAccess.SetIndex(result!, key!, value, ctx.Config, ctx.Context);
            }
            else
            {
                MethodInvoker.InvokeMemberCall(result!, "Add", [value], false, ctx.Context, ctx.Config, null, default);
            }
        }

        return result;
    }
}
