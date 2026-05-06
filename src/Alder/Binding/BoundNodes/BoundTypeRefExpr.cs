namespace Alder.Binding.BoundNodes;

// ECMA-334 §12.8.7.2: a type name used in expression position — e.g. `int.Parse("5")`, where
// `int` is the target of a static member lookup. This is *not* a runtime value; it is a
// compile-time binding context that directs member access at the wrapped CLR type.
//
// Distinct from `BoundLiteralExpr { Value: Type }`, which represents a *runtime* `System.Type`
// value produced by `typeof(...)` or `obj.GetType()`. Member access on the literal path
// resolves against `System.Type`'s instance surface; member access on this node resolves
// statically against <see cref="TargetType"/>. The two must never share a bound node —
// `typeof(int).ToString()` and `int.MaxValue` are spec-different constructs.
//
// StaticType is always `BoundType(typeof(Type))` so that, in the rare case this node is
// evaluated as a value (e.g. as a method argument), it materializes as the wrapped Type.
[BoundNode(BoundNodeKind.TypeReference, "TypeReference")]
internal sealed partial record BoundTypeRefExpr(Type TargetType, BoundType StaticType) : BoundExpr(StaticType);
