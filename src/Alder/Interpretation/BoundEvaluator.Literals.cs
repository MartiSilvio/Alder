using Alder.Runtime.Extensions;
using System.Dynamic;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateCollectionCreation(BoundCollectionCreationExpr collection)
    {
        var values = new List<object?>(collection.Elements.Length);
        foreach (var element in collection.Elements)
        {
            if (element is BoundSpreadExpr spread)
            {
                var spreadValue = Evaluate(spread.Expression);
                CollectionFactory.SpreadIntoList(values, spreadValue);
            }
            else
            {
                values.Add(Evaluate(element));
            }
        }

        return collection.CollectionKind switch
        {
            CollectionKind.Array => RuntimeArrayFactory.CreateFromValues(collection.ElementType, values),
            CollectionKind.InferredArray => RuntimeArrayFactory.InferAndCreateArray(values),
            CollectionKind.TargetTypedCollection => CollectionFactory.Create(
                collection.TargetCollectionType!, collection.ElementType, values),
            _ => throw new InvalidOperationException()
        };
    }

    private static object? EvaluateSpread(BoundSpreadExpr _)
    {
        throw new AlderException(DiagnosticDescriptors.SpreadOutsideLiteral);
    }

    private object? EvaluateObjectLiteral(BoundObjectLiteralExpr objectLiteral)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var property in objectLiteral.Properties)
        {
            if (property.IsSpread)
            {
                var spreadValue = Evaluate(property.Value);
                CollectionFactory.SpreadIntoDict(result, spreadValue, _context);
                continue;
            }

            result[property.PropertyName!] = Evaluate(property.Value);
        }

        return result;
    }

    private object? EvaluateSlice(BoundSliceExpr slice)
    {
        var target = Evaluate(slice.Target);
        var start = slice.Start != null ? Evaluate(slice.Start) : null;
        var end = slice.End != null ? Evaluate(slice.End) : null;
        var step = slice.Step != null ? Evaluate(slice.Step) : null;
        return MemberAccess.GetSlice(target, start, end, step);
    }

    private object? EvaluateObjectCreation(BoundObjectCreationExpr objectCreation)
    {
        var args = new object?[objectCreation.Arguments.Length];
        for (var i = 0; i < objectCreation.Arguments.Length; i++)
            args[i] = Evaluate(objectCreation.Arguments[i]);

        var type = objectCreation.StaticType is BoundUnknownType
            ? _context.TypeResolver.ResolveType(objectCreation.TypeName)
            : objectCreation.StaticType.ClrType;
        var result = ConstructionRuntime.InvokeConstructor(type, args, _config);

        foreach (var entry in objectCreation.InitializerEntries)
        {
            var value = Evaluate(entry.Value);
            if (entry.PropertyName != null)
            {
                MemberAccess.SetMember(result!, entry.PropertyName, value, _config, _context);
            }
            else if (entry.IndexerKey != null)
            {
                var key = Evaluate(entry.IndexerKey);
                MemberAccess.SetIndex(result!, key!, value, _config, _context);
            }
            else
            {
                Runtime.MethodInvoker.InvokeMemberCall(result!, "Add", [value], false, _context, _config, null, default);
            }
        }

        return result;
    }

    private object? EvaluateArrayAllocation(BoundArrayAllocationExpr allocation)
    {
        var sizes = new int[allocation.Sizes.Length];
        for (var i = 0; i < allocation.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(Evaluate(allocation.Sizes[i]));

        return sizes.Length == 1
            ? RuntimeArrayFactory.Create(allocation.ElementType, sizes[0], _config.Security.MaxCollectionSize)
            : RuntimeArrayFactory.Create(allocation.ElementType, sizes);
    }

    private object? EvaluateMultiDimArrayInit(BoundMultiDimArrayInitExpr init)
    {
        var dimensions = init.InferredDimensions;
        if (init.ExplicitSizes != null)
        {
            for (var i = 0; i < init.ExplicitSizes.Value.Length; i++)
                dimensions[i] = Convert.ToInt32(Evaluate(init.ExplicitSizes.Value[i]));
        }

        var array = RuntimeArrayFactory.Create(init.ElementType, dimensions);

        // Fill the array from the flat value list using row-major order
        var indices = new int[init.Rank];
        for (var i = 0; i < init.FlatValues.Length; i++)
        {
            var value = Evaluate(init.FlatValues[i]);
            if (value != null)
                value = Convert.ChangeType(value, init.ElementType);
            array.SetValue(value, indices);

            // Increment indices (rightmost first)
            for (var d = init.Rank - 1; d >= 0; d--)
            {
                indices[d]++;
                if (indices[d] < dimensions[d])
                    break;
                indices[d] = 0;
            }
        }

        return array;
    }

    private object? EvaluateTuple(BoundTupleExpr tuple)
    {
        var values = new object?[tuple.Elements.Length];
        for (var i = 0; i < tuple.Elements.Length; i++)
            values[i] = Evaluate(tuple.Elements[i]);

        var resolvedType = tuple.StaticType.ClrType;
        var result = resolvedType != typeof(object) && TypeHelpers.IsValueTupleType(resolvedType)
            ? ConstructionRuntime.CreateTupleFromResolvedType(resolvedType, values, _config)
            : ConstructionRuntime.CreateTuple(values);

        var hasNames = tuple.ElementNames.Any(static n => n != null);
        if (hasNames)
        {
            var nameMap = new Dictionary<string, int>();
            for (var i = 0; i < tuple.ElementNames.Length; i++)
            {
                if (tuple.ElementNames[i] is { } name)
                    nameMap[name] = i;
            }
            return new NamedTupleValue(result, nameMap);
        }

        return result;
    }

    private object? EvaluateRange(BoundRangeExpr range)
    {
        var startValue = range.Start != null ? Evaluate(range.Start) : null;
        var endValue = range.End != null ? Evaluate(range.End) : null;
        var sysRange = ConstructionRuntime.CreateSystemRange(startValue, endValue);
        return range.ExclusiveEnd ? sysRange : new InclusiveRange(sysRange);
    }
}
