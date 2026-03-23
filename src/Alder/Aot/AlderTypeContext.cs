namespace Alder.Aot;

/// <summary>
/// Base class for AOT-generated type contexts. Each generated context provides <see cref="IAotTypeMetadata"/>
/// for a set of registered types, enabling reflection-free member access at runtime.
/// </summary>
public abstract class AlderTypeContext
{
    /// <summary>
    /// Returns the collection of AOT type metadata entries provided by this context.
    /// </summary>
    /// <returns>A list of type metadata for all types registered in this context.</returns>
    public abstract IReadOnlyList<IAotTypeMetadata> GetTypeMetadata();
}
