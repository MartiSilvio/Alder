using System.Collections.Concurrent;
using System.Collections.Immutable;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Unified type resolution with Roslyn-inspired precedence:
/// 1. Built-in type keywords (int, string, bool, etc.)
/// 2. Implicit BCL imports (List, Dictionary, Task, etc.) when enabled
/// 3. Explicit namespace imports (from RegisterNamespace)
/// 4. Fully qualified name against registered assemblies
/// 5. FAIL with clear error
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
            () => BuildNamespacePrefixes(_namespaceIndex.Value),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _implicitImports = new Lazy<FixedDictionary<string, Type>?>(
            () => _implicitBclImports ? BuildImplicitImports(_namespaceIndex.Value, _comparer) : null,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Resolves a type name. Throws AlderException if the type cannot be found.
    /// Handles generic types (List&lt;int&gt;), nullable suffixes, and fully qualified names.
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
    /// Non-throwing variant. Returns null if the type cannot be found.
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
        {
            try { return ResolveGenericType(typeName); }
            catch (Exception ex) when (ex is AlderException or ArgumentException or TypeLoadException or InvalidOperationException) { return null; }
        }

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
        // Step 1: Built-in type keywords
        if (_builtInTypes.TryGetValue(typeName, out var builtIn))
            return builtIn;

        // Step 2: Implicit BCL imports
        if (_implicitBclImports)
        {
            var fastImplicit = TryResolveImplicitImportFast(typeName);
            if (fastImplicit != null)
                return fastImplicit;
        }

        var implicitImports = _implicitImports.Value;
        if (implicitImports != null && implicitImports.TryGetValue(typeName, out var implicitType))
            return implicitType;

        // Step 3: Explicit namespace imports (check for ambiguity)
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

        // Step 4: Fully qualified name (contains dots)
        if (typeName.Contains('.'))
            return ResolveFullyQualifiedName(typeName);

        // Step 5: Not found
        return null;
    }

    /// <summary>
    /// Namespace vs nested type disambiguation per Roslyn approach:
    /// First try namespace resolution, then try Assembly.GetType for nested types.
    /// </summary>
    private Type? ResolveFullyQualifiedName(string typeName)
    {
        // Try namespace resolution: progressively split from the right
        // For "System.Collections.Generic.List":
        //   Try namespace="System.Collections.Generic" type="List"
        //   Try namespace="System.Collections" type="Generic.List"
        //   Try namespace="System" type="Collections.Generic.List"
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

        // Preserve support for generic type names without explicit arity
        // (e.g., "List" -> "List`1") in implicit namespaces.
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
    /// Resolves a generic type name like "List&lt;int&gt;" or
    /// "System.Collections.Generic.Dictionary&lt;string, int&gt;".
    /// </summary>
    private Type ResolveGenericType(string typeName)
    {
        var ltIndex = typeName.IndexOf('<');
        var baseName = typeName[..ltIndex];
        var argsString = typeName[(ltIndex + 1)..^1]; // strip < and >

        // Parse type arguments respecting nested generics
        var typeArgNames = SplitGenericArgs(argsString);
        var arity = typeArgNames.Count;

        // Resolve the open generic type using CLR backtick notation
        var openGenericName = baseName + "`" + arity;
        var openType = ResolveType(openGenericName);

        // Resolve each type argument recursively
        var typeArgs = new Type[arity];
        for (var i = 0; i < arity; i++)
            typeArgs[i] = ResolveType(typeArgNames[i].Trim());

        return RuntimeGenericFactory.CloseGenericType(openType, typeArgs);
    }

    /// <summary>
    /// Splits generic type arguments at top-level commas, respecting nested angle brackets.
    /// e.g. "string, List&lt;int&gt;" -> ["string", "List&lt;int&gt;"]
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

    /// <summary>
    /// Implicit import namespaces for common BCL types when ImplicitBclImports is enabled.
    /// ECMA-334 compatible: System, System.Collections.Generic, System.Threading.Tasks, System.Linq.
    /// System.Reflection is EXCLUDED for security.
    /// </summary>
    private static readonly string[] DefaultImplicitNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
        "System.Linq",
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

    internal static bool TryResolveKeywordType(string keyword, out Type type)
    {
        return BuiltInTypeKeywordsOrdinal.TryGetValue(keyword, out type);
    }

    /// <summary>
    /// Creates a TypeResolver from the given configuration.
    /// Builds the namespace index from registered assemblies at freeze time.
    /// </summary>
    internal static TypeResolver Create(
        ImmutableArray<Assembly> assemblies,
        ImmutableArray<string> importedNamespaces,
        bool implicitBclImports,
        StringComparer comparer)
    {
        // Deduplicate assemblies
        var assemblySet = new HashSet<Assembly>(assemblies)
        {
            // Always include default assemblies
            typeof(object).Assembly, // System.Private.CoreLib
            typeof(List<>).Assembly, // System.Collections
            typeof(Task).Assembly, // System.Threading.Tasks (may be same as CoreLib)
            typeof(Enumerable).Assembly, // System.Linq (required for FQN and extension method resolution)
            typeof(System.Text.RegularExpressions.Regex).Assembly // System.Text.RegularExpressions
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
    /// Builds a namespace -> (short name -> Type) index from all registered assemblies.
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

                // Use type's short name (without namespace). For generic types,
                // store with backtick notation (e.g., "List`1")
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
    /// Builds the implicit import map from default implicit namespaces.
    /// Maps short type names to their Type for types in System,
    /// System.Collections.Generic, and System.Threading.Tasks.
    /// System.Reflection types are EXCLUDED for security.
    /// For generic types, stores the open generic type under the name without backtick
    /// (e.g., "List" -> typeof(List&lt;&gt;)).
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
        FixedDictionary<string, FixedDictionary<string, Type>> namespaceIndex)
    {
        var prefixes = new HashSet<string>(StringComparer.Ordinal);
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
        return FixedSet<string>.Create(prefixes, StringComparer.Ordinal);
    }
}
