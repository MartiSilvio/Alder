using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding;

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
            : BoundType.Unknown;
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
            throw new AlderException(DiagnosticDescriptors.AssignmentRequiresVariable);
    }

    private BoundMemberAssignExpr BindMemberAssign(MemberAssignExpr memberAssign, BindingContext context)
    {
        var target = Bind(memberAssign.Object, context);
        var value = Bind(memberAssign.Value, context);
        var (resolvedMember, declaringType) = ResolveMemberForAssignment(target.StaticType.ClrType, memberAssign.Name.Lexeme, context);
        return new BoundMemberAssignExpr(target, memberAssign.Name.Lexeme, resolvedMember, declaringType, value, value.StaticType);
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
        var (resolvedMember, _) = ResolveMemberForAssignment(target.StaticType.ClrType, memberCompoundAssign.MemberName, context);
        var memberType = ExtractMemberType(resolvedMember);
        var staticType = memberType != typeof(object)
            ? InferBinaryResultType(memberCompoundAssign.Operator, new BoundType(memberType), value.StaticType)
            : typeof(object);
        return new BoundMemberCompoundAssignExpr(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            value,
            new BoundType(staticType));
    }

    private BoundIndexCompoundAssignExpr BindIndexCompoundAssign(IndexCompoundAssignExpr indexCompoundAssign, BindingContext context)
    {
        var target = Bind(indexCompoundAssign.Object, context);
        var index = Bind(indexCompoundAssign.Index, context);
        var value = Bind(indexCompoundAssign.Value, context);
        var indexResult = ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        var staticType = elementType != typeof(object)
            ? InferBinaryResultType(indexCompoundAssign.Operator, new BoundType(elementType), value.StaticType)
            : typeof(object);
        return new BoundIndexCompoundAssignExpr(target, index, indexCompoundAssign.Operator, value, new BoundType(staticType));
    }

    private BoundMemberNullCoalesceAssignExpr BindMemberNullCoalesceAssign(
        MemberNullCoalesceAssignExpr memberNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(memberNullCoalesceAssign.Object, context);
        var value = Bind(memberNullCoalesceAssign.Value, context);
        var (resolvedMember, _) = ResolveMemberForAssignment(target.StaticType.ClrType, memberNullCoalesceAssign.MemberName, context);
        var memberType = ExtractMemberType(resolvedMember);
        return new BoundMemberNullCoalesceAssignExpr(target, memberNullCoalesceAssign.MemberName, value, new BoundType(memberType));
    }

    private BoundIndexNullCoalesceAssignExpr BindIndexNullCoalesceAssign(
        IndexNullCoalesceAssignExpr indexNullCoalesceAssign,
        BindingContext context)
    {
        var target = Bind(indexNullCoalesceAssign.Object, context);
        var index = Bind(indexNullCoalesceAssign.Index, context);
        var value = Bind(indexNullCoalesceAssign.Value, context);
        var indexResult = ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        return new BoundIndexNullCoalesceAssignExpr(target, index, value, new BoundType(elementType));
    }

    private BoundMemberIncrementExpr BindMemberIncrement(MemberIncrementExpr memberIncrement, BindingContext context)
    {
        var target = Bind(memberIncrement.Object, context);
        var (resolvedMember, _) = ResolveMemberForAssignment(target.StaticType.ClrType, memberIncrement.MemberName, context);
        var memberType = ExtractMemberType(resolvedMember);
        return new BoundMemberIncrementExpr(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsPrefix,
            memberIncrement.IsIncrement,
            new BoundType(memberType));
    }

    private BoundIndexIncrementExpr BindIndexIncrement(IndexIncrementExpr indexIncrement, BindingContext context)
    {
        var target = Bind(indexIncrement.Object, context);
        var index = Bind(indexIncrement.Index, context);
        var indexResult = ResolveIndexPlan(target.StaticType.ClrType, index.StaticType.ClrType, context);
        var elementType = indexResult?.ResultType ?? ResolveIndexElementTypeFallback(target.StaticType.ClrType);
        return new BoundIndexIncrementExpr(
            target,
            index,
            indexIncrement.IsPrefix,
            indexIncrement.IsIncrement,
            new BoundType(elementType));
    }

    private (MemberInfo? ResolvedMember, Type? DeclaringType) ResolveMemberForAssignment(Type targetType, string memberName, BindingContext context)
    {
        if (targetType == typeof(object))
            return (null, null);

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        if (!memberBinder.TryBindMemberRead(new BoundType(targetType), memberName, false, context.IsCaseSensitive,
                out var result, out var member, out _))
            return (null, null);

        return result is MemberBindResult.Property or MemberBindResult.Field
            ? (member, targetType)
            : (null, null);
    }

    private static Type ExtractMemberType(MemberInfo? member) => member switch
    {
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType,
        _ => typeof(object)
    };

    private IndexBindResult? ResolveIndexPlan(Type targetType, Type indexType, BindingContext context)
    {
        if (targetType == typeof(object))
            return null;

        var memberBinder = new MemberBinderService(context.RuntimeContext.TypeMetadata);
        return memberBinder.TryBindIndexRead(targetType, indexType, out var result)
            ? result
            : null;
    }

    private static Type ResolveIndexElementTypeFallback(Type targetType) =>
        targetType == typeof(object) ? typeof(object) : InferElementType(targetType);
}
