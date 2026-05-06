using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Alder.Generators;

/// <summary>
/// Defines the built-in types that receive generated <c>TypeMetadata</c> dispatch.
/// The catalog stays intentionally small. Broader rooting is handled elsewhere, and application types are expected to come
/// from explicit registration rather than from an ever-growing built-in list.
/// </summary>
internal static class BuiltInTypeCatalog
{
    private static readonly SpecialType[] Primitives =
    {
        SpecialType.System_Int32, SpecialType.System_Int64,
        SpecialType.System_Double, SpecialType.System_Single, SpecialType.System_Decimal,
        SpecialType.System_Boolean, SpecialType.System_Char, SpecialType.System_Byte,
        SpecialType.System_Int16, SpecialType.System_UInt16,
        SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_SByte,
        SpecialType.System_String, SpecialType.System_Object,
    };

    private static readonly string[] UtilityTypes =
    {
        "System.DateTime", "System.TimeSpan", "System.Guid",
        "System.Math", "System.Convert", "System.Environment",
        "System.Exception", "System.ArgumentException",
        "System.ArgumentNullException", "System.InvalidOperationException",
        "System.Array", "System.Threading.Tasks.Task", "System.Threading.Tasks.ValueTask",
    };

    // Closed generic shapes that are common enough to justify built-in dispatch support.
    // Each entry is expressed as [openType, arg1, arg2, ...].
    private static readonly string[][] DispatchGenerics =
    {
        new[] { "System.Collections.Generic.List`1", "System.Int32" },
        new[] { "System.Collections.Generic.List`1", "System.Int64" },
        new[] { "System.Collections.Generic.List`1", "System.Double" },
        new[] { "System.Collections.Generic.List`1", "System.String" },
        new[] { "System.Collections.Generic.List`1", "System.Object" },
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.Object" },
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.String" },
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.Int32" },
        new[] { "System.Collections.Generic.KeyValuePair`2", "System.String", "System.Int32" },
        new[] { "System.Collections.Generic.HashSet`1", "System.Int32" },
        new[] { "System.Collections.Generic.HashSet`1", "System.String" },
        new[] { "System.Collections.Generic.Queue`1", "System.Int32" },
        new[] { "System.Collections.Generic.Queue`1", "System.String" },
        new[] { "System.Collections.Generic.Stack`1", "System.Int32" },
        new[] { "System.Collections.Generic.Stack`1", "System.String" },
        new[] { "System.Threading.Tasks.Task`1", "System.Int16" },
        new[] { "System.Threading.Tasks.Task`1", "System.Int32" },
        new[] { "System.Threading.Tasks.Task`1", "System.Int64" },
        new[] { "System.Threading.Tasks.Task`1", "System.UInt16" },
        new[] { "System.Threading.Tasks.Task`1", "System.UInt32" },
        new[] { "System.Threading.Tasks.Task`1", "System.UInt64" },
        new[] { "System.Threading.Tasks.Task`1", "System.Byte" },
        new[] { "System.Threading.Tasks.Task`1", "System.SByte" },
        new[] { "System.Threading.Tasks.Task`1", "System.Single" },
        new[] { "System.Threading.Tasks.Task`1", "System.Double" },
        new[] { "System.Threading.Tasks.Task`1", "System.Decimal" },
        new[] { "System.Threading.Tasks.Task`1", "System.Boolean" },
        new[] { "System.Threading.Tasks.Task`1", "System.Char" },
        new[] { "System.Threading.Tasks.Task`1", "System.String" },
        new[] { "System.Threading.Tasks.Task`1", "System.Object" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Int16" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Int32" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Int64" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.UInt16" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.UInt32" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.UInt64" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Byte" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.SByte" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Single" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Double" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Decimal" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Boolean" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Char" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.String" },
        new[] { "System.Threading.Tasks.ValueTask`1", "System.Object" },
    };

    public static ImmutableArray<INamedTypeSymbol> Resolve(Compilation compilation)
    {
        var result = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var st in Primitives)
        {
            var symbol = compilation.GetSpecialType(st);
            if (symbol != null && symbol.SpecialType != SpecialType.None && seen.Add(symbol))
                result.Add(symbol);
        }

        foreach (var name in UtilityTypes)
            TryAdd(compilation.GetTypeByMetadataName(name), seen, result);

        foreach (var spec in DispatchGenerics)
            TryAddClosed(compilation, spec, seen, result);

        AddTupleShapes(compilation, seen, result);

