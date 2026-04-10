namespace Alder.Aot;

/// <summary>
/// Base class for AOT-generated type contexts. Each context provides <see cref="TypedDispatch"/>
/// instances for a set of registered types, enabling reflection-free member access at runtime.
/// </summary>
public abstract class AlderTypeContext
{
    /// <summary>Returns the typed dispatch entries provided by this context.</summary>
    public abstract IReadOnlyList<TypedDispatch> GetTypeMetadata();

    /// <summary>
    /// Returns pre-instantiated delegate factories for AOT environments where
    /// <c>MakeGenericMethod</c> is unavailable for value-type generic arguments.
    /// Each entry maps a closed delegate type (e.g., <c>Func&lt;int, bool&gt;</c>) to a
    /// factory that wraps a <c>LambdaValue</c> in that delegate type.
    /// </summary>
    public virtual IReadOnlyDictionary<Type, Func<object, Delegate>>? GetDelegateFactories() => null;

    /// <summary>
    /// Returns extension method dispatch entries for value-type collections
    /// (e.g., LINQ methods on <c>List&lt;int&gt;</c>) where <c>MakeGenericMethod</c>
    /// fails under NativeAOT.
    /// </summary>
    public virtual IReadOnlyList<TypedDispatch>? GetExtensionDispatches() => null;
}
