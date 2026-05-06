namespace Alder.Aot;

/// <summary>
/// Base class for AOT-generated type contexts.
/// A context supplies pre-generated dispatch metadata so Alder can avoid reflection in trim-sensitive environments.
/// </summary>
public abstract class AlderTypeContext
{
    /// <summary>Returns the typed dispatch entries provided by this context.</summary>
    public abstract IReadOnlyList<TypedDispatch> GetTypeMetadata();

    /// <summary>
    /// Returns explicit rooted generic static dispatchers for this context.
    /// Each dispatcher covers a bounded set of closed generic method shapes for a single declaring type.
    /// </summary>
    public virtual IReadOnlyList<GenericStaticDispatch>? GetGenericStaticDispatch() => null;

    /// <summary>
    /// Returns delegate factories for environments where runtime generic closure is unavailable or unsafe.
    /// Each entry maps a closed delegate type such as <c>Func&lt;int, bool&gt;</c> to a factory
    /// that wraps an Alder lambda in that delegate type.
    /// </summary>
    public virtual IReadOnlyDictionary<RootedType, Func<object, Delegate>>? GetDelegateFactories() => null;

    /// <summary>
    /// Returns the closed delegate types rooted by this context.
    /// By default this is derived from <see cref="GetDelegateFactories"/> when available.
    /// </summary>
    public virtual IReadOnlyCollection<RootedType>? GetRootedDelegateTypes() => GetDelegateFactories()?.Keys.ToArray();
}
