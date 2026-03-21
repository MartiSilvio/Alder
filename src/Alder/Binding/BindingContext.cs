using Alder.Runtime;

namespace Alder.Binding;

internal sealed class BindingContext
{
    private readonly AlderContext _context;
    private readonly BindingContext? _parent;
    private readonly Dictionary<string, (Type Type, int LocalId)> _locals;
    private readonly HashSet<string> _readOnlyLocals;
    private readonly BindingContext _root;
    private int _nextLocalId;

    public BindingContext(AlderContext context)
        : this(context, parent: null)
    {
    }

    private BindingContext(AlderContext context, BindingContext? parent)
    {
        _context = context;
        _parent = parent;
        _locals = new Dictionary<string, (Type, int)>(context.Comparer);
        _readOnlyLocals = new HashSet<string>(context.Comparer);
        _root = parent?._root ?? this;
    }

    internal AlderContext RuntimeContext => _context;
    internal bool IsCaseSensitive => ReferenceEquals(_context.Comparer, StringComparer.Ordinal);
    internal BindingContext CreateChildScope() => new(_context, this);

    internal int DeclareLocal(string name, Type type, bool isReadOnly = false)
    {
        var id = _root._nextLocalId++;
        _locals[name] = (type, id);
        if (isReadOnly)
            _readOnlyLocals.Add(name);
        else
            _readOnlyLocals.Remove(name);
        return id;
    }

    internal bool IsReadOnlyLocal(string name)
    {
        if (_locals.ContainsKey(name))
            return _readOnlyLocals.Contains(name);

        return _parent?.IsReadOnlyLocal(name) ?? false;
    }

    internal bool TryGetLocal(string name, out Type type, out int localId)
    {
        if (_locals.TryGetValue(name, out var entry))
        {
            type = entry.Type;
            localId = entry.LocalId;
            return true;
        }

        if (_parent != null)
            return _parent.TryGetLocal(name, out type, out localId);

        type = typeof(object);
        localId = -1;
        return false;
    }

    public bool TryGetVariableType(string name, out Type type)
    {
        if (TryGetLocal(name, out type, out _))
            return true;

        if (_context.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = declaredType;
            return true;
        }

        if (_context.TryGet(name, out var fallbackValue) && fallbackValue != null)
        {
            type = fallbackValue.GetType();
            return true;
        }

        type = typeof(object);
        return false;
    }
}
