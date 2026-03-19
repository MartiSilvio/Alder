using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Parsing;

namespace CsEval.Binding;

internal sealed partial class Binder
{
    private BoundAssignExpr BindAssign(AssignExpr assign, BindingContext context)
    {
        EnsureVariableIsAssignable(assign.Name.Lexeme, context);
        var value = Bind(assign.Value, context);
        var staticType = context.TryGetVariableType(assign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isAssignLocal = context.TryGetLocal(assign.Name.Lexeme, out _, out var assignLocalId);
        return new BoundAssignExpr(assign.Name.Lexeme, value, staticType, isAssignLocal ? assignLocalId : null);
    }

    private BoundNullCoalesceAssignExpr BindNullCoalesceAssign(NullCoalesceAssignExpr nullCoalesceAssign, BindingContext context)
    {
        EnsureVariableIsAssignable(nullCoalesceAssign.Name.Lexeme, context);
        var value = Bind(nullCoalesceAssign.Value, context);
        var staticType = context.TryGetVariableType(nullCoalesceAssign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isNcaLocal = context.TryGetLocal(nullCoalesceAssign.Name.Lexeme, out _, out var ncaLocalId);
        return new BoundNullCoalesceAssignExpr(nullCoalesceAssign.Name.Lexeme, value, staticType, isNcaLocal ? ncaLocalId : null);
    }

    private BoundCompoundAssignExpr BindCompoundAssign(CompoundAssignExpr compoundAssign, BindingContext context)
    {
        EnsureVariableIsAssignable(compoundAssign.Name.Lexeme, context);
        var value = Bind(compoundAssign.Value, context);
        var staticType = context.TryGetVariableType(compoundAssign.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isCaLocal = context.TryGetLocal(compoundAssign.Name.Lexeme, out _, out var caLocalId);
        return new BoundCompoundAssignExpr(compoundAssign.Name.Lexeme, compoundAssign.Op.Type, value, staticType, isCaLocal ? caLocalId : null);
    }

    private BoundIncrementDecrementExpr BindIncrementDecrement(IncrementDecrementExpr incrementDecrement, BindingContext context)
    {
        EnsureVariableIsAssignable(incrementDecrement.Name.Lexeme, context);
        var staticType = context.TryGetVariableType(incrementDecrement.Name.Lexeme, out var variableType)
            ? variableType
            : typeof(object);
        var isIdLocal = context.TryGetLocal(incrementDecrement.Name.Lexeme, out _, out var idLocalId);
        return new BoundIncrementDecrementExpr(
            incrementDecrement.Name.Lexeme,
            incrementDecrement.Op.Type,
            incrementDecrement.IsPrefix,
            staticType,
            isIdLocal ? idLocalId : null);
    }

    private static void EnsureVariableIsAssignable(string variableName, BindingContext context)
    {
        if (context.IsReadOnlyLocal(variableName))
            throw new CsEvalException(DiagnosticDescriptors.AssignmentRequiresVariable);
    }

    private BoundMemberAssignExpr BindMemberAssign(MemberAssignExpr memberAssign, BindingContext context)
    {
        var target = Bind(memberAssign.Object, context);
        var value = Bind(memberAssign.Value, context);
        return new BoundMemberAssignExpr(target, memberAssign.Name.Lexeme, value, value.StaticType);
    }

    private BoundIndexAssignExpr BindIndexAssign(IndexAssignExpr indexAssign, BindingContext context)
    {
        var target = Bind(indexAssign.Object, context);
        var index = Bind(indexAssign.Index, context);
        var value = Bind(indexAssign.Value, context);
        return new BoundIndexAssignExpr(target, index, value, value.StaticType);
    }

    private BoundMemberCompoundAssignExpr BindMemberCompoundAssign(MemberCompoundAssignExpr memberCompoundAssign, BindingContext context)
    {
        var target = Bind(memberCompoundAssign.Object, context);
        var value = Bind(memberCompoundAssign.Value, context);
        return new BoundMemberCompoundAssignExpr(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            value,
            typeof(object));
    }

    private BoundIndexCompoundAssignExpr BindIndexCompoundAssign(IndexCompoundAssignExpr indexCompoundAssign, BindingContext context)
    {
        var target = Bind(indexCompoundAssign.Object, context);
        var index = Bind(indexCompoundAssign.Index, context);
        var value = Bind(indexCompoundAssign.Value, context);
        return new BoundIndexCompoundAssignExpr(target, index, indexCompoundAssign.Operator, value, typeof(object));
    }

    private BoundMemberNullCoalesceAssignExpr BindMemberNullCoalesceAssign(
        MemberNullCoalesceAssignExpr memberNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(memberNullCoalesceAssign.Object, context);
        var value = Bind(memberNullCoalesceAssign.Value, context);
        return new BoundMemberNullCoalesceAssignExpr(target, memberNullCoalesceAssign.MemberName, value, typeof(object));
    }

    private BoundIndexNullCoalesceAssignExpr BindIndexNullCoalesceAssign(
        IndexNullCoalesceAssignExpr indexNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(indexNullCoalesceAssign.Object, context);
        var index = Bind(indexNullCoalesceAssign.Index, context);
        var value = Bind(indexNullCoalesceAssign.Value, context);
        return new BoundIndexNullCoalesceAssignExpr(target, index, value, typeof(object));
    }

    private BoundMemberIncrementExpr BindMemberIncrement(MemberIncrementExpr memberIncrement, BindingContext context)
    {
        var target = Bind(memberIncrement.Object, context);
        return new BoundMemberIncrementExpr(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsPrefix,
            memberIncrement.IsIncrement,
            typeof(object));
    }

    private BoundIndexIncrementExpr BindIndexIncrement(IndexIncrementExpr indexIncrement, BindingContext context)
    {
        var target = Bind(indexIncrement.Object, context);
        var index = Bind(indexIncrement.Index, context);
        return new BoundIndexIncrementExpr(
            target,
            index,
            indexIncrement.IsPrefix,
            indexIncrement.IsIncrement,
            typeof(object));
    }
}
