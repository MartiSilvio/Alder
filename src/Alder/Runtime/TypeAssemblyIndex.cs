using System.Collections.Immutable;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Immutable index of types from registered assemblies, organized by namespace and full name.
/// Built lazily on first access. Separates assembly scanning from resolution logic.
/// </summary>
internal sealed class TypeAssemblyIndex
{
    private readonly ImmutableArray<Assembly> _assemblies;
    private readonly StringComparer _comparer;
    private readonly bool _implicitBclImports;
    private readonly Lazy<FixedDictionary<string, FixedDictionary<string, Type>>> _namespaceIndex;
    private readonly Lazy<FixedDictionary<string, Type>> _fullNameIndex;
    private readonly Lazy<FixedSet<string>> _namespacePrefixes;
    private readonly Lazy<FixedDictionary<string, Type>?> _implicitImports;

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
    ];

    internal static IReadOnlyList<string> GetDefaultImplicitNamespaces() => DefaultImplicitNamespaces;

    internal TypeAssemblyIndex(
        ImmutableArray<Assembly> assemblies,
        bool implicitBclImports,
        StringComparer comparer)
    {
        _assemblies = assemblies;
        _implicitBclImports = implicitBclImports;
        _comparer = comparer;
        _namespaceIndex = new Lazy<FixedDictionary<string, FixedDictionary<string, Type>>>(
            () => BuildNamespaceIndex(_assemblies, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _fullNameIndex = new Lazy<FixedDictionary<string, Type>>(
            () => BuildFullNameIndex(_assemblies, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _namespacePrefixes = new Lazy<FixedSet<string>>(
            () => BuildNamespacePrefixes(_namespaceIndex.Value, _comparer),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _implicitImports = new Lazy<FixedDictionary<string, Type>?>(
            () => _implicitBclImports ? BuildImplicitImports(_namespaceIndex.Value, _comparer) : null,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal bool IsNamespaceOrPrefix(string name) => _namespacePrefixes.Value.Contains(name);

    internal bool TryResolveInNamespace(string ns, string shortTypeName, out Type type)
    {
        if (_namespaceIndex.Value.TryGetValue(ns, out var types) &&
            types.TryGetValue(shortTypeName, out type))
            return true;

        type = default!;
        return false;
    }

    /// <summary>
    /// Fast-path resolution against the default implicit namespaces.
    /// Probes each namespace directly, including CLR arity forms for friendly generic names.
    /// Returns null immediately if implicit BCL imports are disabled.
    /// </summary>
    internal Type? TryResolveImplicitImportFast(string typeName)
    {
        if (!_implicitBclImports)
            return null;

        foreach (var ns in DefaultImplicitNamespaces)
        {
            if (TryResolveInNamespace(ns, typeName, out var resolved) && !IsReflectionType(resolved))
                return resolved;
        }

        if (CanProbeGenericArity(typeName))
        {
            for (var arity = 1; arity <= 8; arity++)
            {
                var genericName = $"{typeName}`{arity}";
                foreach (var ns in DefaultImplicitNamespaces)
                {
                    if (TryResolveInNamespace(ns, genericName, out var resolved) && !IsReflectionType(resolved))
                        return resolved;
                }
            }
        }

        return null;
    }

    internal bool TryResolveImplicitImport(string typeName, out Type type)
    {
        var imports = _implicitImports.Value;
        if (imports != null && imports.TryGetValue(typeName, out type))
            return true;

        type = default!;
        return false;
    }

    /// <summary>
    /// Resolves a fully qualified name while handling the namespace versus nested-type ambiguity.
    /// Progressively splits from the right so namespace-qualified types win before nested-type fallback.
    /// </summary>
    internal Type? TryResolveFullyQualifiedName(string typeName)
    {
        var namespaceIndex = _namespaceIndex.Value;
        var fullNameIndex = _fullNameIndex.Value;

        var lastDot = typeName.LastIndexOf('.');
        while (lastDot > 0)
        {
            var namespacePart = typeName[..lastDot];
            var typeNamePart = typeName[(lastDot + 1)..];

            if (namespaceIndex.TryGetValue(namespacePart, out var types) &&
                types.TryGetValue(typeNamePart, out var found))
            {
                return found;
            }

            lastDot = typeName.LastIndexOf('.', lastDot - 1);
        }

        if (fullNameIndex.TryGetValue(typeName, out var direct))
            return direct;

        var nestedProbe = typeName;
        var dotIndex = nestedProbe.LastIndexOf('.');
        while (dotIndex > 0)
        {
            nestedProbe = nestedProbe[..dotIndex] + "+" + nestedProbe[(dotIndex + 1)..];
            if (fullNameIndex.TryGetValue(nestedProbe, out var nested))
                return nested;

            dotIndex = nestedProbe.LastIndexOf('.');
        }

        return null;
    }

    private static bool CanProbeGenericArity(string typeName)
        => typeName.IndexOfAny(['`', '<', '>', '.', '[', ']']) < 0;

    private static bool IsReflectionType(Type type)
    {
        var ns = type.Namespace;
        return ns is "System.Reflection" ||
               (ns != null && ns.StartsWith("System.Reflection.", StringComparison.Ordinal));
    }

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

                // CLR metadata name preserves arity (e.g. List`1) so overloads by arity don't collide.
                nsTypes.TryAdd(type.Name, type);
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
                if (IsReflectionType(type))
                    continue;

                // Also store under the friendly name without backtick (e.g. "List" in addition to "List`1").
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

    private static FixedSet<string> BuildNamespacePrefixes(
        FixedDictionary<string, FixedDictionary<string, Type>> namespaceIndex,
        StringComparer comparer)
    {
        var prefixes = new HashSet<string>(comparer);
        foreach (var ns in namespaceIndex.Keys)
        {
            prefixes.Add(ns);
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
