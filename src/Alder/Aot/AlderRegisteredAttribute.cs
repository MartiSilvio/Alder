namespace Alder.Aot;

/// <summary>
/// Marks a generated <see cref="AlderTypeContext"/> with the types it provides AOT metadata for.
/// Applied by the source generator for discovery and diagnostics.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AlderRegisteredAttribute : Attribute
{
    /// <summary>The type that this context provides AOT metadata for.</summary>
    public Type Type { get; }

    /// <param name="type">The type with AOT metadata.</param>
    public AlderRegisteredAttribute(Type type) => Type = type;
}
