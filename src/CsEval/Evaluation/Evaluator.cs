using System.Dynamic;
using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator : IExprVisitor<object?>
{
    private CsEvalContext _context;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly Func<MethodInfo, object?[], object?[]>? _argumentTransformer;

    public Evaluator(CsEvalContext context, Dictionary<string, Func<object?[], object?>> functions,
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

        if (UnaryOperators.TryGetValue(expr.Op.Type, out var op))
            return op(this, right);

        throw new CsEvalException($"Unknown unary operator '{expr.Op.Lexeme}'");
    }

    public object? VisitBinary(BinaryExpr expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        if (BinaryOperators.TryGetValue(expr.Op.Type, out var op))
            return op(this, left, right);

        throw new CsEvalException($"Unknown binary operator '{expr.Op.Lexeme}'");
    }

    public object? VisitLogical(LogicalExpr expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Op.Type == TokenType.PipePipe)
        {
            if (RuntimeHelpers.IsTruthy(left)) return true;
        }
        else
        {
            if (!RuntimeHelpers.IsTruthy(left)) return false;
        }

        return RuntimeHelpers.IsTruthy(Evaluate(expr.Right));
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
            throw new CsEvalException($"Cannot access property '{expr.Name.Lexeme}' on null");

        if (obj is IEnumerable and not string and not IDictionary<string, object?>)
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
            throw new CsEvalException("Cannot index null");

        return GetIndex(obj, index);
    }

    public object? VisitNamedArgument(NamedArgumentExpr expr)
    {
        // Named arguments should only appear within CallExpr and are handled there.
        // If we get here, it means a named argument was used outside a method call.
        throw new CsEvalException("Named arguments can only be used in method calls");
    }

    public object? VisitCall(CallExpr expr)
    {
        // Evaluate arguments, wrapping named arguments in NamedArg
        var args = expr.Arguments.Select(arg =>
        {
            if (arg is NamedArgumentExpr namedArg)
                return (object?)new NamedArg(namedArg.Name.Lexeme, Evaluate(namedArg.Value));
            return Evaluate(arg);
        }).ToArray();

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
            throw new CsEvalException($"Method '{methodRef.MethodName}' invocation failed");
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

        throw new CsEvalException($"Cannot call '{callee?.GetType().Name ?? "null"}' as a function");
    }

    public object? VisitLambda(LambdaExpr expr)
    {
        return new LambdaValue(expr.Parameters.Select(p => p.Lexeme).ToList(), expr.Body, _context);
    }

    public object? VisitConditional(ConditionalExpr expr)
    {
        var condition = Evaluate(expr.Condition);
        return RuntimeHelpers.IsTruthy(condition) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);
    }

    public object? VisitNullCoalesce(NullCoalesceExpr expr)
    {
        var left = Evaluate(expr.Left);
        return left ?? Evaluate(expr.Right);
    }

    public object? VisitNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = _context.Get(name);

        if (currentValue != null)
            return currentValue;

        if (!_options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {name} ??= ...");

        var newValue = Evaluate(expr.Value);
        _context.Set(name, newValue);
        return newValue;
    }

    public object? VisitAssign(AssignExpr expr)
    {
        if (!_options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {expr.Name.Lexeme} = ...");

        var name = expr.Name.Lexeme;
        var value = Evaluate(expr.Value);
        _context.Set(name, value);
        return value;
    }

    public object? VisitIndexAssign(IndexAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);
        var index = Evaluate(expr.Index);
        var value = Evaluate(expr.Value);

        if (obj == null)
            throw new CsEvalException("Cannot assign to index on null");

        SetIndex(obj, index, value);
        return value;
    }

    public object? VisitMemberAssign(MemberAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);
        var value = Evaluate(expr.Value);

        if (obj == null)
            throw new CsEvalException($"Cannot assign to property '{expr.Name.Lexeme}' on null");

        SetMember(obj, expr.Name.Lexeme, value);
        return value;
    }

    public object? VisitCompoundAssign(CompoundAssignExpr expr)
    {
        if (!_options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {expr.Name.Lexeme} {expr.Op.Lexeme} ...");

        var name = expr.Name.Lexeme;
        var currentValue = _context.Get(name);
        var rightValue = Evaluate(expr.Value);

        if (!CompoundToBaseOperator.TryGetValue(expr.Op.Type, out var baseOp))
            throw new CsEvalException($"Unknown compound assignment operator '{expr.Op.Lexeme}'");

        if (!BinaryOperators.TryGetValue(baseOp, out var op))
            throw new CsEvalException($"Unknown base operator for '{expr.Op.Lexeme}'");

        var result = op(this, currentValue, rightValue);
        _context.Set(name, result);
        return result;
    }

    public object? VisitIncrementDecrement(IncrementDecrementExpr expr)
    {
        if (!_options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {expr.Op.Lexeme}{expr.Name.Lexeme}");

        var name = expr.Name.Lexeme;
        var currentValue = _context.Get(name);

        // Calculate new value (increment or decrement by 1)
        // Use the appropriate type to preserve type in arithmetic operations
        object one = currentValue switch
        {
            int => 1,
            long => 1L,
            double => 1.0,
            float => 1.0f,
            decimal => 1m,
            short => 1,  // promotes to int in arithmetic
            byte => 1,   // promotes to int in arithmetic
            sbyte => 1,  // promotes to int in arithmetic
            ushort => 1, // promotes to int in arithmetic
            uint => 1u,
            ulong => 1ul,
            _ => 1
        };

        var newValue = expr.Op.Type == TokenType.PlusPlus
            ? RuntimeHelpers.Add(currentValue, one, _options, _context)
            : RuntimeHelpers.Subtract(currentValue, one, _options);

        _context.Set(name, newValue);

        // Prefix returns new value, postfix returns old value
        return expr.IsPrefix ? newValue : currentValue;
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
        var result = new List<object?>();
        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = Evaluate(spread.Expression);
                if (spreadValue is IEnumerable enumerable and not string)
                {
                    foreach (var item in enumerable)
                        result.Add(item);
                }
                else
                {
                    throw new CsEvalException("Spread operator requires an iterable");
                }
            }
            else
            {
                result.Add(Evaluate(element));
            }
        }
        return result;
    }

    public object? VisitObjectLiteral(ObjectLiteralExpr expr)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDotDot && value is SpreadExpr spread)
            {
                // Spread object properties
                var spreadValue = Evaluate(spread.Expression);
                if (spreadValue is IDictionary<string, object?> dict)
                {
                    foreach (var kvp in dict)
                        result[kvp.Key] = kvp.Value;
                }
                else if (spreadValue != null)
                {
                    // Spread from regular object via compiled getters
                    var type = spreadValue.GetType();
                    foreach (var prop in _context.TypeCache.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.CanRead)
                            result[prop.Name] = _context.TypeCache.GetPropertyValue(prop, spreadValue);
                    }
                }
            }
            else
            {
                result[key.Lexeme] = Evaluate(value);
            }
        }
        return result;
    }

    public object? VisitSpread(SpreadExpr expr)
    {
        // Spread expressions are handled by VisitArrayLiteral and VisitObjectLiteral
        // This method is only called if spread is used outside of those contexts
        throw new CsEvalException("Spread operator can only be used in array or object literals");
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
        catch (ReturnValue rv)
        {
            return rv.Value;
        }
        finally
        {
            _context = previousContext;
        }
    }

    public object? VisitVariableDecl(VariableDeclExpr expr)
    {
        var value = Evaluate(expr.Initializer);

        // Validate type if declared (strict mode)
        if (expr.DeclaredType != null)
            value = RuntimeHelpers.ValidateAndCoerceType(expr.DeclaredType.Value.Lexeme, value, expr.Name.Lexeme);

        _context.Define(expr.Name.Lexeme, value);
        return value;
    }

    public object? VisitNew(NewExpr expr)
    {
        return Evaluate(expr.Initializer);
    }

    public object? VisitIfStatement(IfStatementExpr expr)
    {
        var condition = Evaluate(expr.Condition);

        if (RuntimeHelpers.IsTruthy(condition))
        {
            // Create a child scope for the then branch
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.ThenStatements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            finally
            {
                _context = previousContext;
            }
        }
        else if (expr.ElseStatements != null)
        {
            // Create a child scope for the else branch
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.ElseStatements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            finally
            {
                _context = previousContext;
            }
        }

        return null;
    }

    public object? VisitReturn(ReturnExpr expr)
    {
        var value = expr.Value != null ? Evaluate(expr.Value) : null;
        throw new ReturnValue(value);
    }

}

internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, CsEvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record ModuleMethodRef(CsEvalEngine.ModuleResolver Resolver, MethodInfo Method);

/// <summary>
/// Wrapper for a named argument value. Used to pass parameter name information
/// through the method invocation stack.
/// </summary>
internal sealed record NamedArg(string Name, object? Value);
