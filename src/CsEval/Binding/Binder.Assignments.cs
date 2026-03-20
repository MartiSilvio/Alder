using CsEval.Binding.BoundNodes;
using CsEval.Binding.Services;
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
        var memberType = ResolveMemberType(target.StaticType, memberCompoundAssign.MemberName);
        var staticType = memberType != typeof(object)
            ? InferBinaryResultType(memberCompoundAssign.Operator, memberType, value.StaticType)
            : typeof(object);
        return new BoundMemberCompoundAssignExpr(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            value,
            staticType);
    }

    private BoundIndexCompoundAssignExpr BindIndexCompoundAssign(IndexCompoundAssignExpr indexCompoundAssign, BindingContext context)
    {
        var target = Bind(indexCompoundAssign.Object, context);
        var index = Bind(indexCompoundAssign.Index, context);
        var value = Bind(indexCompoundAssign.Value, context);
        var elementType = ResolveIndexElementType(target.StaticType, index.StaticType, context);
        var staticType = elementType != typeof(object)
            ? InferBinaryResultType(indexCompoundAssign.Operator, elementType, value.StaticType)
            : typeof(object);
        return new BoundIndexCompoundAssignExpr(target, index, indexCompoundAssign.Operator, value, staticType);
    }

    private BoundMemberNullCoalesceAssignExpr BindMemberNullCoalesceAssign(
        MemberNullCoalesceAssignExpr memberNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(memberNullCoalesceAssign.Object, context);
        var value = Bind(memberNullCoalesceAssign.Value, context);
        var memberType = ResolveMemberType(target.StaticType, memberNullCoalesceAssign.MemberName);
        return new BoundMemberNullCoalesceAssignExpr(target, memberNullCoalesceAssign.MemberName, value, memberType);
    }

    private BoundIndexNullCoalesceAssignExpr BindIndexNullCoalesceAssign(
        IndexNullCoalesceAssignExpr indexNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(indexNullCoalesceAssign.Object, context);
        var index = Bind(indexNullCoalesceAssign.Index, context);
        var value = Bind(indexNullCoalesceAssign.Value, context);
        var elementType = ResolveIndexElementType(target.StaticType, index.StaticType, context);
        return new BoundIndexNullCoalesceAssignExpr(target, index, value, elementType);
    }

    private BoundMemberIncrementExpr BindMemberIncrement(MemberIncrementExpr memberIncrement, BindingContext context)
    {
        var target = Bind(memberIncrement.Object, context);
        var memberType = ResolveMemberType(target.StaticType, memberIncrement.MemberName);
        return new BoundMemberIncrementExpr(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsPrefix,
            memberIncrement.IsIncrement,
            memberType);
    }

    private BoundIndexIncrementExpr BindIndexIncrement(IndexIncrementExpr indexIncrement, BindingContext context)
    {
        var target = Bind(indexIncrement.Object, context);
        var index = Bind(indexIncrement.Index, context);
        var elementType = ResolveIndexElementType(target.StaticType, index.StaticType, context);
        return new BoundIndexIncrementExpr(
            target,
            index,
            indexIncrement.IsPrefix,
            indexIncrement.IsIncrement,
            elementType);
    }

    private static Type ResolveMemberType(Type targetType, string memberName)
    {
        if (targetType == typeof(object))
            return typeof(object);

        var property = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
            return property.PropertyType;

        var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
            return field.FieldType;

        return typeof(object);
    }

    private Type ResolveIndexElementType(Type targetType, Type indexType, BindingContext context)
    {
        if (targetType == typeof(object))
            return typeof(object);

        try
        {
            var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
            var plan = memberBinder.BindIndexRead(targetType, indexType);
            return plan.ResultType;
        }
        catch (CsEvalException)
        {
            return InferElementType(targetType);
        }
    }
}
