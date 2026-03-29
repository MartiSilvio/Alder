using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class CollectionCreationEmitter : INodeEmitter<BoundCollectionCreationExpr>
{
    public Expression Emit(BoundCollectionCreationExpr node, EmissionContext ctx)
    {
        var hasSpread = node.Elements.Any(static e => e is BoundSpreadExpr);
        if (node.CollectionKind == CollectionKind.Array && !hasSpread)
        {
            var elements = node.Elements.Select(
                element => EmitHelpers.EnsureTypedExpression(ctx.Emit(element), node.ElementType));
            return LinqExpression.NewArrayInit(node.ElementType, elements);
        }

        var listVar = LinqExpression.Variable(typeof(List<object?>), "items");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(ListCtor))
        };

        foreach (var element in node.Elements)
        {
            if (element is BoundSpreadExpr spread)
            {
                statements.Add(LinqExpression.Call(
                    SpreadIntoListMethod, listVar, EmitHelpers.AsObject(ctx.Emit(spread.Expression))));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, ListAddMethod, EmitHelpers.AsObject(ctx.Emit(element))));
            }
        }

        switch (node.CollectionKind)
        {
            case CollectionKind.Array:
                statements.Add(LinqExpression.Call(CreateFromValuesMethod, LinqExpression.Constant(node.ElementType), listVar));
                break;
            case CollectionKind.InferredArray:
                statements.Add(LinqExpression.Call(InferAndCreateArrayMethod, listVar));
                break;
            case CollectionKind.TargetTypedCollection:
                statements.Add(LinqExpression.Call(
                    CollectionFactoryCreateMethod,
                    LinqExpression.Constant(node.TargetCollectionType!),
                    LinqExpression.Constant(node.ElementType),
                    listVar));
                break;
        }

        return LinqExpression.Block(typeof(object), [listVar], statements);
    }
}
