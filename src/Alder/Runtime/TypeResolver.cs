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
    private readonly ImmutableArray<Assembly> _registeredAssemblies;
    private readonly StringComparer _comparer;
    private readonly bool _implicitBclImports;
    private readonly Lazy<FixedDictionary<string, FixedDictionary<string, Type>>> _namespaceIndex;
    private readonly Lazy<FixedDictionary<string, Type>> _fullNameIndex;
    private readonly Lazy<FixedSet<string>> _namespacePrefixes;
    private readonly Lazy<FixedDictionary<string, Type>?> _implicitImports;
    private readonly ConcurrentDictionary<string, Type?> _cache = new();

    private static readonly string[] DefaultImplicitNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Text.Json",
        "System.Numerics",
        "System.Globalization",
        // Security-sensitive namespaces remain available through fully qualified names,
        // but they are not made ambient through implicit imports.
    ];

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
        ImmutableArray<Assembly> registeredAssemblies,
        bool implicitBclImports,
        StringComparer comparer)
    {
        _builtInTypes = builtInTypes;
        _importedNamespaces = importedNamespaces;
        _registeredAssemblies = registeredAssemblies;
        _implicitBclImports = implicitBclImports;
        _comparer = comparer;
        _namespaceIndex = new Lazy<FixedDictionary<string, FixedDictionary<string, Type>>>(
            () => BuildNamespaceIndex(_registeredAssemblies, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _fullNameIndex = new Lazy<FixedDictionary<string, Type>>(
            () => BuildFullNameIndex(_registeredAssemblies, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _namespacePrefixes = new Lazy<FixedSet<string>>(
            () => BuildNamespacePrefixes(_namespaceIndex.Value, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _implicitImports = new Lazy<FixedDictionary<string, Type>?>(
            () => _implicitBclImports ? BuildImplicitImports(_namespaceIndex.Value, _comparer) : null,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Resolves a type name and throws if resolution fails.
    /// Generic forms, nullable suffixes, and array suffixes are handled here before the cached core lookup runs.
    /// </summary>
    public Type ResolveType(string typeName)
    {
        if (TryParseArraySuffix(typeName, out var elementTypeName, out var rank))
        {
            var elementType = ResolveType(elementTypeName);
            return RuntimeArrayFactory.GetArrayType(elementType, rank);
        }

        if (typeName.Contains('<'))
            return ResolveGenericType(typeName);

        return _cache.GetOrAdd(typeName, ResolveTypeCore)
            ?? throw new AlderException(DiagnosticDescriptors.TypeNotFound, typeName);
    }

    /// <summary>
    /// Attempts to resolve a type name and returns <c>null</c> when resolution fails.
    /// </summary>
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

        return _cache.GetOrAdd(typeName, ResolveTypeCore);
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

    internal bool IsNamespaceOrPrefix(string name) => _namespacePrefixes.Value.Contains(name);

    private Type? ResolveTypeCore(string typeName)
    {
        // Built-in keyword aliases take precedence over all imported namespaces.
        if (_builtInTypes.TryGetValue(typeName, out var builtIn))
            return builtIn;

        // Implicit imports emulate the ambient BCL surface Alder exposes by default.
        if (_implicitBclImports)
        {
            var fastImplicit = TryResolveImplicitImportFast(typeName);
            if (fastImplicit != null)
                return fastImplicit;
        }

        var implicitImports = _implicitImports.Value;
        if (implicitImports != null && implicitImports.TryGetValue(typeName, out var implicitType))
            return implicitType;

        // Explicit imports are searched next and preserve ambiguity reporting.
        Type? importedMatch = null;
        string? matchedNamespace = null;
        List<(string Namespace, Type Type)>? ambiguousMatches = null;

        foreach (var ns in _importedNamespaces)
        {
            if (_namespaceIndex.Value.TryGetValue(ns, out var types) &&
                types.TryGetValue(typeName, out var found))
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

        // Fully qualified names are the last lookup stage before resolution fails.
        if (typeName.Contains('.'))
            return ResolveFullyQualifiedName(typeName);

        return null;
    }

    /// <summary>
    /// Resolves a fully qualified name while handling the namespace versus nested-type ambiguity.
    /// </summary>
    private Type? ResolveFullyQualifiedName(string typeName)
    {
        // Progressively split from the right so namespace-qualified types win before nested-type fallback runs.
        var lastDot = typeName.LastIndexOf('.');
        while (lastDot > 0)
        {
            var namespacePart = typeName[..lastDot];
            var typeNamePart = typeName[(lastDot + 1)..];

            if (_namespaceIndex.Value.TryGetValue(namespacePart, out var types) &&
                types.TryGetValue(typeNamePart, out var found))
            {
                return found;
            }

            lastDot = typeName.LastIndexOf('.', lastDot - 1);
        }

        if (_fullNameIndex.Value.TryGetValue(typeName, out var direct))
            return direct;

        var nestedProbe = typeName;
        var dotIndex = nestedProbe.LastIndexOf('.');
        while (dotIndex > 0)
        {
            nestedProbe = nestedProbe[..dotIndex] + "+" + nestedProbe[(dotIndex + 1)..];
            if (_fullNameIndex.Value.TryGetValue(nestedProbe, out var nested))
                return nested;

            dotIndex = nestedProbe.LastIndexOf('.');
        }

        return null;
    }

    private Type? TryResolveImplicitImportFast(string typeName)
    {
        foreach (var ns in DefaultImplicitNamespaces)
        {
            var resolved = TryResolveTypeInNamespace(ns, typeName);
            if (resolved != null && !IsReflectionType(resolved))
                return resolved;
        }

        // Support friendly generic names such as List by probing the CLR arity form List`1 within the implicit namespaces.
        if (CanProbeGenericArity(typeName))
        {
            for (var arity = 1; arity <= 8; arity++)
            {
                var genericName = $"{typeName}`{arity}";
                foreach (var ns in DefaultImplicitNamespaces)
                {
                    var resolved = TryResolveTypeInNamespace(ns, genericName);
                    if (resolved != null && !IsReflectionType(resolved))
                        return resolved;
                }
            }
        }

        return null;
    }

    private static bool CanProbeGenericArity(string typeName)
        => typeName.IndexOfAny(['`', '<', '>', '.', '[', ']']) < 0;

    private Type? TryResolveTypeInNamespace(string ns, string shortTypeName)
    {
        return _namespaceIndex.Value.TryGetValue(ns, out var types) &&
               types.TryGetValue(shortTypeName, out var resolved)
            ? resolved
            : null;
    }

    private static bool IsReflectionType(Type type)
    {
        var ns = type.Namespace;
        return ns is "System.Reflection" ||
               (ns != null && ns.StartsWith("System.Reflection.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Resolves a generic type name such as <c>List&lt;int&gt;</c> or <c>System.Collections.Generic.Dictionary&lt;string, int&gt;</c>.
    /// </summary>
    private Type ResolveGenericType(string typeName)
    {
        var ltIndex = typeName.IndexOf('<');
        var baseName = typeName[..ltIndex];
        var argsString = typeName[(ltIndex + 1)..^1]; // strip < and >

        // Parse type arguments while respecting nested generic argument lists.
        var typeArgNames = SplitGenericArgs(argsString);
        var arity = typeArgNames.Count;

        // Resolve the open generic type through its CLR metadata name.
        var openGenericName = baseName + "`" + arity;
        var openType = ResolveType(openGenericName);

        // Resolve each type argument recursively before closing the generic type.
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

    /// <summary>
    /// Splits generic type arguments at top-level commas while respecting nested angle brackets.
    /// </summary>
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

    /// <summary>
    /// Creates a <see cref="TypeResolver"/> for the supplied configuration data.
    /// The resolver augments the explicit assembly set with the core platform assemblies Alder depends on for ambient resolution.
    /// </summary>
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
        return new TypeResolver(builtInTypes, importedNamespaces, allAssemblies, implicitBclImports, comparer);
    }

    private static FixedDictionary<string, Type> GetBuiltInTypeMap(StringComparer comparer)
    {
        if (ReferenceEquals(comparer, StringComparer.Ordinal))
            return BuiltInTypeKeywordsOrdinal;

        if (ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
            return BuiltInTypeKeywordsOrdinalIgnoreCase;

        return FixedDictionary<string, Type>.Create(BuiltInTypeKeywords, comparer);
    }

    /// <summary>
    /// Builds a namespace-to-type index from the registered assemblies.
    /// </summary>
    private static FixedDictionary<string, FixedDictionary<string, Type>> BuildNamespaceIndex(
        ImmutableArray<Assembly> assemblies,
        StringComparer comparer)
    {
        var index = new Dictionary<string, Dictionary<string, Type>>(comparer);

        foreach (var assembly in assemblies)
        {
            foreach (var type in EnumerateAssemblyTypes(assembly))
            {
                var ns = type.Namespace;
                if (ns == null) continue;

                if (!index.TryGetValue(ns, out var nsTypes))
                {
                    nsTypes = new Dictionary<string, Type>(comparer);
                    index[ns] = nsTypes;
                }

                // Generic types remain keyed by their CLR metadata name so arity stays explicit at the index level.
                var shortName = type.Name;
                nsTypes.TryAdd(shortName, type);
            }
        }

        return FixedDictionary<string, FixedDictionary<string, Type>>.Create(
            index,
            kvp => kvp.Key,
            kvp => FixedDictionary<string, Type>.Create(kvp.Value, comparer),
            comparer);
    }

    private static FixedDictionary<string, Type> BuildFullNameIndex(
        ImmutableArray<Assembly> assemblies,
        StringComparer comparer)
    {
        var index = new Dictionary<string, Type>(comparer);
        foreach (var assembly in assemblies)
        {
            foreach (var type in EnumerateAssemblyTypes(assembly))
            {
                if (type.FullName is { } fullName)
                    index.TryAdd(fullName, type);
            }
        }

        return FixedDictionary<string, Type>.Create(index, comparer);
    }

    private static IEnumerable<Type> EnumerateAssemblyTypes(Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes.Select(static typeInfo => typeInfo.AsType()).ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static type => type != null).Cast<Type>().ToArray();
        }
    }

    /// <summary>
    /// Builds the implicit-import map from Alder's default ambient namespaces.
    /// Reflection types stay excluded, and open generic types are also recorded under their friendly name without the CLR backtick suffix.
    /// </summary>
    private static FixedDictionary<string, Type> BuildImplicitImports(
        FixedDictionary<string, FixedDictionary<string, Type>> namespaceIndex,
        StringComparer comparer)
    {
        var imports = new Dictionary<string, Type>(comparer);

        foreach (var ns in DefaultImplicitNamespaces)
        {
            if (!namespaceIndex.TryGetValue(ns, out var types))
                continue;

            foreach (var (shortName, type) in types)
            {
                // CRITICAL: Exclude System.Reflection types
                if (type.Namespace is "System.Reflection" ||
                    (type.Namespace != null && type.Namespace.StartsWith("System.Reflection.", StringComparison.Ordinal)))
                    continue;

                // For generic types, also store under the friendly name without backtick
                // e.g., "List`1" is stored as "List`1" AND "List"
                if (shortName.Contains('`'))
                {
                    var friendlyName = shortName[..shortName.IndexOf('`')];
                    imports.TryAdd(friendlyName, type);
                }

                imports.TryAdd(shortName, type);
            }
        }

        return FixedDictionary<string, Type>.Create(imports, comparer);
    }

    /// <summary>
    /// Builds a set of all namespace strings and all their prefixes.
    /// For "System.Collections.Generic", adds: "System", "System.Collections", "System.Collections.Generic".
    /// Enables O(1) lookup for IsNamespaceOrPrefix.
    /// </summary>
    private static FixedSet<string> BuildNamespacePrefixes(
        FixedDictionary<string, FixedDictionary<string, Type>> namespaceIndex,
        StringComparer comparer)
    {
        var prefixes = new HashSet<string>(comparer);
        foreach (var ns in namespaceIndex.Keys)
        {
            prefixes.Add(ns);
            // Add all prefixes: "System.Collections.Generic" -> "System", "System.Collections"
            var dotIndex = ns.IndexOf('.');
            while (dotIndex > 0)
            {
                prefixes.Add(ns[..dotIndex]);
                dotIndex = ns.IndexOf('.', dotIndex + 1);
            }
        }
        return FixedSet<string>.Create(prefixes, comparer);
    }
}
