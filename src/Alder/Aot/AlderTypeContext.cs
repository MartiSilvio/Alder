namespace Alder.Aot;

/// <summary>
/// Base class for AOT-generated type contexts. Each generated context provides <see cref="TypedDispatch"/>
/// for a set of registered types, enabling reflection-free member access at runtime.
/// </summary>
public abstract class AlderTypeContext
{
    /// <summary>
    /// Returns the collection of typed dispatch entries provided by this context.
    /// </summary>
    public abstract IReadOnlyList<TypedDispatch> GetTypeMetadata();

    /// <summary>
    /// Returns pre-instantiated delegate factories for AOT environments where
    /// MakeGenericMethod is unavailable for value-type generic arguments.
    /// Each entry maps a closed delegate type (e.g., Func&lt;int, bool&gt;) to a
    /// factory that wraps a LambdaValue in that delegate type.
    /// </summary>
    public virtual IReadOnlyDictionary<Type, Func<object, Delegate>>? GetDelegateFactories() => null;

    /// <summary>
    /// Returns extension method dispatch entries for value-type collections.
    /// These handle LINQ methods (Where, Select, etc.) under NativeAOT where
    /// MakeGenericMethod fails for value-type element types.
    /// </summary>
    public virtual IReadOnlyList<TypedDispatch>? GetExtensionDispatches() => null;
}
