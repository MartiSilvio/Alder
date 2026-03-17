using CsEval.Binding.BoundNodes;
using CsEval.Compiled.Compilation.BoundEmission;
using CsEval.Parsing;
using static CsEval.Compiled.Compilation.BoundRuntimeMethodCache;

namespace CsEval.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitVariableDecl(BoundVariableDeclExpr variableDecl)
    {
        return LinqExpression.Call(
            DefineVariableMethod,
            LinqExpression.Constant(variableDecl.Name),
            BoundEmitterSupport.AsObject(Emit(variableDecl.Initializer)),
            variableDecl.DeclaredType != null
                ? LinqExpression.Constant(variableDecl.DeclaredType, typeof(Type))
                : LinqExpression.Constant(null, typeof(Type)),
            _contextParam,
            LinqExpression.Constant(variableDecl.IsConst));
    }

    private LinqExpression EmitAssign(BoundAssignExpr assign)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "assignValue");
        return LinqExpression.Block(
            typeof(object),
            [valueVar],
            LinqExpression.Call(
                CheckAllowAssignmentMethod,
                _optionsParam,
                LinqExpression.Constant(BuildAssignmentOperationDescription(assign.Name, TokenType.Equal))),
            LinqExpression.Assign(valueVar, BoundEmitterSupport.AsObject(Emit(assign.Value))),
            LinqExpression.Assign(
                valueVar,
                LinqExpression.Call(
                    ValidateVariableAssignmentMethod,
                    LinqExpression.Constant(assign.Name),
                    valueVar,
                    _contextParam)),
            LinqExpression.Call(_contextParam, ContextSetMethod, LinqExpression.Constant(assign.Name), valueVar),
            valueVar);
    }

    private LinqExpression EmitNullCoalesceAssign(BoundNullCoalesceAssignExpr nullCoalesceAssign)
    {
        var currentVar = LinqExpression.Variable(typeof(object), "coalesceCurrent");
        var assignedVar = LinqExpression.Variable(typeof(object), "coalesceAssigned");
        return LinqExpression.Block(
            typeof(object),
            [currentVar, assignedVar],
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
                    LinqExpression.Assign(assignedVar, BoundEmitterSupport.AsObject(Emit(nullCoalesceAssign.Value))),
                    LinqExpression.Call(
                        _contextParam,
                        ContextSetMethod,
                        LinqExpression.Constant(nullCoalesceAssign.Name),
                        assignedVar),
                    assignedVar)));
    }

    private LinqExpression EmitCompoundAssign(BoundCompoundAssignExpr compoundAssign)
    {
        return LinqExpression.Call(
            ApplyCompoundAssignMethod,
            LinqExpression.Constant(compoundAssign.Name),
            LinqExpression.Constant(compoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(compoundAssign.Value)),
            _contextParam,
            _optionsParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIncrementDecrement(BoundIncrementDecrementExpr incrementDecrement)
    {
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
            BoundEmitterSupport.AsObject(Emit(memberAssign.Target)),
            LinqExpression.Constant(memberAssign.MemberName),
            BoundEmitterSupport.AsObject(Emit(memberAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitIndexAssign(BoundIndexAssignExpr indexAssign)
    {
        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexAssign.Index)),
            BoundEmitterSupport.AsObject(Emit(indexAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitMemberCompoundAssign(BoundMemberCompoundAssignExpr memberCompoundAssign)
    {
        return LinqExpression.Call(
            ApplyMemberCompoundAssignMethod,
            BoundEmitterSupport.AsObject(Emit(memberCompoundAssign.Target)),
            LinqExpression.Constant(memberCompoundAssign.MemberName),
            LinqExpression.Constant(memberCompoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(memberCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitIndexCompoundAssign(BoundIndexCompoundAssignExpr indexCompoundAssign)
    {
        return LinqExpression.Call(
            ApplyIndexCompoundAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Index)),
            LinqExpression.Constant(indexCompoundAssign.Operator),
            BoundEmitterSupport.AsObject(Emit(indexCompoundAssign.Value)),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private LinqExpression EmitMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr memberNullCoalesceAssign)
    {
        return LinqExpression.Call(
            ApplyMemberNullCoalesceAssignMethod,
            BoundEmitterSupport.AsObject(Emit(memberNullCoalesceAssign.Target)),
            LinqExpression.Constant(memberNullCoalesceAssign.MemberName),
            BoundEmitterSupport.AsObject(Emit(memberNullCoalesceAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr indexNullCoalesceAssign)
    {
        return LinqExpression.Call(
            ApplyIndexNullCoalesceAssignMethod,
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Target)),
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Index)),
            BoundEmitterSupport.AsObject(Emit(indexNullCoalesceAssign.Value)),
            _optionsParam,
            _contextParam);
    }

    private LinqExpression EmitMemberIncrement(BoundMemberIncrementExpr memberIncrement)
    {
        return LinqExpression.Call(
            ApplyMemberIncrementMethod,
            BoundEmitterSupport.AsObject(Emit(memberIncrement.Target)),
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
            BoundEmitterSupport.AsObject(Emit(indexIncrement.Target)),
            BoundEmitterSupport.AsObject(Emit(indexIncrement.Index)),
            LinqExpression.Constant(indexIncrement.IsIncrement),
            LinqExpression.Constant(indexIncrement.IsPrefix),
            _optionsParam,
            _contextParam,
            LinqExpression.Constant(_isChecked));
    }

    private static string BuildAssignmentOperationDescription(string targetName, TokenType operatorToken) =>
        string.Concat(targetName, " ", TokenLexemes.GetCanonical(operatorToken), " ...");
}
