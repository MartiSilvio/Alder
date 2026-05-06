using System.Diagnostics.CodeAnalysis;

namespace Alder.Aot;

/// <summary>
/// Reflection-free member access and method dispatch for a specific CLR type.
/// Generated implementations are checked before Alder falls back to reflection.
/// Override only the operations your type supports. The base implementation returns <c>false</c>
/// for every operation so callers can continue with fallback behavior.
/// </summary>
public abstract class TypedDispatch
{
    /// <summary>The CLR type handled by this dispatch instance.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public abstract Type Type { get; }

    /// <summary>Attempts to read an instance member by name.</summary>
    public virtual bool TryGet(string name, object instance, out object? value) { value = default; return false; }

    /// <summary>Attempts to write an instance member by name.</summary>
    public virtual bool TrySet(string name, object instance, object? value) => false;

    /// <summary>Attempts to read a static member by name.</summary>
    public virtual bool TryGetStatic(string name, out object? value) { value = default; return false; }

    /// <summary>Attempts to read an indexed value, for example <c>list[0]</c>.</summary>
    public virtual bool TryGetIndex(object instance, object key, out object? value) { value = default; return false; }

    /// <summary>Attempts to write an indexed value, for example <c>list[0] = x</c>.</summary>
    public virtual bool TrySetIndex(object instance, object key, object? value) => false;

    /// <summary>Attempts to construct an instance with the supplied arguments.</summary>
    public virtual bool TryCreate(object?[] args, out object? instance) { instance = default; return false; }

    /// <summary>Attempts to invoke an instance method by name.</summary>
    public virtual bool TryInvoke(string name, object instance, object?[] args, out object? result) { result = default; return false; }

    /// <summary>Attempts to invoke a static method by name.</summary>
    public virtual bool TryInvokeStatic(string name, object?[] args, out object? result) { result = default; return false; }
}
