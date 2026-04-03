using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using MethodInvoker = Alder.Runtime.MethodInvoker;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ObjectCreationExpression)]
internal static class ObjectCreationEvaluator
{
    public static object? Evaluate(BoundObjectCreationExpr node, EvaluationContext ctx)
    {
        var args = new object?[node.Arguments.Length];
        for (var i = 0; i < node.Arguments.Length; i++)
            args[i] = ctx.Evaluate(node.Arguments[i]);

        var type = node.StaticType is BoundUnknownType
            ? ctx.Context.TypeResolver.ResolveType(node.TypeName)
            : node.StaticType.ClrType;
        var result = ConstructionRuntime.InvokeConstructor(type, args, ctx.Context);

        foreach (var entry in node.InitializerEntries)
        {
            if (entry.PropertyName != null)
            {
                var value = ctx.Evaluate(entry.Value);
                MemberAccess.SetMember(result!, entry.PropertyName, value, ctx.Context);
            }
            else if (entry.IndexerKey != null)
            {
                var value = ctx.Evaluate(entry.Value);
                var key = ctx.Evaluate(entry.IndexerKey);
                MemberAccess.SetIndex(result!, key!, value, ctx.Context);
            }
            else if (!entry.Elements.IsDefaultOrEmpty)
            {
                var elementArgs = new object?[entry.Elements.Length];
                for (var i = 0; i < entry.Elements.Length; i++)
                    elementArgs[i] = ctx.Evaluate(entry.Elements[i]);
                MethodInvoker.InvokeMemberCall(result!, "Add", elementArgs, false, ctx.Context, null, default);
            }
            else
            {
                var value = ctx.Evaluate(entry.Value);
                MethodInvoker.InvokeMemberCall(result!, "Add", [value], false, ctx.Context, null, default);
            }
        }

        return result;
    }
}