        return result.ToImmutableArray();
    }

    /// <summary>
    /// Returns the value-type pool used for AOT rooting and generic expansion.
    /// This pool is broader than the dispatch catalog and serves a different purpose.
    /// </summary>
    internal static ImmutableArray<INamedTypeSymbol> ResolveValueTypePool(Compilation compilation)
    {
        var pool = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (var st in Primitives)
        {
            var symbol = compilation.GetSpecialType(st);
            if (symbol != null && symbol.IsValueType)
                pool.Add(symbol);
        }
        return pool.ToImmutable();
    }

    private static void TryAddClosed(
        Compilation compilation, string[] spec,
        HashSet<INamedTypeSymbol> seen, List<INamedTypeSymbol> result)
    {
        var openType = compilation.GetTypeByMetadataName(spec[0]);
        if (openType == null) return;

        var typeArgs = new ITypeSymbol[spec.Length - 1];
        for (var i = 1; i < spec.Length; i++)
        {
            var arg = compilation.GetTypeByMetadataName(spec[i]);
            if (arg == null) return;
            typeArgs[i - 1] = arg;
        }

        TryAdd(openType.Construct(typeArgs), seen, result);
    }

    private static void TryAdd(INamedTypeSymbol? symbol, HashSet<INamedTypeSymbol> seen, List<INamedTypeSymbol> result)
    {
        if (symbol != null && seen.Add(symbol))
            result.Add(symbol);
    }

    private static void AddTupleShapes(
        Compilation compilation,
        HashSet<INamedTypeSymbol> seen,
        List<INamedTypeSymbol> result)
    {
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var longType = compilation.GetSpecialType(SpecialType.System_Int64);
        var doubleType = compilation.GetSpecialType(SpecialType.System_Double);
        var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);

        if (HasMissingSymbols(intType, longType, doubleType, boolType, stringType, objectType))
            return;

        var pairPool = new ITypeSymbol[] { intType, longType, doubleType, boolType, stringType, objectType };
        foreach (var first in pairPool)
        foreach (var second in pairPool)
            TryAdd(CreateTupleType(compilation, [first, second]), seen, result);

        TryAdd(CreateTupleType(compilation, [intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, stringType, boolType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, stringType, doubleType]), seen, result);
        TryAdd(CreateTupleType(compilation, [stringType, intType, doubleType]), seen, result);

        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType, intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType, intType, intType, intType, intType]), seen, result);
        TryAdd(CreateTupleType(compilation, [intType, intType, intType, intType, intType, intType, intType, intType, intType]), seen, result);

        var tupleIntInt = CreateTupleType(compilation, [intType, intType]);
        if (tupleIntInt != null)
        {
            TryAdd(CreateTupleType(compilation, [tupleIntInt, intType]), seen, result);

            var listOpen = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            if (listOpen != null)
                TryAdd(listOpen.Construct(tupleIntInt), seen, result);

            var taskOpen = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (taskOpen != null)
                TryAdd(taskOpen.Construct(tupleIntInt), seen, result);

            var valueTaskOpen = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            if (valueTaskOpen != null)
                TryAdd(valueTaskOpen.Construct(tupleIntInt), seen, result);
        }

        var tupleIntIntInt = CreateTupleType(compilation, [intType, intType, intType]);
        if (tupleIntIntInt != null)
        {
            var taskOpen = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (taskOpen != null)
                TryAdd(taskOpen.Construct(tupleIntIntInt), seen, result);

            var valueTaskOpen = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            if (valueTaskOpen != null)
                TryAdd(valueTaskOpen.Construct(tupleIntIntInt), seen, result);
        }
    }

    private static INamedTypeSymbol? CreateTupleType(Compilation compilation, IReadOnlyList<ITypeSymbol> elementTypes)
    {
        return elementTypes.Count switch
        {
            0 => null,
            <= 7 => ConstructTuple(compilation, elementTypes),
            _ => CreateLongTupleType(compilation, elementTypes)
        };
    }

    private static INamedTypeSymbol? CreateLongTupleType(Compilation compilation, IReadOnlyList<ITypeSymbol> elementTypes)
    {
        var head = new ITypeSymbol[8];
        for (var i = 0; i < 7; i++)
            head[i] = elementTypes[i];

        var tail = new ITypeSymbol[elementTypes.Count - 7];
        for (var i = 7; i < elementTypes.Count; i++)
            tail[i - 7] = elementTypes[i];

        var rest = CreateTupleType(compilation, tail);
        if (rest == null)
            return null;

        head[7] = rest;
        return ConstructTuple(compilation, head);
    }

    private static INamedTypeSymbol? ConstructTuple(Compilation compilation, IReadOnlyList<ITypeSymbol> typeArguments)
    {
        var open = compilation.GetTypeByMetadataName($"System.ValueTuple`{typeArguments.Count}");
        return open?.Construct([.. typeArguments]);
    }

    private static bool HasMissingSymbols(params ITypeSymbol[] symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol == null || symbol.SpecialType == SpecialType.None)
                return true;
        }

        return false;
    }
}
