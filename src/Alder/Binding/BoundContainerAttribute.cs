namespace Alder.Binding;

/// <summary>
/// Marks a record that holds <see cref="BoundExpr"/> children but is not itself a BoundExpr subclass.
/// The source generator reads the record's constructor parameters to discover nested children
/// for EnumerateChildren and rewriter generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class BoundContainerAttribute : Attribute;
