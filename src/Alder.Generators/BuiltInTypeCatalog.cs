using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Alder.Generators;

/// <summary>
/// Defines the types that get full TypeMetadata dispatch (TryInvoke, TryGet, TrySet).
/// This is intentionally small — only types every C# program touches.
/// AOT rooting for broader generic instantiations is handled separately by TypeRoots.
/// Users add domain types via [AlderRegistered].
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
        "System.Array", "System.Linq.Enumerable", "System.Threading.Tasks.Task",
    };

    // Closed generics that warrant full dispatch — the types that appear
    // in nearly every C# program. Format: [openType, arg1, arg2, ...]
    private static readonly string[][] DispatchGenerics =
    {
        // Lists
        new[] { "System.Collections.Generic.List`1", "System.Int32" },
        new[] { "System.Collections.Generic.List`1", "System.Int64" },
        new[] { "System.Collections.Generic.List`1", "System.Double" },
        new[] { "System.Collections.Generic.List`1", "System.String" },
        new[] { "System.Collections.Generic.List`1", "System.Object" },
        // Dictionaries
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.Object" },
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.String" },
        new[] { "System.Collections.Generic.Dictionary`2", "System.String", "System.Int32" },
        // Sets, queues, stacks
        new[] { "System.Collections.Generic.HashSet`1", "System.Int32" },
        new[] { "System.Collections.Generic.HashSet`1", "System.String" },
        new[] { "System.Collections.Generic.Queue`1", "System.Int32" },
        new[] { "System.Collections.Generic.Queue`1", "System.String" },
        new[] { "System.Collections.Generic.Stack`1", "System.Int32" },
        new[] { "System.Collections.Generic.Stack`1", "System.String" },
        // Tasks
        new[] { "System.Threading.Tasks.Task`1", "System.Int32" },
        new[] { "System.Threading.Tasks.Task`1", "System.String" },
        new[] { "System.Threading.Tasks.Task`1", "System.Object" },
        new[] { "System.Threading.Tasks.Task`1", "System.Boolean" },
        // Tuples — constructor dispatch avoids Activator.CreateInstance under AOT
        new[] { "System.ValueTuple`2", "System.Int32", "System.Int32" },
        new[] { "System.ValueTuple`2", "System.Int32", "System.String" },
        new[] { "System.ValueTuple`2", "System.Int32", "System.Double" },
        new[] { "System.ValueTuple`2", "System.Int32", "System.Int64" },
        new[] { "System.ValueTuple`2", "System.Int32", "System.Boolean" },
        new[] { "System.ValueTuple`2", "System.String", "System.Int32" },
        new[] { "System.ValueTuple`2", "System.String", "System.String" },
        new[] { "System.ValueTuple`2", "System.Boolean", "System.String" },
        new[] { "System.ValueTuple`2", "System.Double", "System.Double" },
        new[] { "System.ValueTuple`2", "System.Object", "System.Object" },
        new[] { "System.ValueTuple`3", "System.Int32", "System.Int32", "System.Int32" },
        new[] { "System.ValueTuple`3", "System.Int32", "System.String", "System.Boolean" },
        new[] { "System.ValueTuple`4", "System.Int32", "System.Int32", "System.Int32", "System.Int32" },
        new[] { "System.ValueTuple`5", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32" },
        new[] { "System.ValueTuple`6", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32" },
        new[] { "System.ValueTuple`7", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32" },
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

        return result.ToImmutableArray();
    }

    /// <summary>
    /// Returns value types used for AOT rooting — the element pool for TypeRoots
    /// and generic method expansion. Separate from dispatch registration.
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
}
