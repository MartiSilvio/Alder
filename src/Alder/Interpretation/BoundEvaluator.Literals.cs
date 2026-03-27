using Alder.Runtime.Extensions;
using System.Dynamic;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateArrayLiteral(BoundArrayLiteralExpr arrayLiteral)
    {
        var result = new List<object?>(arrayLiteral.Elements.Length);
        foreach (var element in arrayLiteral.Elements)
        {
            if (element is BoundSpreadExpr spread)
            {
                var spreadValue = Evaluate(spread.Expression);
                SpreadHelpers.SpreadIntoList(result, spreadValue);
            }
            else
            {
                result.Add(Evaluate(element));
            }
        }

        return SpreadHelpers.CreateTypedArray(result);
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
                SpreadHelpers.SpreadIntoDict(result, spreadValue, _context);
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

        var type = _context.TypeResolver.ResolveType(objectCreation.TypeName);
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

    private object? EvaluateTypedArrayCreation(BoundTypedArrayCreationExpr typedArrayCreation)
    {
        var sizeValue = Evaluate(typedArrayCreation.Size);
        var size = Convert.ToInt32(sizeValue);
        var elementType = _context.TypeResolver.ResolveType(typedArrayCreation.ElementTypeName);
        return RuntimeArrayFactory.Create(elementType, size);
    }

    private object? EvaluateTypedArrayLiteral(BoundTypedArrayLiteralExpr typedArrayLiteral)
    {
        var elementType = _context.TypeResolver.ResolveType(typedArrayLiteral.ElementTypeName);
        var array = RuntimeArrayFactory.Create(elementType, typedArrayLiteral.Elements.Length);
        for (var i = 0; i < typedArrayLiteral.Elements.Length; i++)
        {
            var value = Evaluate(typedArrayLiteral.Elements[i]);
            array.SetValue(value, i);
        }

        return array;
    }

    private object? EvaluateMultiDimTypedArrayCreation(BoundMultiDimTypedArrayCreationExpr multiDimTypedArrayCreation)
    {
        var sizes = new int[multiDimTypedArrayCreation.Sizes.Length];
        for (var i = 0; i < multiDimTypedArrayCreation.Sizes.Length; i++)
            sizes[i] = Convert.ToInt32(Evaluate(multiDimTypedArrayCreation.Sizes[i]));
        var elementType = _context.TypeResolver.ResolveType(multiDimTypedArrayCreation.ElementTypeName);
        return RuntimeArrayFactory.Create(elementType, sizes);
    }

    private object? EvaluateMultiDimArrayInit(BoundMultiDimArrayInitExpr init)
    {
        var dimensions = init.InferredDimensions;
        if (init.ExplicitSizes != null)
        {
            for (var i = 0; i < init.ExplicitSizes.Value.Length; i++)
                dimensions[i] = Convert.ToInt32(Evaluate(init.ExplicitSizes.Value[i]));
        }

        var elementType = _context.TypeResolver.ResolveType(init.ElementTypeName);
        var array = RuntimeArrayFactory.Create(elementType, dimensions);

        // Fill the array from the flat value list using row-major order
        var indices = new int[init.Rank];
        for (var i = 0; i < init.FlatValues.Length; i++)
        {
            var value = Evaluate(init.FlatValues[i]);
            if (value != null)
                value = Convert.ChangeType(value, elementType);
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
