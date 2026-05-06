using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ArrayAllocation)]
internal static class ArrayAllocEmitter
{
    public static LinqExpression Emit(BoundArrayAllocationExpr node, EmissionContext ctx)
    {
        if (node.Sizes.Length == 1)
        {
            var sizeExpr = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Sizes[0]), typeof(int));
            var maxLengthExpr = LinqExpression.Property(
                LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Security)),
                nameof(Security.SecurityPolicy.MaxCollectionSize));
            return LinqExpression.Call(
                typeof(RuntimeArrayFactory).GetMethod(nameof(RuntimeArrayFactory.Create),
                    [typeof(Type), typeof(int), typeof(int)])!,
                LinqExpression.Constant(node.ElementType, typeof(Type)),
                sizeExpr,
                maxLengthExpr);
        }

        var sizeExprs = node.Sizes.Select(
            size => EmitHelpers.EnsureTypedExpression(ctx.Emit(size), typeof(int)));
        return LinqExpression.NewArrayBounds(node.ElementType, sizeExprs);
    }
}
