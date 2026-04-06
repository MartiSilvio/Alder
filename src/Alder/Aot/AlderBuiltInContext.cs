namespace Alder.Aot;

/// <summary>
/// Built-in AOT type context. The generator automatically includes all C# primitives,
/// common BCL types, and their generic instantiations. No [AlderRegistered] attributes needed.
/// Users can add custom types via [AlderRegistered] on their own context class.
/// </summary>
public partial class AlderBuiltInContext : AlderTypeContext;
