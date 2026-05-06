namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.VariableDeclaration, "VariableDecl")]
internal sealed partial record BoundVariableDeclExpr(
    string Name,
    BoundExpr Initializer,
    Type? DeclaredType,
    BoundType StaticType,
    bool IsConst = false,
    int? LocalId = null) : BoundExpr(StaticType);
