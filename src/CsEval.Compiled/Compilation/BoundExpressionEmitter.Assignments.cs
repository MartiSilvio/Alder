using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using static CsEval.Compiled.Compilation.BoundRuntimeMethodCache;

namespace CsEval.Compiled.Compilation;

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
                    LinqExpression.Constant(variableDecl.Name));
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
            LinqExpression.Constant(variableDecl.IsConst));
    }

    private LinqExpression EmitAssign(BoundAssignExpr assign)
    {
        if (TryGetPromoted(assign.LocalId, out var promoted))
        {
            var valueVar = LinqExpression.Variable(typeof(object), "assignValue");
            return LinqExpression.Block(
                typeof(object),
                [valueVar],
                LinqExpression.Call(
                    CheckAllowAssignmentMethod,
                    _optionsParam,
                    LinqExpression.Constant(BuildAssignmentOperationDescription(assign.Name, TokenType.Equal))),
                LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(assign.Value))),
                LinqExpression.Assign(
                    valueVar,
                    LinqExpression.Call(
                        ValidateVariableAssignmentLocalMethod,
                        LinqExpression.Constant(assign.Name),
                        valueVar,
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)))),
                LinqExpression.Assign(promoted.Variable, valueVar),
                valueVar);
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
                            new CsEvalException(
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
        return LinqExpression.Call(
            ApplyMemberAssignMethod,
            EmitHelpers.AsObject(Emit(memberAssign.Target)),
            LinqExpression.Constant(memberAssign.MemberName),
            EmitHelpers.AsObject(Emit(memberAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            EmitHelpers.AsObject(Emit(indexAssign.Target)),
            EmitHelpers.AsObject(Emit(indexAssign.Index)),
            EmitHelpers.AsObject(Emit(indexAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
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

    private LinqExpression EmitIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
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

    private LinqExpression EmitIndexIncrement(BoundIndexIncrementExpr indexIncrement)
    {
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

    private static string BuildAssignmentOperationDescription(string targetName, TokenType operatorToken) =>
        string.Concat(targetName, " ", TokenLexemes.GetCanonical(operatorToken), " ...");
}
