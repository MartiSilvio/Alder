using System.Collections.Immutable;
using Alder.Runtime;

namespace Alder.Binding;

internal enum ReadOnlyReason
{
    None,
    Const,
    IterationVariable,
}

internal sealed class BindingContext
{
    private readonly BindingContext? _parent;
    private readonly BindingReceiver? _receiver;
    private readonly Dictionary<string, (BoundType Type, int LocalId)> _locals;
    private readonly Dictionary<string, ReadOnlyReason> _readOnlyLocals;
    private readonly BindingContext _root;
    private int _nextLocalId;

    public BindingContext(AlderContext context, BindingReceiver? receiver = null)
        : this(context, parent: null, receiver)
    {
    }

    private BindingContext(AlderContext context, BindingContext? parent, BindingReceiver? receiver)
    {
        RuntimeContext = context;
        _parent = parent;
        _receiver = parent?._receiver ?? receiver;
        _locals = new Dictionary<string, (BoundType, int)>(context.Comparer);
        _readOnlyLocals = new Dictionary<string, ReadOnlyReason>(context.Comparer);
        _root = parent?._root ?? this;
    }

    internal AlderContext RuntimeContext { get; }

    internal LanguageMode LanguageMode => RuntimeContext.Config.LanguageMode;
    internal bool IsCaseSensitive => ReferenceEquals(RuntimeContext.Comparer, StringComparer.Ordinal);
    internal int LocalCount { get => _root._nextLocalId; set => _root._nextLocalId = value; }
    internal BindingContext CreateChildScope() => new(RuntimeContext, this, receiver: null);

    internal int DeclareLocal(string name, BoundType type, ReadOnlyReason readOnlyReason = ReadOnlyReason.None)
    {
        var id = _root._nextLocalId++;
        _locals[name] = (type, id);
        if (readOnlyReason != ReadOnlyReason.None)
            _readOnlyLocals[name] = readOnlyReason;
        else
            _readOnlyLocals.Remove(name);
        return id;
    }

    internal bool IsReadOnlyLocal(string name) => GetReadOnlyReason(name) != ReadOnlyReason.None;

    internal ReadOnlyReason GetReadOnlyReason(string name)
    {
        if (_locals.ContainsKey(name))
            return _readOnlyLocals.TryGetValue(name, out var reason) ? reason : ReadOnlyReason.None;

        return _parent?.GetReadOnlyReason(name) ?? ReadOnlyReason.None;
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

        if (RuntimeContext.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = new BoundType(declaredType);
            return true;
        }

        if (RuntimeContext.TryGet(name, out var fallbackValue) && fallbackValue != null)
        {
            if (fallbackValue is StructuralObjectValue structural)
            {
                var members = ImmutableDictionary.CreateBuilder<string, Type>();
                foreach (var member in structural.TypeInfo.Members)
                    members[member.Name] = member.Type;

                type = new BoundStructuralType(
                    structural.GetType(),
                    members.ToImmutable(),
                    structuralInfo: structural.TypeInfo);
                return true;
            }

            type = new BoundType(fallbackValue.GetType());
            return true;
        }

        type = BoundType.Unknown;
        return false;
    }

    internal bool TryGetImplicitReceiver(out string receiverName, out BoundType receiverType, out int receiverLocalId)
    {
        if (_receiver is null || !_receiver.EnableImplicitReceiver)
        {
            receiverName = string.Empty;
            receiverType = BoundType.Unknown;
            receiverLocalId = -1;
            return false;
        }

        receiverName = _receiver.ReceiverName;
        if (TryGetLocal(receiverName, out receiverType, out receiverLocalId))
            return true;

        receiverType = new BoundType(_receiver.ReceiverType);
        receiverLocalId = -1;
        return true;
    }
}
