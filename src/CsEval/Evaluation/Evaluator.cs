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
            TokenType.Tilde => BitwiseNot(right),
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
            TokenType.Amp => BitwiseAnd(left, right),
            TokenType.Pipe => BitwiseOr(left, right),
            TokenType.Caret => BitwiseXor(left, right),
            TokenType.LessLess => LeftShift(left, right),
            TokenType.GreaterGreater => RightShift(left, right),
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

    public object? VisitNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = _context.Get(name);

        if (currentValue != null)
            return currentValue;

        var newValue = Evaluate(expr.Value);
        _context.Set(name, newValue);
        return newValue;
    }

    public object? VisitAssign(AssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var value = Evaluate(expr.Value);
        _context.Set(name, value);
        return value;
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
                    throw new EvalException("Spread operator requires an iterable");
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
                    // Spread from regular object via reflection
                    var type = spreadValue.GetType();
                    foreach (var prop in TypeCache.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.CanRead)
                            result[prop.Name] = prop.GetValue(spreadValue);
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
        throw new EvalException("Spread operator can only be used in array or object literals");
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
            value = ValidateAndCoerceType(expr.DeclaredType.Value, value, expr.Name.Lexeme);

        _context.Define(expr.Name.Lexeme, value);
        return value;
    }

    private static object? ValidateAndCoerceType(Token typeToken, object? value, string varName)
    {
        return typeToken.Type switch
        {
            TokenType.Int => CoerceToInt(value, varName),
            TokenType.Long => CoerceToLong(value, varName),
            TokenType.Double => CoerceToDouble(value, varName),
            TokenType.Float => CoerceToFloat(value, varName),
            TokenType.Decimal => CoerceToDecimal(value, varName),
            TokenType.StringType => CoerceToString(value, varName),
            TokenType.Bool => CoerceToBool(value, varName),
            TokenType.Object => value, // object accepts anything
            TokenType.Sbyte => CoerceToSbyte(value, varName),
            TokenType.Byte => CoerceToByte(value, varName),
            TokenType.Short => CoerceToShort(value, varName),
            TokenType.Ushort => CoerceToUshort(value, varName),
            TokenType.Uint => CoerceToUint(value, varName),
            TokenType.Ulong => CoerceToUlong(value, varName),
            TokenType.Char => CoerceToChar(value, varName),
            _ => throw new EvalException($"Unknown type '{typeToken.Lexeme}'")
        };
    }

    private static int CoerceToInt(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to int variable '{varName}'"),
            int i => i,
            long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to int variable '{varName}'")
        };
    }

    private static long CoerceToLong(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to long variable '{varName}'"),
            long l => l,
            int i => i,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to long variable '{varName}'")
        };
    }

    private static double CoerceToDouble(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to double variable '{varName}'"),
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            ulong ul => ul,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to double variable '{varName}'")
        };
    }

    private static float CoerceToFloat(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to float variable '{varName}'"),
            float f => f,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to float variable '{varName}'")
        };
    }

    private static decimal CoerceToDecimal(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to decimal variable '{varName}'"),
            decimal m => m,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            ulong ul => ul,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to decimal variable '{varName}'")
        };
    }

    private static string CoerceToString(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to string variable '{varName}'"),
            string s => s,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to string variable '{varName}'")
        };
    }

    private static bool CoerceToBool(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to bool variable '{varName}'"),
            bool b => b,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to bool variable '{varName}'")
        };
    }

    private static sbyte CoerceToSbyte(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to sbyte variable '{varName}'"),
            sbyte sb => sb,
            int i when i >= sbyte.MinValue && i <= sbyte.MaxValue => (sbyte)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to sbyte variable '{varName}'")
        };
    }

    private static byte CoerceToByte(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to byte variable '{varName}'"),
            byte b => b,
            int i when i >= byte.MinValue && i <= byte.MaxValue => (byte)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to byte variable '{varName}'")
        };
    }

    private static short CoerceToShort(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to short variable '{varName}'"),
            short s => s,
            sbyte sb => sb,
            byte b => b,
            int i when i >= short.MinValue && i <= short.MaxValue => (short)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to short variable '{varName}'")
        };
    }

    private static ushort CoerceToUshort(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to ushort variable '{varName}'"),
            ushort us => us,
            byte b => b,
            int i when i >= ushort.MinValue && i <= ushort.MaxValue => (ushort)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to ushort variable '{varName}'")
        };
    }

    private static uint CoerceToUint(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to uint variable '{varName}'"),
            uint ui => ui,
            byte b => b,
            ushort us => us,
            int i when i >= 0 => (uint)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to uint variable '{varName}'")
        };
    }

    private static ulong CoerceToUlong(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to ulong variable '{varName}'"),
            ulong ul => ul,
            byte b => b,
            ushort us => us,
            uint ui => ui,
            int i when i >= 0 => (ulong)i,
            long l when l >= 0 => (ulong)l,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to ulong variable '{varName}'")
        };
    }

    private static char CoerceToChar(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to char variable '{varName}'"),
            char c => c,
            string s when s.Length == 1 => s[0],
            int i when i >= char.MinValue && i <= char.MaxValue => (char)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to char variable '{varName}'")
        };
    }

    public object? VisitNew(NewExpr expr)
    {
        return Evaluate(expr.Initializer);
    }

    public object? VisitIfStatement(IfStatementExpr expr)
    {
        var condition = Evaluate(expr.Condition);

        if (IsTruthy(condition))
        {
            foreach (var stmt in expr.ThenStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Evaluate(stmt);
            }
        }
        else if (expr.ElseStatements != null)
        {
            foreach (var stmt in expr.ElseStatements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Evaluate(stmt);
            }
        }

        return null;
    }

    public object? VisitReturn(ReturnExpr expr)
    {
        var value = expr.Value != null ? Evaluate(expr.Value) : null;
        throw new ReturnValue(value);
    }

    public object? VisitBreak(BreakExpr expr)
    {
        throw new BreakException();
    }

    public object? VisitContinue(ContinueExpr expr)
    {
        throw new ContinueException();
    }

    public object? VisitWhile(WhileStatementExpr expr)
    {
        var iterations = 0;
        var maxIterations = _options.MaxIterations;

        while (IsTruthy(Evaluate(expr.Condition)))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++iterations > maxIterations)
                throw new EvalException($"While loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }

        return null;
    }

    public object? VisitFor(ForStatementExpr expr)
    {
        var iterations = 0;
        var maxIterations = _options.MaxIterations;

        // Execute initializer (if present)
        if (expr.Initializer != null)
        {
            Evaluate(expr.Initializer);
        }

        // Loop while condition is true (or forever if no condition)
        while (expr.Condition == null || IsTruthy(Evaluate(expr.Condition)))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++iterations > maxIterations)
                throw new EvalException($"For loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                // Continue skips to increment
            }

            // Execute increment (if present)
            if (expr.Increment != null)
            {
                Evaluate(expr.Increment);
            }
        }

        return null;
    }

    public object? VisitDoWhile(DoWhileStatementExpr expr)
    {
        var iterations = 0;
        var maxIterations = _options.MaxIterations;

        do
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++iterations > maxIterations)
                throw new EvalException($"Do-while loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        } while (IsTruthy(Evaluate(expr.Condition)));

        return null;
    }

    public object? VisitForEach(ForEachStatementExpr expr)
    {
        var iterations = 0;
        var maxIterations = _options.MaxIterations;

        var collection = Evaluate(expr.Collection);

        if (collection is not IEnumerable enumerable)
        {
            throw new EvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");
        }

        foreach (var item in enumerable)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++iterations > maxIterations)
                throw new EvalException($"Foreach loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            // Set the loop variable for this iteration
            _context.Define(expr.VariableName.Lexeme, item);

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }

        return null;
    }
}

internal sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, EvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record ModuleMethodRef(CsEvalEngine.ModuleResolver Resolver, MethodInfo Method);
