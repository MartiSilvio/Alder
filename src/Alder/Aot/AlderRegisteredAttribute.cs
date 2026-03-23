namespace Alder.Aot;

/// <summary>
/// Applied by the Alder source generator to mark a generated <see cref="AlderTypeContext"/> with
/// the types it provides AOT metadata for. Used for discovery and diagnostic purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AlderRegisteredAttribute : Attribute
{
    /// <summary>
    /// Gets the type that this context provides AOT metadata for.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Creates an attribute indicating that AOT metadata is provided for the specified type.
    /// </summary>
    /// <param name="type">The type with AOT metadata.</param>
    public AlderRegisteredAttribute(Type type) => Type = type;
}
