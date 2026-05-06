using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IncrementDecrementExpr))]
internal static class IncrementDecrementBinder
{
    public static BoundExpr Bind(IncrementDecrementExpr expr, BindingContext context, BinderContext binder)
    {
        // §12.8.15: the operand of ++ / -- must be writable. Foreach iteration variables are
        // readonly per §13.9.5; Roslyn emits CS1656 with the "kind" text rather than CS1059.
        var readOnlyReason = context.GetReadOnlyReason(expr.Name.Lexeme);
        if (readOnlyReason == ReadOnlyReason.IterationVariable)
            throw new AlderException(
                DiagnosticDescriptors.ReadOnlyAssignmentToReservedKind,
                expr.Name.Lexeme, "foreach iteration variable");
        if (readOnlyReason != ReadOnlyReason.None)
            throw new AlderException(DiagnosticDescriptors.IncrementDecrementRequiresVariable);
        var target = NamedTargetBindingService.Resolve(expr.Name.Lexeme, context, BoundType.Unknown);
        var staticType = target.StaticType;
        // §12.8.15 / CS0023: ++ and -- are only defined on numeric, char, enum, and pointer types.
        if (staticType is not BoundUnknownType)
        {
            var clrType = Nullable.GetUnderlyingType(staticType.ClrType) ?? staticType.ClrType;
            if (clrType != typeof(object) && !TypeHelpers.IsArithmetic(clrType) && clrType != typeof(char) && !clrType.IsEnum)
            {
                var opLexeme = TokenLexemes.GetCanonical(expr.Op.Type);
                throw new AlderException(DiagnosticDescriptors.BadUnaryOp, opLexeme, clrType.Name);
            }
        }
        return new BoundIncrementDecrementExpr(
            expr.Name.Lexeme,
            expr.Op.Type,
            expr.IsPrefix,
            staticType,
            target.LocalId);
    }
}
