namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MethodGroup, "MethodGroup", ManualRewrite = true)]
internal sealed partial record BoundMethodGroupExpr(
    BoundExpr Target,
    Type DeclaringType,
    string MethodName,
    bool NullSafe,
    bool IsStatic,
    BoundType StaticType) : BoundMemberAccessBase(Target, MethodName, NullSafe, StaticType);
