using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Immutable index of types from registered assemblies, organized by namespace and full name.
/// Built lazily on first access. Separates assembly scanning from resolution logic.
/// </summary>
internal sealed class TypeAssemblyIndex
{
    private readonly Lazy<(
        FixedDictionary<string, FixedDictionary<string, Type>> NamespaceIndex,
        FixedDictionary<string, Type> FullNameIndex,
        FixedSet<string> NamespacePrefixes,
        FixedDictionary<string, Type>? ImplicitImports)> _indices;

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
        var assemblies1 = assemblies;
        var implicitBclImports1 = implicitBclImports;
        var comparer1 = comparer;
        _indices = new Lazy<(
            FixedDictionary<string, FixedDictionary<string, Type>> NamespaceIndex,
            FixedDictionary<string, Type> FullNameIndex,
            FixedSet<string> NamespacePrefixes,
            FixedDictionary<string, Type>? ImplicitImports)>(
            () => BuildIndices(assemblies1, implicitBclImports1, comparer1),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal bool IsNamespaceOrPrefix(string name) => _indices.Value.NamespacePrefixes.Contains(name);

    internal bool TryResolveInNamespace(string ns, string shortTypeName, [NotNullWhen(true)] out Type? type)
    {
        if (_indices.Value.NamespaceIndex.TryGetValue(ns, out var types) &&
            types.TryGetValue(shortTypeName, out type))
            return true;

        type = default!;
        return false;
    }

    internal bool TryResolveImplicitImport(string typeName, [NotNullWhen(true)] out Type? type)
    {
        var imports = _indices.Value.ImplicitImports;
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
        var namespaceIndex = _indices.Value.NamespaceIndex;
        var fullNameIndex = _indices.Value.FullNameIndex;

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

    private static bool IsReflectionType(Type type)
    {
        var ns = type.Namespace;
        return ns is "System.Reflection" ||
               (ns != null && ns.StartsWith("System.Reflection.", StringComparison.Ordinal));
    }

    private static (
        FixedDictionary<string, FixedDictionary<string, Type>> NamespaceIndex,
        FixedDictionary<string, Type> FullNameIndex,
        FixedSet<string> NamespacePrefixes,
        FixedDictionary<string, Type>? ImplicitImports) BuildIndices(
        ImmutableArray<Assembly> assemblies,
        bool implicitBclImports,
        StringComparer comparer)
    {
        var namespaceIndex = new Dictionary<string, Dictionary<string, Type>>(comparer);
        var fullNameIndex = new Dictionary<string, Type>(comparer);

        foreach (var assembly in assemblies)
        {
            foreach (var type in EnumerateAssemblyTypes(assembly))
            {
                var ns = type.Namespace;
                if (ns != null)
                {
                    if (!namespaceIndex.TryGetValue(ns, out var nsTypes))
                    {
                        nsTypes = new Dictionary<string, Type>(comparer);
                        namespaceIndex[ns] = nsTypes;
                    }

                    // CLR metadata name preserves arity (e.g. List`1) so overloads by arity don't collide.
                    nsTypes.TryAdd(type.Name, type);
                }

                if (type.FullName is { } fullName)
                    fullNameIndex.TryAdd(fullName, type);
            }
        }

        var frozenNamespaceIndex = FixedDictionary<string, FixedDictionary<string, Type>>.Create(
            namespaceIndex,
            kvp => kvp.Key,
            kvp => FixedDictionary<string, Type>.Create(kvp.Value, comparer),
            comparer);
        var frozenFullNameIndex = FixedDictionary<string, Type>.Create(fullNameIndex, comparer);
        var namespacePrefixes = BuildNamespacePrefixes(frozenNamespaceIndex, comparer);
        var implicitImports = implicitBclImports
            ? BuildImplicitImports(frozenNamespaceIndex, comparer)
            : null;

        return (frozenNamespaceIndex, frozenFullNameIndex, namespacePrefixes, implicitImports);
    }

    private static IEnumerable<Type> EnumerateAssemblyTypes(Assembly assembly)
    {
        try
        {
#pragma warning disable IL2026
            return assembly.DefinedTypes.Select(static typeInfo => typeInfo.AsType()).ToArray();
#pragma warning restore IL2026
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

                imports.TryAdd(shortName, type);
            }
        }

        foreach (var ns in DefaultImplicitNamespaces)
        {
            if (!namespaceIndex.TryGetValue(ns, out var types))
                continue;

            foreach (var (shortName, type) in types)
            {
                if (IsReflectionType(type) || !shortName.Contains('`'))
                    continue;

                var friendlyName = shortName[..shortName.IndexOf('`')];
                imports.TryAdd(friendlyName, type);
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
