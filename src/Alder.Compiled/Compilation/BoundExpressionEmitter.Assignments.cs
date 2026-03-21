using System.Collections;
using System.Reflection;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitVariableDecl(BoundVariableDeclExpr variableDecl)
    {
        if (TryGetPromoted(variableDecl.LocalId, out var promoted))
        {
            var value = EmitHelpers.AsObject(Emit(variableDecl.Initializer));
            if (variableDecl.DeclaredType != null)
            {
                value = LinqExpression.Call(
                    ValidateAndCoerceTypeMethod,
                    LinqExpression.Constant(variableDecl.DeclaredType, typeof(Type)),
                    value,
                    LinqExpression.Constant(variableDecl.Name),
                    LinqExpression.Constant(BoundExpr.IsConstantExpression(variableDecl.Initializer)));
            }
            return LinqExpression.Assign(promoted.Variable, value);
        }

        return LinqExpression.Call(
            DefineVariableMethod,
            LinqExpression.Constant(variableDecl.Name),
            EmitHelpers.AsObject(Emit(variableDecl.Initializer)),
            variableDecl.DeclaredType != null
                ? LinqExpression.Constant(variableDecl.DeclaredType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            _contextParam,
            LinqExpression.Constant(variableDecl.IsConst),
            LinqExpression.Constant(BoundExpr.IsConstantExpression(variableDecl.Initializer)));
    }

    private LinqExpression EmitAssign(BoundAssignExpr assign)
    {
        if (TryGetPromoted(assign.LocalId, out var promoted))
        {
            var valueType = assign.Value.StaticType;
            if (valueType != typeof(object) && promoted.VariableType.IsAssignableFrom(valueType))
            {
                return LinqExpression.Block(
                    typeof(object),
                    LinqExpression.Call(
                        CheckAllowAssignmentMethod,
                        _optionsParam,
                        LinqExpression.Constant(BuildAssignmentOperationDescription(assign.Name, TokenType.Equal))),
                    LinqExpression.Assign(promoted.Variable, EmitHelpers.AsObject(Emit(assign.Value))),
                    promoted.Variable);
            }

            var validatedVar = LinqExpression.Variable(typeof(object), "assignValue");
            return LinqExpression.Block(
                typeof(object),
                [validatedVar],
                LinqExpression.Call(
                    CheckAllowAssignmentMethod,
                    _optionsParam,
                    LinqExpression.Constant(BuildAssignmentOperationDescription(assign.Name, TokenType.Equal))),
                LinqExpression.Assign(validatedVar, EmitHelpers.AsObject(Emit(assign.Value))),
                LinqExpression.Assign(
                    validatedVar,
                    LinqExpression.Call(
                        ValidateVariableAssignmentLocalMethod,
                        LinqExpression.Constant(assign.Name),
                        validatedVar,
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)))),
                LinqExpression.Assign(promoted.Variable, validatedVar),
                validatedVar);
        }

        var nonPromotedValue = LinqExpression.Variable(typeof(object), "assignValue");
        return LinqExpression.Block(
            typeof(object),
            [nonPromotedValue],
            LinqExpression.Call(
                CheckAllowAssignmentMethod,
                _optionsParam,
                LinqExpression.Constant(BuildAssignmentOperationDescription(assign.Name, TokenType.Equal))),
            LinqExpression.Assign(nonPromotedValue, EmitHelpers.AsObject(Emit(assign.Value))),
            LinqExpression.Assign(
                nonPromotedValue,
                LinqExpression.Call(
                    ValidateVariableAssignmentMethod,
                    LinqExpression.Constant(assign.Name),
                    nonPromotedValue,
                    _contextParam)),
            LinqExpression.Call(_contextParam, ContextSetMethod, LinqExpression.Constant(assign.Name), nonPromotedValue),
            nonPromotedValue);
    }

    private LinqExpression EmitNullCoalesceAssign(BoundNullCoalesceAssignExpr nullCoalesceAssign)
    {
        if (TryGetPromoted(nullCoalesceAssign.LocalId, out var promoted))
        {
            if (!TypeHelpers.IsNullableType(promoted.VariableType))
            {
                return LinqExpression.Block(
                    typeof(object),
                    LinqExpression.Throw(
                        LinqExpression.Constant(
                            new AlderException(
                                DiagnosticDescriptors.BadBinaryOps,
                                TokenLexemes.GetCanonical(TokenType.QuestionQuestionEqual),
                                promoted.VariableType.Name,
                                promoted.VariableType.Name))),
                    LinqExpression.Constant(null, typeof(object)));
            }

            var assignedVar = LinqExpression.Variable(typeof(object), "coalesceAssigned");
            return LinqExpression.Block(
                typeof(object),
                [assignedVar],
                LinqExpression.Condition(
                    LinqExpression.NotEqual(promoted.Variable, LinqExpression.Constant(null, typeof(object))),
                    promoted.Variable,
                    LinqExpression.Block(
                        LinqExpression.Call(
                            CheckAllowAssignmentMethod,
                            _optionsParam,
                            LinqExpression.Constant(
                                BuildAssignmentOperationDescription(nullCoalesceAssign.Name, TokenType.QuestionQuestionEqual))),
                        LinqExpression.Assign(assignedVar, EmitHelpers.AsObject(Emit(nullCoalesceAssign.Value))),
                        LinqExpression.Assign(promoted.Variable, assignedVar),
                        assignedVar)));
        }

        var currentVar = LinqExpression.Variable(typeof(object), "coalesceCurrent");
        var nonPromotedAssigned = LinqExpression.Variable(typeof(object), "coalesceAssigned");
        return LinqExpression.Block(
            typeof(object),
            [currentVar, nonPromotedAssigned],
            LinqExpression.Call(
                CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(nullCoalesceAssign.Name),
                _contextParam),
            LinqExpression.Assign(
                currentVar,
                LinqExpression.Call(_contextParam, ContextGetMethod, LinqExpression.Constant(nullCoalesceAssign.Name))),
            LinqExpression.Condition(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                currentVar,
                LinqExpression.Block(
                    LinqExpression.Call(
                        CheckAllowAssignmentMethod,
                        _optionsParam,
                        LinqExpression.Constant(
                            BuildAssignmentOperationDescription(nullCoalesceAssign.Name, TokenType.QuestionQuestionEqual))),
                    LinqExpression.Assign(nonPromotedAssigned, EmitHelpers.AsObject(Emit(nullCoalesceAssign.Value))),
                    LinqExpression.Call(
                        _contextParam,
                        ContextSetMethod,
                        LinqExpression.Constant(nullCoalesceAssign.Name),
                        nonPromotedAssigned),
                    nonPromotedAssigned)));
    }

    private LinqExpression EmitCompoundAssign(BoundCompoundAssignExpr compoundAssign)
    {
        if (TryGetPromoted(compoundAssign.LocalId, out var promoted))
        {
            if (TryEmitPureCompoundAssign(compoundAssign, promoted, out var pureResult))
                return pureResult;

            var resultVar = LinqExpression.Variable(typeof(object), "compoundResult");
            return LinqExpression.Block(
                typeof(object),
                [resultVar],
                LinqExpression.Assign(
                    resultVar,
                    LinqExpression.Call(
                        ApplyCompoundAssignLocalMethod,
                        LinqExpression.Constant(compoundAssign.Name),
                        promoted.Variable,
                        LinqExpression.Constant(compoundAssign.Operator),
                        EmitHelpers.AsObject(Emit(compoundAssign.Value)),
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)),
                        _optionsParam,
                        _contextParam,
                        LinqExpression.Constant(_isChecked))),
                LinqExpression.Assign(promoted.Variable, resultVar),
                resultVar);
        }

        return LinqExpression.Call(
            ApplyCompoundAssignMethod,
            LinqExpression.Constant(compoundAssign.Name),
            LinqExpression.Constant(compoundAssign.Operator),
            EmitHelpers.AsObject(Emit(compoundAssign.Value)),
            _contextParam,
            _optionsParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIncrementDecrement(BoundIncrementDecrementExpr incrementDecrement)
    {
        if (TryGetPromoted(incrementDecrement.LocalId, out var promoted))
        {
            if (TryEmitPureIncrementDecrement(incrementDecrement, promoted, out var pureResult))
                return pureResult;

            var isIncrement = incrementDecrement.Operator == TokenType.PlusPlus;
            var oldVar = LinqExpression.Variable(typeof(object), "incrOld");
            var newVar = LinqExpression.Variable(typeof(object), "incrNew");
            return LinqExpression.Block(
                typeof(object),
                [oldVar, newVar],
                LinqExpression.Assign(oldVar, promoted.Variable),
                LinqExpression.Assign(
                    newVar,
                    LinqExpression.Call(
                        ApplyIncrementDecrementLocalMethod,
                        LinqExpression.Constant(incrementDecrement.Name),
                        promoted.Variable,
                        LinqExpression.Constant(isIncrement),
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)),
                        _optionsParam,
                        _contextParam,
                        LinqExpression.Constant(_isChecked))),
                LinqExpression.Assign(promoted.Variable, newVar),
                incrementDecrement.IsPrefix ? newVar : oldVar);
        }

        return LinqExpression.Call(
            ApplyIncrementDecrementMethod,
            LinqExpression.Constant(incrementDecrement.Name),
            LinqExpression.Constant(incrementDecrement.Operator == TokenType.PlusPlus),
            LinqExpression.Constant(incrementDecrement.IsPrefix),
            _contextParam,
            _optionsParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitMemberAssign(BoundMemberAssignExpr memberAssign)
    {
        if (memberAssign.Plan?.Member is PropertyInfo property && property.CanWrite
            && !memberAssign.Target.StaticType.IsValueType)
        {
            return EmitDirectMemberAssignProperty(memberAssign, property);
        }

        if (memberAssign.Plan?.Member is FieldInfo field && !field.IsInitOnly
            && !memberAssign.Target.StaticType.IsValueType)
        {
            return EmitDirectMemberAssignField(memberAssign, field);
        }

        return LinqExpression.Call(
            ApplyMemberAssignMethod,
            EmitHelpers.AsObject(Emit(memberAssign.Target)),
            LinqExpression.Constant(memberAssign.MemberName),
            EmitHelpers.AsObject(Emit(memberAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitDirectMemberAssignProperty(BoundMemberAssignExpr memberAssign, PropertyInfo property)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "maTarget");
        var valueVar = LinqExpression.Variable(typeof(object), "maValue");
        var targetType = property.DeclaringType ?? memberAssign.Plan!.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(memberAssign.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var typedValue = LinqExpression.Convert(valueVar, property.PropertyType);

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, valueVar],
            LinqExpression.Call(CheckAllowPropertySetMethod, _optionsParam, LinqExpression.Constant(memberAssign.MemberName)),
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAssign.Target))),
            LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(memberAssign.Value))),
            LinqExpression.Assign(LinqExpression.Property(typedTarget, property), typedValue),
            valueVar);
    }

    private LinqExpression EmitDirectMemberAssignField(BoundMemberAssignExpr memberAssign, FieldInfo field)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "maTarget");
        var valueVar = LinqExpression.Variable(typeof(object), "maValue");
        var targetType = field.DeclaringType ?? memberAssign.Plan!.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(memberAssign.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var typedValue = LinqExpression.Convert(valueVar, field.FieldType);

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, valueVar],
            LinqExpression.Call(CheckAllowPropertySetMethod, _optionsParam, LinqExpression.Constant(memberAssign.MemberName)),
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberAssign.Target))),
            LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(memberAssign.Value))),
            LinqExpression.Assign(LinqExpression.Field(typedTarget, field), typedValue),
            valueVar);
    }

    private LinqExpression EmitIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        if (indexAssign.Plan is { IsDirectCollectionAccess: true } plan
            && typeof(IList).IsAssignableFrom(plan.TargetType)
            && TryEmitDirectIndexAssign(indexAssign, plan, out var result))
        {
            return result;
        }

        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            EmitHelpers.AsObject(Emit(indexAssign.Target)),
            EmitHelpers.AsObject(Emit(indexAssign.Index)),
            EmitHelpers.AsObject(Emit(indexAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private bool TryEmitDirectIndexAssign(BoundIndexAssignExpr indexAssign, Binding.Plans.BoundIndexPlan plan, out LinqExpression result)
    {
        result = null!;
        var valueStaticType = indexAssign.Value.StaticType;

        var targetObjVar = LinqExpression.Variable(typeof(object), "iaTarget");
        var indexObjVar = LinqExpression.Variable(typeof(object), "iaIndex");
        var valueVar = LinqExpression.Variable(typeof(object), "iaValue");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        LinqExpression assignExpr;
        LinqExpression normalizedIndex;

        if (plan.TargetType.IsArray)
        {
            var elementType = plan.TargetType.GetElementType()!;
            // Only use pure path when value type exactly matches element type (avoids unbox cast issues)
            if (valueStaticType != elementType && valueStaticType != typeof(object))
                return false;

            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar);
            normalizedIndex = LinqExpression.Call(NormalizeIndexMethod, rawIndex,
                LinqExpression.ArrayLength(typedTarget),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var accessExpr = LinqExpression.ArrayAccess(typedTarget, normalizedIndex);

            assignExpr = LinqExpression.Assign(accessExpr, LinqExpression.Convert(valueVar, elementType));
        }
        else if (EmitHelpers.TryGetIntIndexer(plan.TargetType, out var indexer) && indexer.CanWrite
                 && EmitHelpers.TryGetCountProperty(plan.TargetType, out var countProp))
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar);
            normalizedIndex = LinqExpression.Call(NormalizeIndexMethod, rawIndex,
                LinqExpression.Property(typedTarget, countProp),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            assignExpr = LinqExpression.Assign(
                LinqExpression.Property(typedTarget, indexer, normalizedIndex),
                LinqExpression.Convert(valueVar, indexer.PropertyType));
        }
        else
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(IList));
            var countExpr = LinqExpression.Property(
                EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            var rawIndex = LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar);
            normalizedIndex = LinqExpression.Call(NormalizeIndexMethod, rawIndex, countExpr,
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            assignExpr = LinqExpression.Assign(
                LinqExpression.Property(typedTarget, IListIndexerProperty, normalizedIndex),
                valueVar);
        }

        result = LinqExpression.Block(
            typeof(object),
            [targetObjVar, indexObjVar, valueVar],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexAssign.Target))),
            LinqExpression.Assign(indexObjVar, EmitHelpers.AsObject(Emit(indexAssign.Index))),
            LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexObjVar),
            LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(indexAssign.Value))),
            assignExpr,
            valueVar);
        return true;
    }

    private LinqExpression EmitMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
        if (!_isChecked
            && memberCompoundAssign.Plan?.Member is PropertyInfo property
            && property.CanWrite && property.CanRead
            && !memberCompoundAssign.Target.StaticType.IsValueType
            && !property.PropertyType.IsEnum
            && property.PropertyType != typeof(object))
        {
            var rhsType = memberCompoundAssign.Value.StaticType;
            if (rhsType != typeof(object))
            {
                var binaryFactory = GetCompoundBinaryFactory(memberCompoundAssign.Operator, property.PropertyType, rhsType);
                if (binaryFactory != null)
                {
                    var direct = TryEmitDirectMemberCompoundAssign(memberCompoundAssign, property, binaryFactory, rhsType);
                    if (direct != null) return direct;
                }
            }
        }

        return LinqExpression.Call(
            ApplyMemberCompoundAssignMethod,
            EmitHelpers.AsObject(Emit(memberCompoundAssign.Target)),
            LinqExpression.Constant(memberCompoundAssign.MemberName),
            LinqExpression.Constant(memberCompoundAssign.Operator),
            EmitHelpers.AsObject(Emit(memberCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression? TryEmitDirectMemberCompoundAssign(
        BoundMemberCompoundAssignExpr memberCompoundAssign,
        PropertyInfo property,
        Func<LinqExpression, LinqExpression, LinqExpression> binaryFactory,
        Type rhsType)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "mcaTarget");
        var targetType = property.DeclaringType ?? memberCompoundAssign.Plan!.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(memberCompoundAssign.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);

        var typedCurrent = LinqExpression.Variable(property.PropertyType, "mcaCurrent");
        var typedRhs = LinqExpression.Variable(rhsType, "mcaRhs");

        LinqExpression rhsOperand = typedRhs;
        if (rhsType != property.PropertyType)
        {
            if (!IsConvertSafe(rhsType, property.PropertyType))
                return null;
            rhsOperand = LinqExpression.Convert(typedRhs, property.PropertyType);
        }

        LinqExpression binaryExpr = binaryFactory(typedCurrent, rhsOperand);
        if (binaryExpr.Type != property.PropertyType)
        {
            if (!IsConvertSafe(binaryExpr.Type, property.PropertyType))
                return null;
            binaryExpr = LinqExpression.Convert(binaryExpr, property.PropertyType);
        }

        var propAccess = LinqExpression.Property(typedTarget, property);

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, typedCurrent, typedRhs],
            LinqExpression.Call(CheckAllowPropertySetMethod, _optionsParam, LinqExpression.Constant(memberCompoundAssign.MemberName)),
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberCompoundAssign.Target))),
            LinqExpression.Assign(typedCurrent, propAccess),
            LinqExpression.Assign(typedRhs, EmitHelpers.UnboxOrCoerce(Emit(memberCompoundAssign.Value), rhsType)),
            LinqExpression.Assign(propAccess, binaryExpr),
            LinqExpression.Convert(propAccess, typeof(object)));
    }

    private LinqExpression EmitIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
        if (!_isChecked
            && indexCompoundAssign.Plan is { IsDirectCollectionAccess: true } plan
            && typeof(IList).IsAssignableFrom(plan.TargetType)
            && plan.ResultType != typeof(object)
            && !plan.ResultType.IsEnum)
        {
            var rhsType = indexCompoundAssign.Value.StaticType;
            if (rhsType != typeof(object))
            {
                var binaryFactory = GetCompoundBinaryFactory(indexCompoundAssign.Operator, plan.ResultType, rhsType);
                if (binaryFactory != null)
                {
                    var direct = TryEmitDirectIndexCompoundAssign(indexCompoundAssign, plan, binaryFactory, rhsType);
                    if (direct != null) return direct;
                }
            }
        }

        return LinqExpression.Call(
            ApplyIndexCompoundAssignMethod,
            EmitHelpers.AsObject(Emit(indexCompoundAssign.Target)),
            EmitHelpers.AsObject(Emit(indexCompoundAssign.Index)),
            LinqExpression.Constant(indexCompoundAssign.Operator),
            EmitHelpers.AsObject(Emit(indexCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression? TryEmitDirectIndexCompoundAssign(
        BoundIndexCompoundAssignExpr indexCompoundAssign,
        Binding.Plans.BoundIndexPlan plan,
        Func<LinqExpression, LinqExpression, LinqExpression> binaryFactory,
        Type rhsType)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "icaTarget");
        var indexObjVar = LinqExpression.Variable(typeof(object), "icaIndex");
        var normalizedIdx = LinqExpression.Variable(typeof(int), "icaNormIdx");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        var typedCurrent = LinqExpression.Variable(plan.ResultType, "icaCurrent");
        var typedRhs = LinqExpression.Variable(rhsType, "icaRhs");

        LinqExpression rhsOperand = typedRhs;
        if (rhsType != plan.ResultType)
        {
            if (!IsConvertSafe(rhsType, plan.ResultType))
                return null;
            rhsOperand = LinqExpression.Convert(typedRhs, plan.ResultType);
        }

        LinqExpression binaryExpr = binaryFactory(typedCurrent, rhsOperand);
        if (binaryExpr.Type != plan.ResultType)
        {
            if (!IsConvertSafe(binaryExpr.Type, plan.ResultType))
                return null;
            binaryExpr = LinqExpression.Convert(binaryExpr, plan.ResultType);
        }

        LinqExpression readExpr;
        LinqExpression writeExpr;
        LinqExpression normalizeExpr;

        if (plan.TargetType.IsArray)
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar),
                LinqExpression.ArrayLength(typedTarget),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var accessExpr = LinqExpression.ArrayAccess(typedTarget, normalizedIdx);
            readExpr = accessExpr;
            writeExpr = LinqExpression.Assign(accessExpr, binaryExpr);
        }
        else if (EmitHelpers.TryGetIntIndexer(plan.TargetType, out var indexer) && indexer.CanWrite
                 && EmitHelpers.TryGetCountProperty(plan.TargetType, out var countProp))
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar),
                LinqExpression.Property(typedTarget, countProp),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var propAccess = LinqExpression.Property(typedTarget, indexer, normalizedIdx);
            readExpr = LinqExpression.Convert(propAccess, plan.ResultType);
            writeExpr = LinqExpression.Assign(propAccess, LinqExpression.Convert(binaryExpr, indexer.PropertyType));
        }
        else
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(IList));
            var countExpr = LinqExpression.Property(
                EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar), countExpr,
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var propAccess = LinqExpression.Property(typedTarget, IListIndexerProperty, normalizedIdx);
            readExpr = LinqExpression.Unbox(propAccess, plan.ResultType);
            writeExpr = LinqExpression.Assign(propAccess, LinqExpression.Convert(binaryExpr, typeof(object)));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, indexObjVar, normalizedIdx, typedCurrent, typedRhs],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexCompoundAssign.Target))),
            LinqExpression.Assign(indexObjVar, EmitHelpers.AsObject(Emit(indexCompoundAssign.Index))),
            LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexObjVar),
            LinqExpression.Assign(normalizedIdx, normalizeExpr),
            LinqExpression.Assign(typedCurrent, readExpr),
            LinqExpression.Assign(typedRhs, EmitHelpers.UnboxOrCoerce(Emit(indexCompoundAssign.Value), rhsType)),
            writeExpr,
            LinqExpression.Convert(binaryExpr, typeof(object)));
    }

    private LinqExpression EmitMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign)
    {
        var targetVar = LinqExpression.Variable(typeof(object), "nca_target");
        var currentVar = LinqExpression.Variable(typeof(object), "nca_current");
        var resultVar = LinqExpression.Variable(typeof(object), "nca_result");
        var memberName = LinqExpression.Constant(memberNullCoalesceAssign.MemberName);

        return LinqExpression.Block(
            typeof(object),
            [targetVar, currentVar, resultVar],
            LinqExpression.Assign(targetVar, LinqExpression.Call(
                EnsureMemberTargetNotNullMethod,
                EmitHelpers.AsObject(Emit(memberNullCoalesceAssign.Target)),
                memberName)),
            LinqExpression.Assign(currentVar, LinqExpression.Call(
                GetMemberMethod, targetVar, memberName, _optionsParam,
                LinqExpression.Constant(false), _contextParam)),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(resultVar, currentVar),
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(memberNullCoalesceAssign.Value))),
                    LinqExpression.Call(SetMemberMethod, targetVar, memberName, resultVar, _optionsParam, _contextParam))),
            resultVar);
    }

    private LinqExpression EmitIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign)
    {
        var targetVar = LinqExpression.Variable(typeof(object), "nca_target");
        var indexVar = LinqExpression.Variable(typeof(object), "nca_index");
        var currentVar = LinqExpression.Variable(typeof(object), "nca_current");
        var resultVar = LinqExpression.Variable(typeof(object), "nca_result");

        return LinqExpression.Block(
            typeof(object),
            [targetVar, indexVar, currentVar, resultVar],
            LinqExpression.Assign(targetVar, LinqExpression.Call(
                EnsureIndexTargetNotNullMethod,
                EmitHelpers.AsObject(Emit(indexNullCoalesceAssign.Target)))),
            LinqExpression.Assign(indexVar, EmitHelpers.AsObject(Emit(indexNullCoalesceAssign.Index))),
            LinqExpression.Assign(currentVar, LinqExpression.Call(
                GetIndexMethod, targetVar, indexVar, _optionsParam, _contextParam)),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(currentVar, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(resultVar, currentVar),
                LinqExpression.Block(
                    LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(indexNullCoalesceAssign.Value))),
                    LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexVar),
                    LinqExpression.Call(SetIndexMethod, targetVar, indexVar, resultVar, _optionsParam, _contextParam))),
            resultVar);
    }

    private LinqExpression EmitMemberIncrement(BoundMemberIncrementExpr memberIncrement)
    {
        if (!_isChecked
            && memberIncrement.Plan?.Member is PropertyInfo property
            && property.CanWrite && property.CanRead
            && !memberIncrement.Target.StaticType.IsValueType
            && IsAddSubtractSafeType(property.PropertyType))
        {
            return EmitDirectMemberIncrement(memberIncrement, property);
        }

        return LinqExpression.Call(
            ApplyMemberIncrementMethod,
            EmitHelpers.AsObject(Emit(memberIncrement.Target)),
            LinqExpression.Constant(memberIncrement.MemberName),
            LinqExpression.Constant(memberIncrement.IsIncrement),
            LinqExpression.Constant(memberIncrement.IsPrefix),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitDirectMemberIncrement(BoundMemberIncrementExpr memberIncrement, PropertyInfo property)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "miTarget");
        var targetType = property.DeclaringType ?? memberIncrement.Plan!.DeclaringType;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(memberIncrement.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var propAccess = LinqExpression.Property(typedTarget, property);

        var oldTyped = LinqExpression.Variable(property.PropertyType, "miOld");
        var one = LinqExpression.Constant(Convert.ChangeType(1, property.PropertyType), property.PropertyType);
        var newValue = memberIncrement.IsIncrement
            ? LinqExpression.Add(oldTyped, one)
            : LinqExpression.Subtract(oldTyped, one);

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, oldTyped],
            LinqExpression.Call(CheckAllowPropertySetMethod, _optionsParam, LinqExpression.Constant(memberIncrement.MemberName)),
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(memberIncrement.Target))),
            LinqExpression.Assign(oldTyped, propAccess),
            LinqExpression.Assign(propAccess, newValue),
            memberIncrement.IsPrefix
                ? LinqExpression.Convert(propAccess, typeof(object))
                : LinqExpression.Convert(oldTyped, typeof(object)));
    }

    private LinqExpression EmitIndexIncrement(BoundIndexIncrementExpr indexIncrement)
    {
        if (!_isChecked
            && indexIncrement.Plan is { IsDirectCollectionAccess: true } plan
            && typeof(IList).IsAssignableFrom(plan.TargetType)
            && IsAddSubtractSafeType(plan.ResultType))
        {
            return EmitDirectIndexIncrement(indexIncrement, plan);
        }

        return LinqExpression.Call(
            ApplyIndexIncrementMethod,
            EmitHelpers.AsObject(Emit(indexIncrement.Target)),
            EmitHelpers.AsObject(Emit(indexIncrement.Index)),
            LinqExpression.Constant(indexIncrement.IsIncrement),
            LinqExpression.Constant(indexIncrement.IsPrefix),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitDirectIndexIncrement(BoundIndexIncrementExpr indexIncrement, Binding.Plans.BoundIndexPlan plan)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "iiTarget");
        var indexObjVar = LinqExpression.Variable(typeof(object), "iiIndex");
        var normalizedIdx = LinqExpression.Variable(typeof(int), "iiNormIdx");
        var checkedTarget = LinqExpression.Call(EnsureIndexTargetNotNullMethod, targetObjVar);

        var oldTyped = LinqExpression.Variable(plan.ResultType, "iiOld");
        var one = LinqExpression.Constant(Convert.ChangeType(1, plan.ResultType), plan.ResultType);
        var newValue = indexIncrement.IsIncrement
            ? LinqExpression.Add(oldTyped, one)
            : LinqExpression.Subtract(oldTyped, one);

        LinqExpression readExpr;
        LinqExpression writeExpr;
        LinqExpression normalizeExpr;

        if (plan.TargetType.IsArray)
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar),
                LinqExpression.ArrayLength(typedTarget),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var accessExpr = LinqExpression.ArrayAccess(typedTarget, normalizedIdx);
            readExpr = accessExpr;
            writeExpr = LinqExpression.Assign(accessExpr, newValue);
        }
        else if (EmitHelpers.TryGetIntIndexer(plan.TargetType, out var indexer) && indexer.CanWrite
                 && EmitHelpers.TryGetCountProperty(plan.TargetType, out var countProp))
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, plan.TargetType);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar),
                LinqExpression.Property(typedTarget, countProp),
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var propAccess = LinqExpression.Property(typedTarget, indexer, normalizedIdx);
            readExpr = LinqExpression.Convert(propAccess, plan.ResultType);
            writeExpr = LinqExpression.Assign(propAccess, LinqExpression.Convert(newValue, indexer.PropertyType));
        }
        else
        {
            var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(IList));
            var countExpr = LinqExpression.Property(
                EmitHelpers.EnsureTypedExpression(checkedTarget, typeof(ICollection)),
                ICollectionCountProperty);
            normalizeExpr = LinqExpression.Call(NormalizeIndexMethod,
                LinqExpression.Call(ConvertToInt32ObjectMethod, indexObjVar), countExpr,
                LinqExpression.Property(_optionsParam, nameof(AlderOptions.LanguageMode)));
            var propAccess = LinqExpression.Property(typedTarget, IListIndexerProperty, normalizedIdx);
            readExpr = LinqExpression.Unbox(propAccess, plan.ResultType);
            writeExpr = LinqExpression.Assign(propAccess, LinqExpression.Convert(newValue, typeof(object)));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar, indexObjVar, normalizedIdx, oldTyped],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(Emit(indexIncrement.Target))),
            LinqExpression.Assign(indexObjVar, EmitHelpers.AsObject(Emit(indexIncrement.Index))),
            LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexObjVar),
            LinqExpression.Assign(normalizedIdx, normalizeExpr),
            LinqExpression.Assign(oldTyped, readExpr),
            writeExpr,
            indexIncrement.IsPrefix
                ? LinqExpression.Convert(newValue, typeof(object))
                : LinqExpression.Convert(oldTyped, typeof(object)));
    }

    private bool TryEmitPureCompoundAssign(
        BoundCompoundAssignExpr compoundAssign,
        PromotedLocal promoted,
        out LinqExpression result)
    {
        result = null!;
        if (_isChecked || promoted.VariableType == typeof(object) || promoted.VariableType.IsEnum)
            return false;

        var rhsType = compoundAssign.Value.StaticType;
        if (rhsType == typeof(object))
            return false;

        var binaryFactory = GetCompoundBinaryFactory(compoundAssign.Operator, promoted.VariableType, rhsType);
        if (binaryFactory == null)
            return false;

        var typedLocal = LinqExpression.Variable(promoted.VariableType, "cmpTyped");
        var typedRhs = LinqExpression.Variable(rhsType, "cmpRhs");

        LinqExpression rhsOperand = typedRhs;
        if (rhsType != promoted.VariableType)
        {
            if (!IsConvertSafe(rhsType, promoted.VariableType))
                return false;
            rhsOperand = LinqExpression.Convert(typedRhs, promoted.VariableType);
        }

        LinqExpression binaryExpr = binaryFactory(typedLocal, rhsOperand);

        if (binaryExpr.Type != promoted.VariableType)
        {
            if (!IsConvertSafe(binaryExpr.Type, promoted.VariableType))
                return false;
            binaryExpr = LinqExpression.Convert(binaryExpr, promoted.VariableType);
        }

        result = LinqExpression.Block(
            typeof(object),
            [typedLocal, typedRhs],
            LinqExpression.Call(
                CheckAllowAssignmentMethod,
                _optionsParam,
                LinqExpression.Constant(BuildAssignmentOperationDescription(compoundAssign.Name, compoundAssign.Operator))),
            LinqExpression.Assign(typedLocal, LinqExpression.Unbox(promoted.Variable, promoted.VariableType)),
            LinqExpression.Assign(typedRhs, EmitHelpers.UnboxOrCoerce(Emit(compoundAssign.Value), rhsType)),
            LinqExpression.Assign(promoted.Variable, LinqExpression.Convert(binaryExpr, typeof(object))),
            promoted.Variable);
        return true;
    }

    private bool TryEmitPureIncrementDecrement(
        BoundIncrementDecrementExpr incrementDecrement,
        PromotedLocal promoted,
        out LinqExpression result)
    {
        result = null!;
        if (_isChecked || promoted.VariableType == typeof(object) || promoted.VariableType.IsEnum)
            return false;

        if (!IsAddSubtractSafeType(promoted.VariableType))
            return false;

        var isIncrement = incrementDecrement.Operator == TokenType.PlusPlus;
        var typedLocal = LinqExpression.Variable(promoted.VariableType, "incrTyped");
        var one = LinqExpression.Constant(Convert.ChangeType(1, promoted.VariableType), promoted.VariableType);
        var newValue = isIncrement
            ? LinqExpression.Add(typedLocal, one)
            : LinqExpression.Subtract(typedLocal, one);

        var checkAssign = LinqExpression.Call(
            CheckAllowAssignmentMethod,
            _optionsParam,
            LinqExpression.Constant(
                (isIncrement ? "++" : "--") + incrementDecrement.Name));

        if (incrementDecrement.IsPrefix)
        {
            result = LinqExpression.Block(
                typeof(object),
                [typedLocal],
                checkAssign,
                LinqExpression.Assign(typedLocal, LinqExpression.Unbox(promoted.Variable, promoted.VariableType)),
                LinqExpression.Assign(promoted.Variable, LinqExpression.Convert(newValue, typeof(object))),
                promoted.Variable);
        }
        else
        {
            var oldVar = LinqExpression.Variable(typeof(object), "incrOld");
            result = LinqExpression.Block(
                typeof(object),
                [typedLocal, oldVar],
                checkAssign,
                LinqExpression.Assign(typedLocal, LinqExpression.Unbox(promoted.Variable, promoted.VariableType)),
                LinqExpression.Assign(oldVar, promoted.Variable),
                LinqExpression.Assign(promoted.Variable, LinqExpression.Convert(newValue, typeof(object))),
                oldVar);
        }
        return true;
    }

    private static Func<LinqExpression, LinqExpression, LinqExpression>? GetCompoundBinaryFactory(
        TokenType compoundOp, Type leftType, Type rightType)
    {
        return compoundOp switch
        {
            TokenType.PlusEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Add,
            TokenType.MinusEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Subtract,
            TokenType.StarEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Multiply,
            TokenType.SlashEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Divide,
            TokenType.PercentEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Modulo,
            TokenType.AmpEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.And,
            TokenType.PipeEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.Or,
            TokenType.CaretEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.ExclusiveOr,
            TokenType.LessLessEqual when IsIntegralSafeType(leftType) && rightType == typeof(int)
                => LinqExpression.LeftShift,
            TokenType.GreaterGreaterEqual when IsIntegralSafeType(leftType) && rightType == typeof(int)
                => LinqExpression.RightShift,
            _ => null
        };
    }

    private static bool IsIntegralSafeType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(uint) || t == typeof(ulong);

    private static bool IsArithmeticFastPathType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float)
        || t == typeof(decimal) || t == typeof(uint) || t == typeof(ulong)
        || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);

    private static bool IsAddSubtractSafeType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float)
        || t == typeof(decimal) || t == typeof(uint) || t == typeof(ulong);

    private static string BuildAssignmentOperationDescription(string targetName, TokenType operatorToken) =>
        string.Concat(targetName, " ", TokenLexemes.GetCanonical(operatorToken), " ...");
}
