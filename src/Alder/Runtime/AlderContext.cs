using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Evaluation context for Alder expressions.
/// Root contexts use ConcurrentDictionary for concurrent lookup and slot replacement.
/// This does not provide transactional semantics for compound read-modify-write operations
/// across multiple evaluation steps. Child contexts (block scopes) use plain Dictionary
/// and are intended for isolated scope mutation rather than shared concurrent writes.
/// </summary>
internal sealed class AlderContext
{
    private IDictionary<string, VariableSlot>? _variables;
    private readonly AlderContext? _parent;
    private int _variableTypeVersion;

    private static readonly IReadOnlyDictionary<string, object?> EmptyVariables =
        new Dictionary<string, object?>();

    private record struct VariableSlot(object? Value, Type? DeclaredType, bool IsReadOnly);

    public AlderContext(AlderConfig config) : this(config, null, null, useConcurrentStore: true)
    {
    }

    public AlderContext(AlderConfig config, IServiceProvider? serviceProvider) : this(config, null, serviceProvider, useConcurrentStore: true)
    {
    }

    private AlderContext(AlderConfig config, AlderContext? parent, IServiceProvider? serviceProvider, bool useConcurrentStore)
    {
        Config = config;
        _parent = parent;
        ServiceProvider = serviceProvider ?? parent?.ServiceProvider;
        if (useConcurrentStore)
        {
            _variables = new ConcurrentDictionary<string, VariableSlot>(Config.Comparer);
        }
    }

    internal CancellationToken ActiveCancellationToken;

    internal CancellationToken GetActiveCancellationToken()
    {
        for (var ctx = this; ctx != null; ctx = ctx._parent)
        {
            if (ctx.ActiveCancellationToken != default)
                return ctx.ActiveCancellationToken;
        }
        return default;
    }

    public AlderConfig Config { get; }

    public StringComparer Comparer => Config.Comparer;
    public IServiceProvider? ServiceProvider { get; }
    internal TypeMetadataProvider TypeMetadata => Config.TypeMetadata;
    internal TypeResolver TypeResolver => Config.TypeResolver;
    internal FixedDictionary<string, Func<object?[], object?>> Functions => Config.Functions;
    internal FixedDictionary<string, ModuleInfo> Modules => Config.Modules;
    internal ImmutableArray<Type> ExtensionTypes => Config.ExtensionTypes;

    public void Define(string name, object? value) =>
        GetOrCreateVariablesForWrite()[name] = new VariableSlot(value, null, false);

    public void Define(string name, object? value, Type inferredType, bool isReadOnly = false)
    {
        var variables = GetOrCreateVariablesForWrite();
        var typeChanged = !variables.TryGetValue(name, out var existing) || existing.DeclaredType != inferredType;
        variables[name] = new VariableSlot(value, inferredType, isReadOnly);
        if (typeChanged)
            Interlocked.Increment(ref _variableTypeVersion);
    }

    /// <summary>
    /// Defines a new variable, enforcing C# scoping rules.
    /// Throws if the variable already exists in the current scope.
    /// Allows shadowing of variables in parent scopes (§7.7).
    /// </summary>
    public void DefineNew(string name, object? value, Type inferredType, bool isReadOnly = false)
    {
        var variables = GetOrCreateVariablesForWrite();
        if (variables.ContainsKey(name))
            throw new AlderException(DiagnosticDescriptors.DuplicateLocalVariable, name);

        variables[name] = new VariableSlot(value, inferredType, isReadOnly);
        Interlocked.Increment(ref _variableTypeVersion);
    }

    public bool TryGetVariableType(string name, out Type? type)
    {
        if (_variables != null && _variables.TryGetValue(name, out var slot) && slot.DeclaredType != null)
        {
            type = slot.DeclaredType;
            return true;
        }

        if (_parent != null)
            return _parent.TryGetVariableType(name, out type);

        type = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string name, out object? value)
    {
        if (_variables != null && _variables.TryGetValue(name, out var slot))
        {
            value = slot.Value;
            return true;
        }

        if (_parent != null)
            return _parent.TryGet(name, out value);

        value = null;
        return false;
    }

    public object? Get(string name)
    {
        if (TryGet(name, out var value))
            return value;
        throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetVariableTyped<T>(string name)
    {
        object? value;
        if (_parent == null)
        {
            if (_variables == null || !_variables.TryGetValue(name, out var slot))
                throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
            value = slot.Value;
        }
        else if (!TryGet(name, out value))
        {
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
        }

        return TypeHelpers.CoerceToType<T>(value);
    }

    public void Set(string name, object? value)
    {
        if (_variables != null && _variables.TryGetValue(name, out var slot))
        {
            if (slot.IsReadOnly)
                throw new AlderException(DiagnosticDescriptors.ReadonlyAssignment);
            _variables[name] = slot with { Value = value };
            return;
        }

        if (_parent != null && _parent.Contains(name))
        {
            _parent.Set(name, value);
            return;
        }

        throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
    }

    private bool Contains(string name)
    {
        if (_variables != null && _variables.ContainsKey(name))
            return true;
        return _parent?.Contains(name) ?? false;
    }

    public AlderContext CreateChild()
    {
        return new AlderContext(Config, this, null, useConcurrentStore: false);
    }

    internal void ClearScope()
    {
        _variables?.Clear();
        Interlocked.Increment(ref _variableTypeVersion);
    }

    public IReadOnlyDictionary<string, object?> GetAllVisible()
    {
        var result = new Dictionary<string, object?>(Config.Comparer);
        if (_parent != null)
        {
            foreach (var (name, value) in _parent.GetAllVisible())
                result[name] = value;
        }

        if (_variables != null)
        {
            foreach (var kvp in _variables)
                result[kvp.Key] = kvp.Value.Value;
        }
        return result;
    }

    private IDictionary<string, VariableSlot> GetOrCreateVariablesForWrite()
    {
        return _variables ??= new Dictionary<string, VariableSlot>(Config.Comparer);
    }

    internal int GetTypeInferenceVersion()
    {
        var version = _variableTypeVersion;
        if (_parent != null)
            version = unchecked((version * 397) ^ _parent.GetTypeInferenceVersion());
        return version;
    }

    public static AlderContext FromExpandoObject(ExpandoObject? expando, AlderConfig config)
    {
        var ctx = new AlderContext(config);
        if (expando == null) return ctx;

        foreach (var kvp in (IDictionary<string, object?>)expando)
            ctx.Define(kvp.Key, kvp.Value);
        return ctx;
    }

    public static AlderContext FromDictionary(IDictionary<string, object?>? dict, AlderConfig config)
    {
        var ctx = new AlderContext(config);
        if (dict == null) return ctx;

        foreach (var kvp in dict)
            ctx.Define(kvp.Key, kvp.Value);
        return ctx;
    }
}
