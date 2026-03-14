namespace CsEval.Binding.BoundNodes;

internal sealed record BoundGotoExpr(string Label, Type StaticType) : BoundExpr(StaticType);
internal sealed record BoundGotoCaseExpr(BoundExpr Value, Type StaticType) : BoundExpr(StaticType);
internal sealed record BoundGotoDefaultExpr(Type StaticType) : BoundExpr(StaticType);
internal sealed record BoundLabelExpr(string Name, Type StaticType) : BoundExpr(StaticType);
