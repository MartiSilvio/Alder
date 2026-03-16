using CsEval.Runtime.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CsEval.Runtime;

/// <summary>
/// Immutable configuration for CsEval evaluation contexts.
/// Once created, this configuration is thread-safe and can be shared across multiple threads.
/// Uses FixedDictionary for optimal read performance on immutable data.
/// </summary>
internal sealed class CsEvalConfig
{
    public FixedDictionary<string, Func<object?[], object?>> Functions { get; }
    internal FixedDictionary<string, ModuleInfo> Modules { get; }
    internal ImmutableArray<Type> ExtensionTypes { get; }
    internal TypeMetadataProvider TypeMetadata { get; }
    internal TypeResolver TypeResolver { get; }
    public StringComparer Comparer { get; }
    internal FixedDictionary<Type, IAotTypeMetadata>? AotMetadata { get; }

    private CsEvalConfig(
        FixedDictionary<string, Func<object?[], object?>> functions,
        FixedDictionary<string, ModuleInfo> modules,
        ImmutableArray<Type> extensionTypes,
        TypeMetadataProvider typeMetadata,
        TypeResolver typeResolver,
        StringComparer comparer,
        FixedDictionary<Type, IAotTypeMetadata>? aotMetadata)
    {
        Functions = functions;
        Modules = modules;
        ExtensionTypes = extensionTypes;
        TypeMetadata = typeMetadata;
        TypeResolver = typeResolver;
        Comparer = comparer;
        AotMetadata = aotMetadata;
    }

    internal static CsEvalConfig Create(
        Dictionary<string, Func<object?[], object?>> functions,
        Dictionary<string, ModuleInfo> modules,
        List<Type> extensionTypes,
        TypeMetadataProvider typeMetadata,
        TypeResolver typeResolver,
        StringComparer comparer,
        Dictionary<Type, IAotTypeMetadata>? aotMetadata = null)
    {
        return new CsEvalConfig(
            FixedDictionary<string, Func<object?[], object?>>.Create(functions, comparer),
            FixedDictionary<string, ModuleInfo>.Create(modules, comparer),
            [..extensionTypes],
            typeMetadata,
            typeResolver,
            comparer,
            aotMetadata != null ? FixedDictionary<Type, IAotTypeMetadata>.Create(aotMetadata) : null);
    }

    internal static readonly CsEvalConfig Empty = new(
        FixedDictionary<string, Func<object?[], object?>>.Empty,
        FixedDictionary<string, ModuleInfo>.Empty,
        [],
        new TypeMetadataProvider(),
        TypeResolver.Create([], [], true, StringComparer.Ordinal),
        StringComparer.Ordinal,
        null);
}

internal sealed class ModuleInfo
{
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)]
    public Type Type { get; }
    public object? Instance { get; }
    public IReadOnlyDictionary<string, MemberInfo> Members { get; }

    public ModuleInfo(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)]
        Type type,
        object? instance,
        IReadOnlyDictionary<string, MemberInfo> members)
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
