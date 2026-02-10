using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace CsEval.Runtime;

/// <summary>
/// Unified type resolution with Roslyn-inspired precedence:
/// 1. Built-in type keywords (int, string, bool, etc.)
/// 2. Implicit BCL imports (List, Dictionary, Task, etc.) when enabled
/// 3. Explicit namespace imports (from AddUsing)
/// 4. Fully qualified name against registered assemblies
/// 5. FAIL with clear error
/// </summary>
internal sealed class TypeResolver
{
    private readonly FrozenDictionary<string, Type> _builtInTypes;
    private readonly FrozenDictionary<string, Type>? _implicitImports;
    private readonly ImmutableArray<string> _importedNamespaces;
    private readonly FrozenDictionary<string, FrozenDictionary<string, Type>> _namespaceIndex;
    private readonly ImmutableArray<Assembly> _registeredAssemblies;
    private readonly ConcurrentDictionary<string, Type?> _cache = new();

    private TypeResolver(
        FrozenDictionary<string, Type> builtInTypes,
        FrozenDictionary<string, Type>? implicitImports,
        ImmutableArray<string> importedNamespaces,
        FrozenDictionary<string, FrozenDictionary<string, Type>> namespaceIndex,
        ImmutableArray<Assembly> registeredAssemblies)
    {
        _builtInTypes = builtInTypes;
        _implicitImports = implicitImports;
        _importedNamespaces = importedNamespaces;
        _namespaceIndex = namespaceIndex;
        _registeredAssemblies = registeredAssemblies;
    }

    /// <summary>
    /// Resolves a type name. Throws CsEvalException if the type cannot be found.
    /// Handles generic types (List&lt;int&gt;), nullable suffixes, and fully qualified names.
    /// </summary>
    public Type ResolveType(string typeName)
    {
        if (typeName.Contains('<'))
            return ResolveGenericType(typeName);

        return _cache.GetOrAdd(typeName, ResolveTypeCore)
            ?? throw new CsEvalException(
                $"Unknown type '{typeName}'. Ensure the type's assembly is registered with AddAssembly() " +
                "and its namespace is imported with AddUsing(), or use its fully qualified name.");
    }

    /// <summary>
    /// Non-throwing variant. Returns null if the type cannot be found.
    /// </summary>
    public Type? TryResolveType(string typeName)
    {
        if (typeName.Contains('<'))
        {
            try { return ResolveGenericType(typeName); }
            catch { return null; }
        }

        return _cache.GetOrAdd(typeName, ResolveTypeCore);
    }

    private Type? ResolveTypeCore(string typeName)
    {
        // Step 1: Built-in type keywords
        if (_builtInTypes.TryGetValue(typeName, out var builtIn))
            return builtIn;

        // Step 2: Implicit BCL imports
        if (_implicitImports != null && _implicitImports.TryGetValue(typeName, out var implicitType))
            return implicitType;

        // Step 3: Explicit namespace imports (check for ambiguity)
        Type? importedMatch = null;
        string? matchedNamespace = null;
        List<(string Namespace, Type Type)>? ambiguousMatches = null;

        foreach (var ns in _importedNamespaces)
        {
            if (_namespaceIndex.TryGetValue(ns, out var types) &&
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
            var candidates = string.Join("\n", ambiguousMatches.Select(m => $"  - {m.Namespace}.{typeName}"));
            throw new CsEvalException(
                $"Ambiguous type reference: '{typeName}' could refer to:\n{candidates}\n" +
                "Use a fully qualified name to disambiguate.");
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

            if (_namespaceIndex.TryGetValue(namespacePart, out var types) &&
                types.TryGetValue(typeNamePart, out var found))
            {
                return found;
            }

            lastDot = typeName.LastIndexOf('.', lastDot - 1);
        }

        // Try Assembly.GetType on each registered assembly (handles nested types
        // like OuterClass.InnerClass where CLR uses + notation internally)
        foreach (var assembly in _registeredAssemblies)
        {
            var resolved = assembly.GetType(typeName);
            if (resolved != null)
                return resolved;
        }

        return null;
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

        return openType.MakeGenericType(typeArgs);
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

    /// <summary>
    /// Static bridge for IL compiler emission. Resolves type name via resolver instance.
    /// </summary>
    internal static Type ResolveTypeStatic(TypeResolver resolver, string typeName)
        => resolver.ResolveType(typeName);

    /// <summary>
    /// Static bridge for IL compiler emission. Non-throwing variant.
    /// </summary>
    internal static Type? TryResolveTypeStatic(TypeResolver resolver, string typeName)
        => resolver.TryResolveType(typeName);

    /// <summary>
    /// Implicit import namespaces for common BCL types when ImplicitBclImports is enabled.
    /// ECMA-334 compatible: System, System.Collections.Generic, System.Threading.Tasks.
    /// System.Linq is NOT included (extended BCL requires explicit registration).
    /// System.Reflection is EXCLUDED for security.
    /// </summary>
    private static readonly string[] DefaultImplicitNamespaces =
    [
        "System",
        "System.Collections.Generic",
        "System.Threading.Tasks",
    ];

    /// <summary>
    /// Built-in C# type keyword map per ECMA-334 §8.3.5.
    /// </summary>
    private static readonly Dictionary<string, Type> BuiltInTypeKeywords = new()
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
    };

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
        var assemblySet = new HashSet<Assembly>(assemblies);

        // Always include default assemblies
        assemblySet.Add(typeof(object).Assembly);        // System.Private.CoreLib
        assemblySet.Add(typeof(List<>).Assembly);         // System.Collections
        assemblySet.Add(typeof(Task).Assembly);           // System.Threading.Tasks (may be same as CoreLib)

        var allAssemblies = assemblySet.ToImmutableArray();

        // Build namespace index from all assemblies
        var namespaceIndex = BuildNamespaceIndex(allAssemblies, comparer);

        // Build built-in type keyword map
        var builtInTypes = BuiltInTypeKeywords.ToFrozenDictionary(comparer);

        // Build implicit import map if enabled
        FrozenDictionary<string, Type>? implicitImports = null;
        if (implicitBclImports)
        {
            implicitImports = BuildImplicitImports(namespaceIndex, comparer);
        }

        return new TypeResolver(builtInTypes, implicitImports, importedNamespaces, namespaceIndex, allAssemblies);
    }

    /// <summary>
    /// Builds a namespace -> (short name -> Type) index from all registered assemblies.
    /// </summary>
    private static FrozenDictionary<string, FrozenDictionary<string, Type>> BuildNamespaceIndex(
        ImmutableArray<Assembly> assemblies,
        StringComparer comparer)
    {
        var index = new Dictionary<string, Dictionary<string, Type>>(comparer);

        foreach (var assembly in assemblies)
        {
            Type[] exportedTypes;
            try
            {
                exportedTypes = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some assemblies have unloadable types; use what we can
                exportedTypes = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in exportedTypes)
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

        return index.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToFrozenDictionary(comparer),
            comparer);
    }

    /// <summary>
    /// Builds the implicit import map from default implicit namespaces.
    /// Maps short type names to their Type for types in System,
    /// System.Collections.Generic, and System.Threading.Tasks.
    /// System.Reflection types are EXCLUDED for security.
    /// For generic types, stores the open generic type under the name without backtick
    /// (e.g., "List" -> typeof(List&lt;&gt;)).
    /// </summary>
    private static FrozenDictionary<string, Type> BuildImplicitImports(
        FrozenDictionary<string, FrozenDictionary<string, Type>> namespaceIndex,
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

        return imports.ToFrozenDictionary(comparer);
    }
}
