using System.Collections.Frozen;
using System.Collections.Immutable;

namespace CsEval.Runtime;

/// <summary>
/// Immutable configuration for CsEval evaluation contexts.
/// Once created, this configuration is thread-safe and can be shared across multiple threads.
/// Uses FrozenDictionary for optimal read performance on immutable data.
/// </summary>
public sealed class CsEvalConfig
{
    public FrozenDictionary<string, Func<object?[], object?>> Functions { get; }
    internal FrozenDictionary<string, ModuleInfo> Modules { get; }
    internal ImmutableArray<Type> ExtensionTypes { get; }
    internal TypeCache TypeCache { get; }
    public StringComparer Comparer { get; }

    private CsEvalConfig(
        FrozenDictionary<string, Func<object?[], object?>> functions,
        FrozenDictionary<string, ModuleInfo> modules,
        ImmutableArray<Type> extensionTypes,
        TypeCache typeCache,
        StringComparer comparer)
    {
        Functions = functions;
        Modules = modules;
        ExtensionTypes = extensionTypes;
        TypeCache = typeCache;
        Comparer = comparer;
    }

    internal static CsEvalConfig Create(
        Dictionary<string, Func<object?[], object?>> functions,
        Dictionary<string, ModuleInfo> modules,
        List<Type> extensionTypes,
        TypeCache typeCache,
        StringComparer comparer)
    {
        return new CsEvalConfig(
            functions.ToFrozenDictionary(comparer),
            modules.ToFrozenDictionary(comparer),
            [..extensionTypes],
            typeCache,
            comparer);
    }

    internal static readonly CsEvalConfig Empty = new(
        FrozenDictionary<string, Func<object?[], object?>>.Empty,
        FrozenDictionary<string, ModuleInfo>.Empty,
        [],
        new TypeCache(),
        StringComparer.Ordinal);
}

internal sealed class ModuleInfo
{
    public Type Type { get; }
    public object? Instance { get; }
    public IReadOnlyDictionary<string, MemberInfo> Members { get; }

    public ModuleInfo(Type type, object? instance, IReadOnlyDictionary<string, MemberInfo> members)
    {
        Type = type;
        Instance = instance;
        Members = members;
    }

    public object Resolve(IServiceProvider? serviceProvider)
    {
        if (Instance != null)
            return Instance;

        if (serviceProvider != null)
        {
            var resolved = serviceProvider.GetService(Type);
            if (resolved != null)
                return resolved;
        }

        if (Type.GetConstructor(Type.EmptyTypes) != null)
        {
            return Activator.CreateInstance(Type)
                   ?? throw new InvalidOperationException($"Cannot create instance of '{Type.FullName}'");
        }

        throw new InvalidOperationException(
            $"Cannot resolve instance of '{Type.FullName}'. " +
            $"Either register it in IServiceProvider or ensure it has a parameterless constructor.");
    }
}
