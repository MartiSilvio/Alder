using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Dynamic;

namespace CsEval.Runtime;

/// <summary>
/// Thread-safe evaluation context for CsEval expressions.
/// Uses ConcurrentDictionary for thread-safe variable access across parent/child relationships.
/// Parent contexts are never modified by child evaluations - children have isolated state.
/// </summary>
public sealed class CsEvalContext
{
    private readonly ConcurrentDictionary<string, object?> _variables;
    private readonly ConcurrentDictionary<string, Type> _variableTypes;
    private readonly CsEvalContext? _parent;
    private readonly CsEvalConfig _config;

    public CsEvalContext(CsEvalConfig config) : this(config, null, null)
    {
    }

    public CsEvalContext(CsEvalConfig config, IServiceProvider? serviceProvider) : this(config, null, serviceProvider)
    {
    }

    private CsEvalContext(CsEvalConfig config, CsEvalContext? parent, IServiceProvider? serviceProvider)
    {
        _config = config;
        _parent = parent;
        ServiceProvider = serviceProvider ?? parent?.ServiceProvider;
        _variables = new ConcurrentDictionary<string, object?>(_config.Comparer);
        _variableTypes = new ConcurrentDictionary<string, Type>(_config.Comparer);
    }

    public CsEvalConfig Config => _config;
    public StringComparer Comparer => _config.Comparer;
    public IServiceProvider? ServiceProvider { get; }
    internal TypeCache TypeCache => _config.TypeCache;
    internal TypeResolver TypeResolver => _config.TypeResolver;
    internal FrozenDictionary<string, Func<object?[], object?>> Functions => _config.Functions;
    internal FrozenDictionary<string, ModuleInfo> Modules => _config.Modules;
    internal ImmutableArray<Type> ExtensionTypes => _config.ExtensionTypes;

    public void Define(string name, object? value) => _variables[name] = value;

    public void Define(string name, object? value, Type inferredType)
    {
        _variables[name] = value;
        _variableTypes[name] = inferredType;
    }

    /// <summary>
    /// Defines a new variable, enforcing C# shadowing rules.
    /// Throws if the variable already exists in the current scope or any parent scope.
    /// </summary>
    public void DefineNew(string name, object? value, Type inferredType)
    {
        if (Contains(name))
            throw new CsEvalException($"A local variable named '{name}' is already defined in this scope");

        _variables[name] = value;
        _variableTypes[name] = inferredType;
    }

    public bool TryGetVariableType(string name, out Type? type)
    {
        if (_variableTypes.TryGetValue(name, out type!))
            return true;

        if (_parent != null)
            return _parent.TryGetVariableType(name, out type);

        type = null;
        return false;
    }

    public bool TryGet(string name, out object? value)
    {
        if (_variables.TryGetValue(name, out value))
            return true;

        if (_parent != null)
            return _parent.TryGet(name, out value);

        value = null;
        return false;
    }

    public object? Get(string name)
    {
        if (TryGet(name, out var value))
            return value;
        throw new CsEvalException($"Undefined variable '{name}'");
    }

    public void Set(string name, object? value)
    {
        if (_variables.ContainsKey(name))
        {
            _variables[name] = value;
            return;
        }

        if (_parent != null && _parent.Contains(name))
        {
            _parent.Set(name, value);
            return;
        }

        throw new CsEvalException($"Undefined variable '{name}'");
    }

    private bool Contains(string name)
    {
        if (_variables.ContainsKey(name))
            return true;
        return _parent?.Contains(name) ?? false;
    }

    public CsEvalContext CreateChild() => new(_config, this, null);

    public IReadOnlyDictionary<string, object?> GetAll() => _variables;

    public static CsEvalContext FromExpandoObject(ExpandoObject? expando, CsEvalConfig config)
    {
        var ctx = new CsEvalContext(config);
        if (expando == null) return ctx;

        foreach (var kvp in (IDictionary<string, object?>)expando)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    public static CsEvalContext FromDictionary(IDictionary<string, object?>? dict, CsEvalConfig config)
    {
        var ctx = new CsEvalContext(config);
        if (dict == null) return ctx;

        foreach (var kvp in dict)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }
}
