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
    /// Returns delegate factories for environments where runtime generic closure is unavailable or unsafe.
    /// Each entry maps a closed delegate type such as <c>Func&lt;int, bool&gt;</c> to a factory
    /// that wraps an Alder lambda in that delegate type.
    /// </summary>
    public virtual IReadOnlyDictionary<Type, Func<object, Delegate>>? GetDelegateFactories() => null;

    /// <summary>
    /// Returns extra dispatch entries for cases where runtime generic closure is not reliable,
    /// such as LINQ over value-type collections under NativeAOT.
    /// </summary>
    public virtual IReadOnlyList<TypedDispatch>? GetExtensionDispatches() => null;
}
