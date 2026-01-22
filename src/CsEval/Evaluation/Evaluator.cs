using System.Dynamic;
using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator : IExprVisitor<object?>
{
    private EvalContext _context;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly Func<MethodInfo, object?[], object?[]>? _argumentTransformer;

    public Evaluator(EvalContext context, Dictionary<string, Func<object?[], object?>> functions,
        CsEvalOptions? options = null, CancellationToken cancellationToken = default,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer = null)
    {
        _context = context;
        _functions = functions;
        _options = options ?? CsEvalOptions.Default;
        _cancellationToken = cancellationToken;
        _argumentTransformer = argumentTransformer;
    }

    public object? Evaluate(Expr expr)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        return expr.Accept(this);
    }

    public object? VisitLiteral(LiteralExpr expr) => expr.Value;

    public object? VisitUnary(UnaryExpr expr)
    {
        var right = Evaluate(expr.Right);

        return expr.Op.Type switch
        {
            TokenType.Minus => Negate(right),
            TokenType.Bang => !IsTruthy(right),
            _ => throw new EvalException($"Unknown unary operator '{expr.Op.Lexeme}'")
        };
    }

    public object? VisitBinary(BinaryExpr expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        return expr.Op.Type switch
        {
            TokenType.Plus => Add(left, right),
            TokenType.Minus => Subtract(left, right),
            TokenType.Star => Multiply(left, right),
            TokenType.Slash => Divide(left, right),
            TokenType.Percent => Modulo(left, right),
            TokenType.EqualEqual => Equals(left, right),
            TokenType.BangEqual => !Equals(left, right),
            TokenType.Less => Compare(left, right) < 0,
            TokenType.LessEqual => Compare(left, right) <= 0,
            TokenType.Greater => Compare(left, right) > 0,
            TokenType.GreaterEqual => Compare(left, right) >= 0,
            _ => throw new EvalException($"Unknown binary operator '{expr.Op.Lexeme}'")
        };
    }

    public object? VisitLogical(LogicalExpr expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Op.Type == TokenType.PipePipe)
        {
            if (IsTruthy(left)) return true;
        }
        else
        {
            if (!IsTruthy(left)) return false;
        }

        return IsTruthy(Evaluate(expr.Right));
    }

    public object? VisitGrouping(GroupingExpr expr) => Evaluate(expr.Expression);

    public object? VisitIdentifier(IdentifierExpr expr)
    {
        var name = expr.Name.Lexeme;

        if (_functions.ContainsKey(name))
            return new FunctionRef(name, _functions[name]);

        return _context.Get(name);
    }

    public object? VisitMemberAccess(MemberAccessExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (expr.NullSafe && obj == null)
            return null;

        if (obj == null)
            throw new EvalException($"Cannot access property '{expr.Name.Lexeme}' on null");

        if (obj is IEnumerable && obj is not string && obj is not IDictionary<string, object?>)
        {
            var methodName = expr.Name.Lexeme.ToLowerInvariant();
            if (IsEnumerableMethod(methodName))
            {
                return new MethodRef(obj, expr.Name.Lexeme);
            }
        }

        return GetMember(obj, expr.Name.Lexeme);
    }

    public object? VisitIndexAccess(IndexAccessExpr expr)
    {
        var obj = Evaluate(expr.Object);
        var index = Evaluate(expr.Index);

        if (obj == null)
            throw new EvalException("Cannot index null");

        return GetIndex(obj, index);
    }

    public object? VisitCall(CallExpr expr)
    {
        var args = expr.Arguments.Select(Evaluate).ToArray();

        if (expr.Callee is MemberAccessExpr memberAccess)
        {
            var target = Evaluate(memberAccess.Object);
            if (target != null && target is not CsEvalEngine.ModuleResolver)
            {
                var result = TryInvokeMethod(target, memberAccess.Name.Lexeme, args);
                if (result.Success)
                    return result.Value;
            }
        }

        var callee = Evaluate(expr.Callee);

        if (callee is MethodRef methodRef)
        {
            var result = TryInvokeMethod(methodRef.Target, methodRef.MethodName, args);
            if (result.Success)
                return result.Value;
            throw new EvalException($"Method '{methodRef.MethodName}' invocation failed");
        }

        if (callee is ModuleMethodRef filteredRef)
        {
            return InvokeModuleMethod(filteredRef, args);
        }

        if (callee is FunctionRef funcRef)
            return funcRef.Invoke(args);

        if (callee is Delegate del)
            return del.DynamicInvoke(args);

        if (callee is LambdaValue lambda)
            return InvokeLambda(lambda, args);

        throw new EvalException($"Cannot call '{callee?.GetType().Name ?? "null"}' as a function");
    }

    public object? VisitLambda(LambdaExpr expr)
    {
        return new LambdaValue(expr.Parameters.Select(p => p.Lexeme).ToList(), expr.Body, _context);
    }

    public object? VisitConditional(ConditionalExpr expr)
    {
        var condition = Evaluate(expr.Condition);
        return IsTruthy(condition) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);
    }

    public object? VisitNullCoalesce(NullCoalesceExpr expr)
    {
        var left = Evaluate(expr.Left);
        return left ?? Evaluate(expr.Right);
    }

    public object? VisitInterpolatedString(InterpolatedStringExpr expr)
    {
        var sb = new StringBuilder();
        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    sb.Append(text.Text);
                    break;
                case ExpressionPart exprPart:
                    var value = Evaluate(exprPart.Expression);
                    sb.Append(value?.ToString() ?? "");
                    break;
            }
        }
        return sb.ToString();
    }

    public object? VisitArrayLiteral(ArrayLiteralExpr expr)
    {
        return expr.Elements.Select(Evaluate).ToList();
    }

    public object? VisitObjectLiteral(ObjectLiteralExpr expr)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var (key, value) in expr.Properties)
        {
            result[key.Lexeme] = Evaluate(value);
        }
        return result;
    }

    public object? VisitBlock(BlockExpr expr)
    {
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var stmt in expr.Statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Evaluate(stmt);
            }

            return expr.ReturnExpr != null ? Evaluate(expr.ReturnExpr) : null;
        }
        finally
        {
            _context = previousContext;
        }
    }

    public object? VisitVariableDecl(VariableDeclExpr expr)
    {
        var value = Evaluate(expr.Initializer);
        _context.Define(expr.Name.Lexeme, value);
        return value;
    }

    public object? VisitNew(NewExpr expr)
    {
        return Evaluate(expr.Initializer);
    }
}

internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, EvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record ModuleMethodRef(CsEvalEngine.ModuleResolver Resolver, MethodInfo Method);
