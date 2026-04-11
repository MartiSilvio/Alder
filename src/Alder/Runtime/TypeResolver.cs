using System.Collections.Concurrent;
using System.Collections.Immutable;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Resolves type names using a precedence order aligned with ordinary C# expectations.
/// Resolution checks built-in keywords first, then implicit imports, then explicit imports,
/// and finally fully qualified names rooted in the registered assemblies.
/// </summary>
internal sealed class TypeResolver
{
    private readonly FixedDictionary<string, Type> _builtInTypes;
    private readonly ImmutableArray<string> _importedNamespaces;
    private readonly TypeAssemblyIndex _index;
    private readonly ConcurrentDictionary<string, Type?> _cache = new();
    private readonly ConcurrentQueue<string> _cacheInsertionOrder = new();
    private const int CacheCapacity = 4096;

    /// <summary>
    /// Built-in C# type keyword map per ECMA-334 §8.3.5.
    /// </summary>
    private static readonly FixedDictionary<string, Type> BuiltInTypeKeywords = FixedDictionary<string, Type>.Create(new Dictionary<string, Type>
    {
        ["sbyte"] = typeof(sbyte),
        ["byte"] = typeof(byte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["bool"] = typeof(bool),
        ["char"] = typeof(char),
        ["string"] = typeof(string),
        ["object"] = typeof(object),
        ["dynamic"] = typeof(object),
        ["sbyte?"] = typeof(sbyte?),
        ["byte?"] = typeof(byte?),
        ["short?"] = typeof(short?),
        ["ushort?"] = typeof(ushort?),
        ["int?"] = typeof(int?),
        ["uint?"] = typeof(uint?),
        ["long?"] = typeof(long?),
        ["ulong?"] = typeof(ulong?),
        ["float?"] = typeof(float?),
        ["double?"] = typeof(double?),
        ["decimal?"] = typeof(decimal?),
        ["bool?"] = typeof(bool?),
        ["char?"] = typeof(char?),
        ["string?"] = typeof(string),
        ["object?"] = typeof(object),
        ["void"] = typeof(void),
    });

    private static readonly FixedDictionary<string, Type> BuiltInTypeKeywordsOrdinal =
        FixedDictionary<string, Type>.Create(BuiltInTypeKeywords, kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

    private static readonly FixedDictionary<string, Type> BuiltInTypeKeywordsOrdinalIgnoreCase =
        FixedDictionary<string, Type>.Create(BuiltInTypeKeywords, kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

    private TypeResolver(
        FixedDictionary<string, Type> builtInTypes,
        ImmutableArray<string> importedNamespaces,
        TypeAssemblyIndex index)
    {
        _builtInTypes = builtInTypes;
        _importedNamespaces = importedNamespaces;
        _index = index;
    }

    public Type ResolveType(string typeName)
    {
        if (TryParseArraySuffix(typeName, out var elementTypeName, out var rank))
        {
            var elementType = ResolveType(elementTypeName);
            return RuntimeArrayFactory.GetArrayType(elementType, rank);
        }

        if (typeName.Contains('<'))
            return ResolveGenericType(typeName);

        return CacheGetOrAdd(typeName, ResolveTypeCore)
            ?? throw new AlderException(DiagnosticDescriptors.TypeNotFound, typeName);
    }

    public Type? TryResolveType(string typeName)
    {
        if (TryParseArraySuffix(typeName, out var elementTypeName, out var rank))
        {
            var elementType = TryResolveType(elementTypeName);
            if (elementType == null)
                return null;

            return RuntimeArrayFactory.GetArrayType(elementType, rank);
        }

        if (typeName.Contains('<'))
            return TryResolveGenericType(typeName);

        return CacheGetOrAdd(typeName, ResolveTypeCore);
    }

    private Type? CacheGetOrAdd(string key, Func<string, Type?> valueFactory)
    {
        if (_cache.TryGetValue(key, out var existing))
            return existing;

        var value = _cache.GetOrAdd(key, valueFactory);

        _cacheInsertionOrder.Enqueue(key);

        while (_cache.Count > CacheCapacity && _cacheInsertionOrder.TryDequeue(out var oldest))
            _cache.TryRemove(oldest, out _);

        return value;
    }

    private static bool TryParseArraySuffix(string typeName, out string elementTypeName, out int rank)
    {
        elementTypeName = string.Empty;
        rank = 0;

        if (typeName.Length == 0 || typeName[typeName.Length - 1] != ']')
            return false;

        var openIndex = typeName.LastIndexOf('[');
        if (openIndex < 0 || openIndex > typeName.Length - 2)
            return false;

        var rankPart = typeName.AsSpan((openIndex + 1), typeName.Length - openIndex - 2);
        foreach (var c in rankPart)
        {
            if (c != ',')
                return false;
        }

        elementTypeName = typeName[..openIndex];
        rank = rankPart.Length + 1;
        return true;
    }

    internal bool IsNamespaceOrPrefix(string name) => _index.IsNamespaceOrPrefix(name);

    private Type? ResolveTypeCore(string typeName)
    {
        if (_builtInTypes.TryGetValue(typeName, out var builtIn))
            return builtIn;

        var fastImplicit = _index.TryResolveImplicitImportFast(typeName);
        if (fastImplicit != null)
            return fastImplicit;

        if (_index.TryResolveImplicitImport(typeName, out var implicitType))
            return implicitType;

        // Explicit imports preserve ambiguity reporting per §7.3.
        Type? importedMatch = null;
        string? matchedNamespace = null;
        List<(string Namespace, Type Type)>? ambiguousMatches = null;

        foreach (var ns in _importedNamespaces)
        {
            if (_index.TryResolveInNamespace(ns, typeName, out var found))
            {
                if (importedMatch == null)
                {
                    importedMatch = found;
                    matchedNamespace = ns;
                }
                else
                {
                    ambiguousMatches ??= [(matchedNamespace!, importedMatch)];
                    ambiguousMatches.Add((ns, found));
                }
            }
        }

        if (ambiguousMatches != null)
        {
            throw new AlderException(DiagnosticDescriptors.AmbiguousReference,
                typeName,
                $"{ambiguousMatches[0].Namespace}.{typeName}",
                $"{ambiguousMatches[1].Namespace}.{typeName}");
        }

        if (importedMatch != null)
            return importedMatch;

        if (typeName.Contains('.'))
            return _index.TryResolveFullyQualifiedName(typeName);

        return null;
    }

    private Type ResolveGenericType(string typeName)
    {
        var ltIndex = typeName.IndexOf('<');
        var baseName = typeName[..ltIndex];
        var argsString = typeName[(ltIndex + 1)..^1];

        var typeArgNames = SplitGenericArgs(argsString);
        var arity = typeArgNames.Count;

        var openGenericName = baseName + "`" + arity;
        var openType = ResolveType(openGenericName);

        var typeArgs = new Type[arity];
        for (var i = 0; i < arity; i++)
            typeArgs[i] = ResolveType(typeArgNames[i].Trim());

        return RuntimeGenericFactory.CloseGenericType(openType, typeArgs);
    }

    private Type? TryResolveGenericType(string typeName)
    {
        var ltIndex = typeName.IndexOf('<');
        var baseName = typeName[..ltIndex];
        var argsString = typeName[(ltIndex + 1)..^1];
        var typeArgNames = SplitGenericArgs(argsString);
        var arity = typeArgNames.Count;

        var openGenericName = baseName + "`" + arity;
        var openType = TryResolveType(openGenericName);
        if (openType == null)
            return null;

        var typeArgs = new Type[arity];
        for (var i = 0; i < arity; i++)
        {
            var arg = TryResolveType(typeArgNames[i].Trim());
            if (arg == null)
                return null;
            typeArgs[i] = arg;
        }

        return RuntimeGenericFactory.TryCloseGenericType(openType, typeArgs, out var closed) ? closed : null;
    }

    private static List<string> SplitGenericArgs(string argsString)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < argsString.Length; i++)
        {
            switch (argsString[i])
            {
                case '<': depth++; break;
                case '>': depth--; break;
                case ',' when depth == 0:
                    result.Add(argsString[start..i]);
                    start = i + 1;
                    break;
            }
        }

        result.Add(argsString[start..]);
        return result;
    }

    internal static Type ResolveTypeStatic(TypeResolver resolver, string typeName)
        => resolver.ResolveType(typeName);

    internal static Type? TryResolveTypeStatic(TypeResolver resolver, string typeName)
        => resolver.TryResolveType(typeName);

    internal static bool TryResolveKeywordType(string keyword, out Type type)
    {
        return BuiltInTypeKeywordsOrdinal.TryGetValue(keyword, out type);
    }

    internal static TypeResolver Create(
        ImmutableArray<Assembly> assemblies,
        ImmutableArray<string> importedNamespaces,
        bool implicitBclImports,
        StringComparer comparer)
    {
        var assemblySet = new HashSet<Assembly>(assemblies)
        {
            typeof(object).Assembly,                                    // System.Private.CoreLib
            typeof(Enumerable).Assembly,                                // System.Linq
            typeof(System.Text.RegularExpressions.Regex).Assembly,     // System.Text.RegularExpressions
            typeof(Stack<>).Assembly,                                   // System.Collections
            typeof(Uri).Assembly,                                       // System.Private.Uri
            typeof(System.Numerics.BigInteger).Assembly,               // System.Runtime.Numerics (BigInteger, Complex)
            typeof(System.Security.Cryptography.SHA256).Assembly,      // System.Security.Cryptography
        };

        var allAssemblies = assemblySet.ToImmutableArray();
        var builtInTypes = GetBuiltInTypeMap(comparer);
        var index = new TypeAssemblyIndex(allAssemblies, implicitBclImports, comparer);
        return new TypeResolver(builtInTypes, importedNamespaces, index);
    }

    private static FixedDictionary<string, Type> GetBuiltInTypeMap(StringComparer comparer)
    {
        if (ReferenceEquals(comparer, StringComparer.Ordinal))
            return BuiltInTypeKeywordsOrdinal;

        if (ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
            return BuiltInTypeKeywordsOrdinalIgnoreCase;

        return FixedDictionary<string, Type>.Create(BuiltInTypeKeywords, comparer);
    }
}
