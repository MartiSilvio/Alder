namespace Alder.Aot;

/// <summary>
/// Built-in AOT type context. The source generator automatically includes all C# primitives,
/// common BCL types, and their generic instantiations. Custom types are added via
/// <see cref="AlderRegisteredAttribute"/> on user-defined context classes.
/// </summary>
public partial class AlderBuiltInContext : AlderTypeContext;
