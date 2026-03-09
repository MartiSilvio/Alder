using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

internal sealed class BoundEvaluator
{
    private readonly CsEvalContext _context;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;

    public BoundEvaluator(
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken = default)
    {
        _context = context;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public object? Evaluate(BoundExpr expr)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        return expr switch
        {
            BoundLiteralExpr literal => literal.Value,
            BoundIdentifierExpr identifier => _context.Get(identifier.Name),
            BoundBinaryExpr binary => EvaluateBinary(binary),
            BoundMemberAccessExpr memberAccess => EvaluateMemberAccess(memberAccess),
            BoundIndexAccessExpr indexAccess => EvaluateIndexAccess(indexAccess),
            BoundCallExpr call => EvaluateCall(call),
            _ => throw new BindingNotSupportedException(
                $"Bound execution for node '{expr.GetType().Name}' is not implemented")
        };
    }

    private object? EvaluateBinary(BoundBinaryExpr binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        return binary.Operator switch
        {
            TokenType.Plus => Operators.Add(left, right, _options, _context),
            TokenType.Minus => Operators.Subtract(left, right),
            TokenType.Star => Operators.Multiply(left, right, _options),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, _options),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, _options),
            TokenType.Greater => Operators.GreaterThan(left, right, _options),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, _options),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            _ => throw new BindingNotSupportedException(
                $"Bound binary operator '{binary.Operator}' is not implemented")
        };
    }

    private object? EvaluateMemberAccess(BoundMemberAccessExpr memberAccess)
    {
        var target = Evaluate(memberAccess.Target);
        return MemberAccess.GetMember(
            target,
            memberAccess.MemberName,
            _options,
            nullSafe: false,
            _context);
    }

    private object? EvaluateIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        var target = Evaluate(indexAccess.Target);
        var index = Evaluate(indexAccess.Index);
        return MemberAccess.GetIndex(target, index, _options);
    }

    private object? EvaluateCall(BoundCallExpr call)
    {
        var args = new object?[call.Arguments.Length];
        for (var i = 0; i < call.Arguments.Length; i++)
            args[i] = Evaluate(call.Arguments[i]);

        if (call.Callee is BoundMemberAccessExpr memberAccess)
        {
            var target = memberAccess.Plan.IsStatic ? null : Evaluate(memberAccess.Target);
            var result = CsEval.Runtime.MethodInvoker.InvokeMethodWithArgs(
                call.Plan.SelectedMethod,
                target,
                args,
                _cancellationToken);
            if (result.Success)
                return result.Value;
        }

        var callee = Evaluate(call.Callee);
        return CsEval.Runtime.MethodInvoker.InvokeCall(callee, args, _context, _options, _cancellationToken);
    }
}
