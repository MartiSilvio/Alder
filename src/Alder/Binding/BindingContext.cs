using Alder.Runtime;

namespace Alder.Binding;

internal sealed class BindingContext
{
    private readonly AlderContext _context;
    private readonly BindingContext? _parent;
    private readonly Dictionary<string, (BoundType Type, int LocalId)> _locals;
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
        _locals = new Dictionary<string, (BoundType, int)>(context.Comparer);
        _readOnlyLocals = new HashSet<string>(context.Comparer);
        _root = parent?._root ?? this;
    }

    internal AlderContext RuntimeContext => _context;
    internal LanguageMode LanguageMode => _context.Config.LanguageMode;
    internal bool IsCaseSensitive => ReferenceEquals(_context.Comparer, StringComparer.Ordinal);
    internal int LocalCount => _root._nextLocalId;
    internal BindingContext CreateChildScope() => new(_context, this);

    internal int DeclareLocal(string name, BoundType type, bool isReadOnly = false)
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

    internal bool TryGetLocal(string name, out BoundType type, out int localId)
    {
        if (_locals.TryGetValue(name, out var entry))
        {
            type = entry.Type;
            localId = entry.LocalId;
            return true;
        }

        if (_parent != null)
            return _parent.TryGetLocal(name, out type, out localId);

        type = BoundType.Unknown;
        localId = -1;
        return false;
    }

    public bool TryGetVariableType(string name, out BoundType type)
    {
        if (TryGetLocal(name, out type, out _))
            return true;

        if (_context.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = new BoundType(declaredType);
            return true;
        }

        if (_context.TryGet(name, out var fallbackValue) && fallbackValue != null)
        {
            type = new BoundType(fallbackValue.GetType());
            return true;
        }

        type = BoundType.Unknown;
        return false;
    }
}
