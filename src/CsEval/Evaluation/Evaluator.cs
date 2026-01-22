using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text;
using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed class Evaluator : IExprVisitor<object?>
{
    private EvalContext _context;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;

    public Evaluator(EvalContext context, Dictionary<string, Func<object?[], object?>> functions, CsEvalOptions? options = null, CancellationToken cancellationToken = default)
    {
        _context = context;
        _functions = functions;
        _options = options ?? CsEvalOptions.Default;
        _cancellationToken = cancellationToken;
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

        // Check for built-in functions first
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

        // Check if this is a method reference on an enumerable (for later invocation)
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

    private static bool IsEnumerableMethod(string name) => name.ToLowerInvariant() switch
    {
        "where" or "select" or "aggregate" or
            "first" or "firstordefault" or "last" or "lastordefault" or
            "single" or "singleordefault" or
            "any" or "all" or "count" or "sum" or "average" or
            "min" or "max" or "orderby" or "orderbydescending" or
            "thenby" or "thenbydescending" or
            "distinct" or "take" or "skip" or "contains" or "reverse" or
            "tolist" or "toarray" or "cast" or "concat" or
            "groupby" or "selectmany" or "zip" => true,
        _ => false
    };

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

        // Handle method calls on objects first (before evaluating callee)
        // Skip this for ModuleResolver - let the callee evaluation handle member filtering
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

        // Method reference (from enumerable)
        if (callee is MethodRef methodRef)
        {
            var result = TryInvokeMethod(methodRef.Target, methodRef.MethodName, args);
            if (result.Success)
                return result.Value;
            throw new EvalException($"Method '{methodRef.MethodName}' invocation failed");
        }

        // Filtered method reference (from module with method filter)
        if (callee is ModuleMethodRef filteredRef)
        {
            return InvokeModuleMethod(filteredRef, args);
        }

        // Function reference (built-in or user-defined)
        if (callee is FunctionRef funcRef)
            return funcRef.Invoke(args);

        // Delegate
        if (callee is Delegate del)
            return del.DynamicInvoke(args);

        // Lambda
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

    private object? InvokeLambda(LambdaValue lambda, object?[] args)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        var previousContext = _context;
        _context = childContext;
        try
        {
            return Evaluate(lambda.Body);
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? InvokeModuleMethod(ModuleMethodRef methodRef, object?[] args)
    {
        var method = methodRef.Method;
        var target = method.IsStatic ? null : methodRef.Resolver.Resolve();
        var parameters = method.GetParameters();

        var finalArgs = TryAppendCancellationToken(parameters, args);
        finalArgs = PadWithDefaults(parameters, finalArgs);

        var result = method.Invoke(target, finalArgs);
        return UnwrapTask(result);
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            }
            else if (parameters[i].HasDefaultValue)
            {
                result[i] = parameters[i].DefaultValue;
            }
            else
            {
                throw new EvalException($"Missing required argument '{parameters[i].Name}'");
            }
        }

        return result;
    }

    private static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (arg is IConvertible && IsNumericType(underlying))
            return Convert.ChangeType(arg, underlying);

        return arg;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(decimal) || type == typeof(short) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong);

    private object? GetMember(object obj, string name)
    {
        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => p.GetValue(resolver.Resolve()),
                    _ => throw new EvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new EvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return value;

            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return dict[key];
                }
            }

            throw new EvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = type.GetProperty(name, bindingFlags);
        if (prop != null)
            return prop.GetValue(obj);

        var field = type.GetField(name, bindingFlags);
        if (field != null)
            return field.GetValue(obj);

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private static object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return value;
            return null;
        }

        // List/Array with integer index
        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new EvalException($"Index {idx} out of range");
            return list[idx];
        }

        // Try indexer via reflection
        var type = obj.GetType();
        var indexer = type.GetProperty("Item");
        if (indexer != null)
            return indexer.GetValue(obj, [index]);

        throw new EvalException($"Cannot index type '{type.Name}'");
    }

    private (bool Success, object? Value) TryInvokeMethod(object target, string methodName, object?[] args)
    {
        if (target is CsEvalEngine.ModuleResolver)
            return (false, null);

        var type = target.GetType();

        // Special handling for IEnumerable extension methods
        if (target is IEnumerable enumerable && !target.GetType().IsPrimitive && target is not string)
        {
            var result = TryInvokeEnumerableMethod(enumerable, methodName, args);
            if (result.Success)
                return result;
        }

        // Regular method invocation
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args);
            if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
            {
                var result = method.Invoke(target, convertedArgs);
                return (true, UnwrapTask(result));
            }
        }

        return (false, null);
    }

    private object?[] TryAppendCancellationToken(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return args;

        var lastParam = parameters[^1];
        if (lastParam.ParameterType == typeof(CancellationToken) && args.Length == parameters.Length - 1)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[^1] = _cancellationToken;
            return newArgs;
        }

        return args;
    }

    private object? UnwrapTask(object? result)
    {
        if (result is Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();

            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return null;
        }

        return result;
    }

    private (bool Success, object? Value) TryInvokeEnumerableMethod(IEnumerable enumerable, string methodName, object?[] args)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        var list = enumerable.Cast<object?>().ToList();

        switch (methodName.ToLowerInvariant())
        {
            case "where" when args.Length == 1 && args[0] is LambdaValue predicate:
                return (true, list.Where(item => IsTruthy(InvokeLambda(predicate, [item]))).ToList());

            case "select" when args.Length == 1 && args[0] is LambdaValue selector:
                return (true, list.Select(item => InvokeLambda(selector, [item])).ToList());

            case "aggregate" when args.Length == 2 && args[0] is LambdaValue aggregator:
                return (true, list.Aggregate(args[1], (acc, item) => InvokeLambda(aggregator, [acc, item])));

            case "aggregate" when args.Length == 1 && args[0] is LambdaValue reducer:
                return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => InvokeLambda(reducer, [acc, item])));

            case "first":
                if (args.Length == 1 && args[0] is LambdaValue firstPredicate)
                    return (true, list.First(item => IsTruthy(InvokeLambda(firstPredicate, [item]))));
                return (true, list.First());

            case "firstordefault":
                if (args.Length == 1 && args[0] is LambdaValue firstOrDefaultPredicate)
                    return (true, list.FirstOrDefault(item => IsTruthy(InvokeLambda(firstOrDefaultPredicate, [item]))));
                return (true, list.FirstOrDefault());

            case "last":
                if (args.Length == 1 && args[0] is LambdaValue lastPredicate)
                    return (true, list.Last(item => IsTruthy(InvokeLambda(lastPredicate, [item]))));
                return (true, list.Last());

            case "lastordefault":
                if (args.Length == 1 && args[0] is LambdaValue lastOrDefaultPredicate)
                    return (true, list.LastOrDefault(item => IsTruthy(InvokeLambda(lastOrDefaultPredicate, [item]))));
                return (true, list.LastOrDefault());

            case "single":
                if (args.Length == 1 && args[0] is LambdaValue singlePredicate)
                    return (true, list.Single(item => IsTruthy(InvokeLambda(singlePredicate, [item]))));
                return (true, list.Single());

            case "singleordefault":
                if (args.Length == 1 && args[0] is LambdaValue singleOrDefaultPredicate)
                    return (true, list.SingleOrDefault(item => IsTruthy(InvokeLambda(singleOrDefaultPredicate, [item]))));
                return (true, list.SingleOrDefault());

            case "any":
                if (args.Length == 1 && args[0] is LambdaValue anyPredicate)
                    return (true, list.Any(item => IsTruthy(InvokeLambda(anyPredicate, [item]))));
                return (true, list.Any());

            case "all" when args.Length == 1 && args[0] is LambdaValue allPredicate:
                return (true, list.All(item => IsTruthy(InvokeLambda(allPredicate, [item]))));

            case "count":
                if (args.Length == 1 && args[0] is LambdaValue countPredicate)
                    return (true, list.Count(item => IsTruthy(InvokeLambda(countPredicate, [item]))));
                return (true, list.Count);

            case "sum":
                if (args.Length == 1 && args[0] is LambdaValue sumSelector)
                    return (true, list.Sum(item => ToDouble(InvokeLambda(sumSelector, [item]))));
                return (true, list.Sum(item => ToDouble(item)));

            case "average":
                if (args.Length == 1 && args[0] is LambdaValue avgSelector)
                    return (true, list.Average(item => ToDouble(InvokeLambda(avgSelector, [item]))));
                return (true, list.Average(item => ToDouble(item)));

            case "min":
                if (args.Length == 1 && args[0] is LambdaValue minSelector)
                    return (true, list.Min(item => InvokeLambda(minSelector, [item])));
                return (true, list.Min());

            case "max":
                if (args.Length == 1 && args[0] is LambdaValue maxSelector)
                    return (true, list.Max(item => InvokeLambda(maxSelector, [item])));
                return (true, list.Max());

            case "orderby" when args.Length == 1 && args[0] is LambdaValue orderSelector:
                return (true, list.OrderBy(item => InvokeLambda(orderSelector, [item])).ToList());

            case "orderbydescending" when args.Length == 1 && args[0] is LambdaValue orderDescSelector:
                return (true, list.OrderByDescending(item => InvokeLambda(orderDescSelector, [item])).ToList());

            case "distinct":
                return (true, list.Distinct().ToList());

            case "take" when args.Length == 1:
                return (true, list.Take(Convert.ToInt32(args[0])).ToList());

            case "skip" when args.Length == 1:
                return (true, list.Skip(Convert.ToInt32(args[0])).ToList());

            case "contains" when args.Length == 1:
                return (true, list.Contains(args[0]));

            case "reverse":
                var reversed = list.ToList();
                reversed.Reverse();
                return (true, reversed);

            case "tolist":
                return (true, list);

            case "toarray":
                return (true, list.ToArray());
        }

        return (false, null);
    }

    private static bool CanInvokeMethod(ParameterInfo[] parameters, object?[] args, out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];

        if (args.Length > parameters.Length)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                if (args[i] == null)
                {
                    if (parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
                        return false;
                    convertedArgs[i] = null;
                }
                else if (parameters[i].ParameterType.IsAssignableFrom(args[i]!.GetType()))
                {
                    convertedArgs[i] = args[i];
                }
                else
                {
                    try
                    {
                        convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            else if (parameters[i].HasDefaultValue)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is long l) return l != 0;
        if (value is double d) return d != 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private static new bool Equals(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        // Numeric comparison
        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left) == ToDouble(right);

        return false;
    }

    private static int Compare(object? left, object? right)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

        if (IsNumeric(left) && IsNumeric(right))
            return ToDouble(left).CompareTo(ToDouble(right));

        if (left is string ls && right is string rs)
            return string.Compare(ls, rs, StringComparison.Ordinal);

        if (left is IComparable comparable)
            return comparable.CompareTo(right);

        throw new EvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}");
    }

    private static object? Add(object? left, object? right)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) + ToDouble(right);
            return ToLong(left) + ToLong(right);
        }

        throw new EvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Subtract(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) - ToDouble(right);
            return ToLong(left) - ToLong(right);
        }

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Multiply(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if (left is double || right is double)
                return ToDouble(left) * ToDouble(right);
            return ToLong(left) * ToLong(right);
        }

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Divide(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            var r = ToDouble(right);
            if (r == 0) throw new EvalException("Division by zero");
            return ToDouble(left) / r;
        }

        throw new EvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Modulo(object? left, object? right)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            var r = ToLong(right);
            if (r == 0) throw new EvalException("Modulo by zero");
            return ToLong(left) % r;
        }

        throw new EvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    private static object? Negate(object? value)
    {
        if (value is int i) return -i;
        if (value is long l) return -l;
        if (value is double d) return -d;
        throw new EvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    private static bool IsNumeric(object? value) =>
        value is int or long or double or float or decimal or short or byte;

    private static double ToDouble(object? value) => Convert.ToDouble(value);
    private static long ToLong(object? value) => Convert.ToInt64(value);

}

internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, EvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record ModuleMethodRef(CsEvalEngine.ModuleResolver Resolver, MethodInfo Method);