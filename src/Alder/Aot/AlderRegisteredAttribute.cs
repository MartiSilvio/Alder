namespace Alder.Aot;

/// <summary>
/// Marks an <see cref="AlderTypeContext"/> with the CLR types it covers.
/// Alder uses this for discovery, diagnostics, and duplicate-registration checks.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AlderRegisteredAttribute : Attribute
{
    /// <summary>The CLR type described by the annotated context.</summary>
    public Type Type { get; }

    /// <param name="type">The type covered by the generated AOT metadata.</param>
    public AlderRegisteredAttribute(Type type) => Type = type;
}
