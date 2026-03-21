using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateAssign(BoundAssignExpr assign)
    {
        var value = Evaluate(assign.Value);
        ExecutionRuntime.CheckAllowAssignment(_options, $"{assign.Name} = ...");
        value = AssignmentRuntime.ValidateVariableAssignment(assign.Name, value, _context);
        _context.Set(assign.Name, value);
        return value;
    }

    private object? EvaluateNullCoalesceAssign(BoundNullCoalesceAssignExpr nullCoalesceAssign)
    {
        var name = nullCoalesceAssign.Name;
        ExecutionRuntime.CheckNullCoalesceAssignAllowed(name, _context);

        var currentValue = _context.Get(name);
        if (currentValue != null)
            return currentValue;

        ExecutionRuntime.CheckAllowAssignment(_options, $"{name} ??= ...");

        var newValue = Evaluate(nullCoalesceAssign.Value);
        _context.Set(name, newValue);
        return newValue;
    }

    private object? EvaluateCompoundAssign(BoundCompoundAssignExpr compoundAssign)
    {
        var rightValue = Evaluate(compoundAssign.Value);
        return AssignmentRuntime.ApplyCompoundAssign(
            compoundAssign.Name,
            compoundAssign.Operator,
            rightValue,
            _context,
            _options,
            _isChecked);
    }

    private object? EvaluateIncrementDecrement(BoundIncrementDecrementExpr incrementDecrement)
    {
        return AssignmentRuntime.ApplyIncrementDecrement(
            incrementDecrement.Name,
            incrementDecrement.Operator == TokenType.PlusPlus,
            incrementDecrement.IsPrefix,
            _context,
            _options,
            _isChecked);
    }

    private object? EvaluateMemberAssign(BoundMemberAssignExpr memberAssign)
    {
        var target = Evaluate(memberAssign.Target);
        var value = Evaluate(memberAssign.Value);

        if (memberAssign.Plan?.Member is PropertyInfo property && property.CanWrite
            && target != null && !target.GetType().IsValueType)
        {
            AssignmentRuntime.CheckAllowPropertySet(_options, memberAssign.MemberName);
            property.SetValue(target, value);
            return value;
        }

        if (memberAssign.Plan?.Member is FieldInfo field && !field.IsInitOnly
            && target != null && !target.GetType().IsValueType)
        {
            AssignmentRuntime.CheckAllowPropertySet(_options, memberAssign.MemberName);
            field.SetValue(target, value);
            return value;
        }

        return AssignmentRuntime.ApplyMemberAssign(target, memberAssign.MemberName, value, _options, _context);
    }

    private object? EvaluateIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        var target = Evaluate(indexAssign.Target);
        var index = Evaluate(indexAssign.Index);
        var value = Evaluate(indexAssign.Value);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, _options, _context);
    }

    private object? EvaluateMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
        var target = Evaluate(memberCompoundAssign.Target);
        var rightValue = Evaluate(memberCompoundAssign.Value);
        return AssignmentRuntime.ApplyMemberCompoundAssign(
            target,
            memberCompoundAssign.MemberName,
            memberCompoundAssign.Operator,
            rightValue,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
        var target = Evaluate(indexCompoundAssign.Target);
        var index = Evaluate(indexCompoundAssign.Index);
        var rightValue = Evaluate(indexCompoundAssign.Value);
        return AssignmentRuntime.ApplyIndexCompoundAssign(
            target,
            index,
            indexCompoundAssign.Operator,
            rightValue,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign)
    {
        var target = Evaluate(memberNullCoalesceAssign.Target);
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, memberNullCoalesceAssign.MemberName);
        var currentValue = MemberAccess.GetMember(target, memberNullCoalesceAssign.MemberName, _options, nullSafe: false, _context);
        if (currentValue != null)
            return currentValue;
        var newValue = Evaluate(memberNullCoalesceAssign.Value);
        MemberAccess.SetMember(target, memberNullCoalesceAssign.MemberName, newValue, _options, _context);
        return newValue;
    }

    private object? EvaluateIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign)
    {
        var target = Evaluate(indexNullCoalesceAssign.Target);
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var index = Evaluate(indexNullCoalesceAssign.Index);
        var currentValue = MemberAccess.GetIndex(target, index, _options, _context);
        if (currentValue != null)
            return currentValue;
        var newValue = Evaluate(indexNullCoalesceAssign.Value);
        AssignmentRuntime.CheckAllowIndexSet(_options, index);
        MemberAccess.SetIndex(target, index, newValue, _options, _context);
        return newValue;
    }

    private object? EvaluateMemberIncrement(BoundMemberIncrementExpr memberIncrement)
    {
        var target = Evaluate(memberIncrement.Target);
        return AssignmentRuntime.ApplyMemberIncrement(
            target,
            memberIncrement.MemberName,
            memberIncrement.IsIncrement,
            memberIncrement.IsPrefix,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateIndexIncrement(BoundIndexIncrementExpr indexIncrement)
    {
        var target = Evaluate(indexIncrement.Target);
        var index = Evaluate(indexIncrement.Index);
        return AssignmentRuntime.ApplyIndexIncrement(
            target,
            index,
            indexIncrement.IsIncrement,
            indexIncrement.IsPrefix,
            _options,
            _context,
            _isChecked);
    }

    private object? EvaluateDeconstruction(BoundDeconstructionExpr deconstruction)
    {
        var value = Evaluate(deconstruction.ValueExpression);

        if (value is System.Runtime.CompilerServices.ITuple tuple)
        {
            if (tuple.Length != deconstruction.VariableNames.Length)
            {
                throw new AlderException(
                    DiagnosticDescriptors.DeconstructionCountMismatch, deconstruction.VariableNames.Length, tuple.Length);
            }

            for (var i = 0; i < deconstruction.VariableNames.Length; i++)
            {
                var elementValue = tuple[i];
                var elementType = elementValue?.GetType() ?? typeof(object);
                _context.DefineNew(deconstruction.VariableNames[i], elementValue, elementType);
            }

            return value;
        }

        if (value != null)
        {
            var deconstructed = ConstructionRuntime.TryDeconstruct(value, deconstruction.VariableNames.Length);
            if (deconstructed != null)
            {
                for (var i = 0; i < deconstruction.VariableNames.Length; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    _context.DefineNew(deconstruction.VariableNames[i], elementValue, elementType);
                }

                return value;
            }
        }

        throw new AlderException(DiagnosticDescriptors.DeconstructionFailed, value?.GetType().Name ?? "null");
    }
}
