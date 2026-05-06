namespace Alder.Aot;

/// <summary>
/// Base class for explicit AOT-generated generic static dispatch.
/// This covers only exact rooted generic method shapes that a context chooses to expose.
/// </summary>
public abstract class GenericStaticDispatch
{
    /// <summary>The declaring CLR type that owns the generic static methods.</summary>
    public abstract Type DeclaringType { get; }

    /// <summary>
    /// Attempts to invoke a rooted generic static method for the given declaring type.
    /// Implementations must return <c>false</c> on unsupported shapes and must not throw for expected misses.
    /// </summary>
    public abstract bool TryInvoke(string methodName, Type? requestedType, object?[] args, out object? result);
}
