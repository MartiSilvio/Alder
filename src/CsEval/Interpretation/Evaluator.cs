using System.Collections.Frozen;
using System.Runtime.ExceptionServices;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Interpretation.Extensions;

namespace CsEval.Interpretation;

public sealed class Evaluator : IExprVisitor<object?>
{
    private CsEvalContext _context;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly Func<MethodInfo, object?[], object?[]>? _argumentTransformer;
    private readonly TypeInferrer _typeInferrer;

    private long _iterationCount;
    private readonly Stack<Exception> _caughtExceptions = new();

    public Evaluator(
        CsEvalContext context,
        CsEvalOptions? options = null,
        CancellationToken cancellationToken = default,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer = null)
    {
        _context = context;
        _options = options ?? CsEvalOptions.Default;
        _argumentTransformer = argumentTransformer;
        _typeInferrer = new TypeInferrer(context);
        _cancellationToken = cancellationToken;
    }

    private FrozenDictionary<string, Func<object?[], object?>> Functions => _context.Functions;

    public object? Evaluate(Expr expr)
    {
        _typeInferrer.InferAll(expr);
        _cancellationToken.ThrowIfCancellationRequested();
        return expr.Accept(this);
    }

    #region Expression Visitors

    public object? VisitLiteral(LiteralExpr expr) => expr.Value;

    public object? VisitUnary(UnaryExpr expr)
    {
        var right = Evaluate(expr.Right);

        if (UnaryOperators.TryGetValue(expr.Op.Type, out var op))
            return op(this, right);

        throw new CsEvalException($"Unknown unary operator '{expr.Op.Lexeme}'");
    }

    public object? VisitCast(CastExpr expr)
    {
        var value = Evaluate(expr.Expression);
        var sourceStaticType = _typeInferrer.Infer(expr.Expression);
        var targetType = _context.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        return TypeHelpers.ExplicitCast(value, targetType, sourceStaticType);
    }

    public object? VisitIsPattern(IsPatternExpr expr)
    {
        var value = Evaluate(expr.Expression);
        return MatchPattern(value, expr.Pattern);
    }

    public object? VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        var value = Evaluate(expr.Expression);

        foreach (var arm in expr.Arms)
        {
            // ECMA-334 §12.8.21: Each arm is evaluated in its own scope.
            // Pattern variables are scoped to the arm and must not leak between arms.
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                if ((bool)MatchPattern(value, arm.Pattern)!)
                {
                    // Check when guard if present
                    if (arm.WhenGuard != null)
                    {
                        var guardResult = Evaluate(arm.WhenGuard);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    return Evaluate(arm.Value);
                }
            }
            finally
            {
                _context = previousContext;
            }
        }

        // ECMA-334 §12.8.21: If no pattern matches, throw SwitchExpressionException
        throw new System.Runtime.CompilerServices.SwitchExpressionException(value);
    }

    /// <summary>
    /// Evaluates whether a value matches a pattern, performing variable bindings as needed.
    /// ECMA-334 §11.2 - Pattern matching
    /// </summary>
    private object? MatchPattern(object? value, Pattern pattern)
    {
        switch (pattern)
        {
            case ConstantPattern cp:
            {
                var constantValue = Evaluate(cp.Value);
                return (bool)Operators.Equals(value, constantValue);
            }

            case TypePattern tp:
            {
                var targetType = _context.TypeResolver.ResolveType(tp.TypeToken.Lexeme);
                var isMatch = TypeHelpers.IsType(value, targetType);
                if (isMatch && tp.VariableName != null)
                {
                    _context.DefineNew(tp.VariableName.Value.Lexeme, value, targetType);
                }
                return isMatch;
            }

            case VarPattern vp:
            {
                // var pattern always matches (ECMA-334 §11.2.4)
                var runtimeType = value?.GetType() ?? typeof(object);
                _context.DefineNew(vp.VariableName.Lexeme, value, runtimeType);
                return true;
            }

            case DiscardPattern:
                return true;

            case NotPattern np:
                return !(bool)MatchPattern(value, np.Operand)!;

            case AndPattern ap:
                return (bool)MatchPattern(value, ap.Left)! && (bool)MatchPattern(value, ap.Right)!;

            case OrPattern op:
                return (bool)MatchPattern(value, op.Left)! || (bool)MatchPattern(value, op.Right)!;

            case ParenthesizedPattern pp:
                return MatchPattern(value, pp.Inner);

            case RelationalPattern rp:
            {
                var operand = Evaluate(rp.Operand);
                return rp.Operator.Type switch
                {
                    TokenType.Less => (bool)Operators.LessThan(value, operand, _options),
                    TokenType.LessEqual => (bool)Operators.LessThanOrEqual(value, operand, _options),
                    TokenType.Greater => (bool)Operators.GreaterThan(value, operand, _options),
                    TokenType.GreaterEqual => (bool)Operators.GreaterThanOrEqual(value, operand, _options),
                    _ => throw new CsEvalException($"Unknown relational pattern operator '{rp.Operator.Lexeme}'")
                };
            }

            case PropertyPattern pp:
            {
                // Type check first if type specified
                if (pp.TypeToken != null)
                {
                    var propTargetType = _context.TypeResolver.ResolveType(pp.TypeToken.Value.Lexeme);
                    if (!TypeHelpers.IsType(value, propTargetType))
                        return false;
                }

                if (value == null) return false;

                // Check each property sub-pattern
                foreach (var (name, subPattern) in pp.Properties)
                {
                    var propValue = GetMember(value, name.Lexeme);
                    if (!(bool)MatchPattern(propValue, subPattern)!)
                        return false;
                }

                // Bind variable if present
                if (pp.VariableName != null)
                {
                    var runtimeType = value.GetType();
                    _context.DefineNew(pp.VariableName.Value.Lexeme, value, runtimeType);
                }

                return true;
            }

            default:
                throw new CsEvalException($"Pattern type '{pattern.GetType().Name}' not yet implemented");
        }
    }

    public object? VisitAs(AsExpr expr)
    {
        var value = Evaluate(expr.Expression);
        var targetType = _context.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        return TypeHelpers.TryAs(value, targetType);
    }

    public object? VisitBinary(BinaryExpr expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        // ECMA-334 §10.2.11: Implicit constant expression conversions.
        // A constant int literal that fits in uint/ulong can be promoted directly,
        // avoiding the standard Rule 6 (uint + int -> long) promotion.
        if (left != null && right != null &&
            TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
        {
            bool leftIsConstant = expr.Left is LiteralExpr { IsConstant: true };
            bool rightIsConstant = expr.Right is LiteralExpr { IsConstant: true };

            if (leftIsConstant || rightIsConstant)
            {
                var promoted = NumericDispatch.TryConstantPromotion(
                    left, leftIsConstant, right, rightIsConstant);
                if (promoted != null)
                {
                    left = promoted.Value.Left;
                    right = promoted.Value.Right;
                }
            }
        }

        if (BinaryOperators.TryGetValue(expr.Op.Type, out var op))
            return op(this, left, right);

        throw new CsEvalException($"Unknown binary operator '{expr.Op.Lexeme}'");
    }

    public object? VisitLogical(LogicalExpr expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Op.Type == TokenType.PipePipe)
        {
            if (TypeHelpers.RequireBoolean(left)) return true;
        }
        else
        {
            if (!TypeHelpers.RequireBoolean(left)) return false;
        }

        return TypeHelpers.RequireBoolean(Evaluate(expr.Right));
    }

    public object? VisitGrouping(GroupingExpr expr) => Evaluate(expr.Expression);

    public object? VisitIdentifier(IdentifierExpr expr)
    {
        var name = expr.Name.Lexeme;

        if (Functions.TryGetValue(name, out var func))
            return new FunctionRef(name, func);

        if (_context.Modules.TryGetValue(name, out var module))
            return module;

        return _context.Get(name);
    }

    public object? VisitMemberAccess(MemberAccessExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (expr.NullSafe && obj == null)
            return null;

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{expr.Name.Lexeme}' on null");

        return GetMember(obj, expr.Name.Lexeme);
    }

    public object? VisitTypeReference(TypeReferenceExpr expr)
    {
        // Return the actual Type object for static member access
        return _context.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
    }

    public object? VisitIndexAccess(IndexAccessExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (expr.NullSafe && obj == null)
            return null;

        if (obj == null)
            throw new CsEvalException("Cannot index null");

        var index = Evaluate(expr.Index);
        return GetIndex(obj, index);
    }

    public object? VisitNamedArgument(NamedArgumentExpr expr)
    {
        throw new CsEvalException("Named arguments can only be used in method calls");
    }

    public object? VisitCall(CallExpr expr)
    {
        var args = expr.Arguments.Select(arg =>
        {
            if (arg is NamedArgumentExpr namedArg)
                return (object?)new NamedArg(namedArg.Name.Lexeme, Evaluate(namedArg.Value));
            return Evaluate(arg);
        }).ToArray();

        if (expr.Callee is MemberAccessExpr memberAccess)
        {
            var target = Evaluate(memberAccess.Object);
            return Runtime.MethodInvoker.InvokeMemberCall(
                target, memberAccess.Name.Lexeme, args, memberAccess.NullSafe,
                _context, _options, _cancellationToken, _argumentTransformer, expr.TypeArguments);
        }

        var callee = Evaluate(expr.Callee);
        return Runtime.MethodInvoker.InvokeCall(callee, args, _context, _options, _cancellationToken, _argumentTransformer, expr.TypeArguments);
    }

    public object? VisitLambda(LambdaExpr expr)
    {
        return new LambdaValue(expr.Parameters.Select(p => p.Name.Lexeme).ToList(), expr.Body, _context);
    }

    public object? VisitConditional(ConditionalExpr expr)
    {
        var condition = Evaluate(expr.Condition);
        var result = TypeHelpers.RequireBoolean(condition) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);

        // ECMA-334 §12.18: For numeric types, determine common type and promote result
        // Use static type inference to avoid evaluating both branches (pattern matching binds variables)
        var thenType = _typeInferrer.Infer(expr.ThenBranch);
        var elseType = _typeInferrer.Infer(expr.ElseBranch);

        if (result != null && thenType != typeof(object) && elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) && TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var resultType = NumericDispatch.GetResultType(thenType, elseType);
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }

    public object? VisitNullCoalesce(NullCoalesceExpr expr)
    {
        var left = Evaluate(expr.Left);
        return left ?? Evaluate(expr.Right);
    }

    public object? VisitNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;

        if (_context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "??=", varType.Name, varType.Name);

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

        if (_context.TryGetVariableType(name, out var varType) && varType != null && value != null)
        {
            value = TypeHelpers.ValidateAssignment(varType, value, name);
        }

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
        result = RuntimeHelpers.ValidateCompoundAssignment(name, result, rightValue, _context);

        _context.Set(name, result);
        return result;
    }

    public object? VisitMemberCompoundAssign(MemberCompoundAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{expr.MemberName}' on null");

        var currentValue = GetMember(obj, expr.MemberName);
        var rightValue = Evaluate(expr.Value);

        if (!CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new CsEvalException($"Unknown compound assignment operator");

        if (!BinaryOperators.TryGetValue(baseOp, out var op))
            throw new CsEvalException($"Unknown base operator for compound assignment");

        var result = op(this, currentValue, rightValue);
        SetMember(obj, expr.MemberName, result);
        return result;
    }

    public object? VisitIndexCompoundAssign(IndexCompoundAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException("Cannot index null");

        var index = Evaluate(expr.Index);
        var currentValue = GetIndex(obj, index);
        var rightValue = Evaluate(expr.Value);

        if (!CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new CsEvalException($"Unknown compound assignment operator");

        if (!BinaryOperators.TryGetValue(baseOp, out var op))
            throw new CsEvalException($"Unknown base operator for compound assignment");

        var result = op(this, currentValue, rightValue);
        SetIndex(obj, index, result);
        return result;
    }

    public object? VisitIncrementDecrement(IncrementDecrementExpr expr)
    {
        if (!_options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {expr.Op.Lexeme}{expr.Name.Lexeme}");

        var name = expr.Name.Lexeme;
        var currentValue = _context.Get(name);

        object one = currentValue switch
        {
            int => 1,
            long => 1L,
            double => 1.0,
            float => 1.0f,
            decimal => 1m,
            short => 1,
            byte => 1,
            sbyte => 1,
            ushort => 1,
            uint => 1u,
            ulong => 1ul,
            _ => 1
        };

        var newValue = expr.Op.Type == TokenType.PlusPlus
            ? Operators.Add(currentValue, one, _options, _context)
            : Operators.Subtract(currentValue, one);

        _context.Set(name, newValue);

        return expr.IsPrefix ? newValue : currentValue;
    }

    public object? VisitMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{expr.MemberName}' on null");

        var currentValue = GetMember(obj, expr.MemberName);

        if (currentValue != null)
            return currentValue;

        var newValue = Evaluate(expr.Value);
        SetMember(obj, expr.MemberName, newValue);
        return newValue;
    }

    public object? VisitIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException("Cannot index null");

        var index = Evaluate(expr.Index);
        var currentValue = GetIndex(obj, index);

        if (currentValue != null)
            return currentValue;

        var newValue = Evaluate(expr.Value);
        SetIndex(obj, index, newValue);
        return newValue;
    }

    public object? VisitMemberIncrement(MemberIncrementExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{expr.MemberName}' on null");

        var currentValue = GetMember(obj, expr.MemberName);

        object one = currentValue switch
        {
            int => 1,
            long => 1L,
            double => 1.0,
            float => 1.0f,
            decimal => 1m,
            short => 1,
            byte => 1,
            sbyte => 1,
            ushort => 1,
            uint => 1u,
            ulong => 1ul,
            _ => 1
        };

        var newValue = expr.IsIncrement
            ? Operators.Add(currentValue, one, _options, _context)
            : Operators.Subtract(currentValue, one);

        SetMember(obj, expr.MemberName, newValue);

        return expr.IsPrefix ? newValue : currentValue;
    }

    public object? VisitIndexIncrement(IndexIncrementExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException("Cannot index null");

        var index = Evaluate(expr.Index);
        var currentValue = GetIndex(obj, index);

        object one = currentValue switch
        {
            int => 1,
            long => 1L,
            double => 1.0,
            float => 1.0f,
            decimal => 1m,
            short => 1,
            byte => 1,
            sbyte => 1,
            ushort => 1,
            uint => 1u,
            ulong => 1ul,
            _ => 1
        };

        var newValue = expr.IsIncrement
            ? Operators.Add(currentValue, one, _options, _context)
            : Operators.Subtract(currentValue, one);

        SetIndex(obj, index, newValue);

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
                    if (exprPart.AlignmentSpecifier != null || exprPart.FormatSpecifier != null)
                    {
                        var formatStr = "{0";
                        if (exprPart.AlignmentSpecifier != null) formatStr += "," + exprPart.AlignmentSpecifier;
                        if (exprPart.FormatSpecifier != null) formatStr += ":" + exprPart.FormatSpecifier;
                        formatStr += "}";
                        sb.Append(string.Format(formatStr, value));
                    }
                    else
                    {
                        sb.Append(value?.ToString() ?? "");
                    }
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
        return SpreadHelpers.CreateTypedArray(result);
    }

    public object? VisitObjectLiteral(ObjectLiteralExpr expr) =>
        ObjectLiteralEvaluator.EvaluateObjectLiteral(expr, Evaluate, _context);

    public object? VisitSpread(SpreadExpr expr)
    {
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
                var result = Evaluate(stmt);
                if (result is ControlFlowSignal signal)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return)
                        return signal.Value;
                    return result; // Propagate break/continue upward
                }
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

        Type? declType = null;
        if (expr.DeclaredType != null)
        {
            declType = _context.TypeResolver.ResolveType(expr.DeclaredType.Value.Lexeme);
            value = TypeHelpers.ValidateAndCoerceType(declType, value, expr.Name.Lexeme);
        }

        var inferredType = declType ?? value?.GetType() ?? typeof(object);

        _context.DefineNew(expr.Name.Lexeme, value, inferredType);
        return value;
    }

    public object? VisitNew(NewExpr expr)
    {
        return Evaluate(expr.Initializer);
    }

    public object? VisitDefault(DefaultExpr expr)
    {
        if (expr.TypeToken == null)
            return null;

        var type = _context.TypeResolver.ResolveType(expr.TypeToken.Value.Lexeme);
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public object? VisitNameof(NameofExpr expr) => expr.Name;

    public object? VisitTypeof(TypeofExpr expr)
    {
        return _context.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
    }

    public object? VisitSizeof(SizeofExpr expr)
    {
        return expr.TypeName switch
        {
            "bool" or "Boolean" or "System.Boolean" => 1,
            "byte" or "Byte" or "System.Byte" => 1,
            "sbyte" or "SByte" or "System.SByte" => 1,
            "char" or "Char" or "System.Char" => 2,
            "short" or "Int16" or "System.Int16" => 2,
            "ushort" or "UInt16" or "System.UInt16" => 2,
            "int" or "Int32" or "System.Int32" => 4,
            "uint" or "UInt32" or "System.UInt32" => 4,
            "float" or "Single" or "System.Single" => 4,
            "long" or "Int64" or "System.Int64" => 8,
            "ulong" or "UInt64" or "System.UInt64" => 8,
            "double" or "Double" or "System.Double" => 8,
            "decimal" or "Decimal" or "System.Decimal" => 16,
            _ => throw new CsEvalException($"Cannot take the sizeof of type '{expr.TypeName}'")
        };
    }

    public object? VisitObjectCreation(ObjectCreationExpr expr)
    {
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToArray();
        var type = _context.TypeResolver.ResolveType(expr.TypeName);
        var result = RuntimeHelpers.InvokeConstructor(type, args);

        // Apply object/collection initializer if present
        if (expr.Initializer != null)
        {
            foreach (var entry in expr.Initializer.Entries)
            {
                var value = Evaluate(entry.Value);
                if (entry.PropertyName != null)
                {
                    // Property initializer: set property on newly created object
                    SetMember(result!, entry.PropertyName, value);
                }
                else
                {
                    // Collection initializer: call Add method
                    var addMethod = result!.GetType().GetMethod("Add");
                    if (addMethod != null)
                    {
                        addMethod.Invoke(result, new[] { value });
                    }
                    else
                    {
                        throw new CsEvalException($"Type '{result.GetType().Name}' does not have an 'Add' method for collection initializer");
                    }
                }
            }
        }

        return result;
    }

    public object? VisitMultiDimIndexAccess(MultiDimIndexAccessExpr expr)
    {
        var obj = Evaluate(expr.Object);
        if (expr.NullSafe && obj == null) return null;
        var indices = expr.Indices.Select(i => Convert.ToInt32(Evaluate(i))).ToArray();
        if (obj is Array arr)
            return arr.GetValue(indices);
        throw new CsEvalException($"Multi-dimensional index access not supported on type '{obj?.GetType().Name}'");
    }

    public object? VisitMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr)
    {
        var sizes = expr.Sizes.Select(s => Convert.ToInt32(Evaluate(s))).ToArray();
        var elementType = _context.TypeResolver.ResolveType(expr.ElementTypeName);
        return Array.CreateInstance(elementType, sizes);
    }

    public object? VisitMultiDimIndexAssign(MultiDimIndexAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);
        var indices = expr.Indices.Select(i => Convert.ToInt32(Evaluate(i))).ToArray();
        var value = Evaluate(expr.Value);
        if (obj is Array arr)
        {
            arr.SetValue(value, indices);
            return value;
        }
        throw new CsEvalException($"Multi-dimensional index assignment not supported on type '{obj?.GetType().Name}'");
    }

    public object? VisitTypedArrayCreation(TypedArrayCreationExpr expr)
    {
        var sizeValue = Evaluate(expr.Size);
        var size = Convert.ToInt32(sizeValue);
        var elementType = _context.TypeResolver.ResolveType(expr.ElementTypeName);
        return Array.CreateInstance(elementType, size);
    }

    public object? VisitTypedArrayLiteral(TypedArrayLiteralExpr expr)
    {
        var elementType = _context.TypeResolver.ResolveType(expr.ElementTypeName);
        var elements = expr.Elements.Elements;
        var array = Array.CreateInstance(elementType, elements.Count);
        for (var i = 0; i < elements.Count; i++)
        {
            var value = Evaluate(elements[i]);
            array.SetValue(value, i);
        }
        return array;
    }

    public object? VisitThrow(ThrowExpr expr)
    {
        var result = Evaluate(expr.Expression);
        if (result is not Exception ex)
            throw new CsEvalException($"Cannot throw object of type '{result?.GetType().Name ?? "null"}': throw expression must throw an Exception type");
        throw ex;
    }

    public object? VisitTuple(TupleExpr expr)
    {
        var values = new object?[expr.Elements.Count];
        for (var i = 0; i < expr.Elements.Count; i++)
            values[i] = Evaluate(expr.Elements[i].Expression);
        return RuntimeHelpers.CreateTuple(values);
    }

    public object? VisitDeconstruction(DeconstructionExpr expr)
    {
        var value = Evaluate(expr.ValueExpression);
        if (value is not System.Runtime.CompilerServices.ITuple tuple)
            throw new CsEvalException($"Cannot deconstruct non-tuple value of type '{value?.GetType().Name ?? "null"}'");
        if (tuple.Length != expr.VariableNames.Count)
            throw new CsEvalException($"Deconstruction requires {expr.VariableNames.Count} values but tuple has {tuple.Length} elements");
        for (var i = 0; i < expr.VariableNames.Count; i++)
        {
            var elementValue = tuple[i];
            var elementType = elementValue?.GetType() ?? typeof(object);
            _context.DefineNew(expr.VariableNames[i], elementValue, elementType);
        }
        return value;
    }

    public object? VisitIfStatement(IfStatementExpr expr)
    {
        var condition = Evaluate(expr.Condition);

        if (TypeHelpers.RequireBoolean(condition))
        {
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.ThenStatements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var result = Evaluate(stmt);
                    if (result is ControlFlowSignal)
                        return result;
                }
            }
            finally
            {
                _context = previousContext;
            }
        }
        else if (expr.ElseStatements != null)
        {
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.ElseStatements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var result = Evaluate(stmt);
                    if (result is ControlFlowSignal)
                        return result;
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
        return ControlFlowSignal.Return(value);
    }

    #endregion

    #region Exception Handling

    public object? VisitTryCatchFinally(TryCatchFinallyExpr expr)
    {
        object? result = null;
        Exception? unhandledException = null;
        ControlFlowSignal? pendingSignal = null;

        try
        {
            foreach (var stmt in expr.TryBody)
            {
                result = Evaluate(stmt);
                if (result is ControlFlowSignal signal)
                {
                    pendingSignal = signal;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            var (handled, catchResult, catchSignal) = TryMatchCatchClause(expr.CatchClauses, ex);
            if (handled)
            {
                result = catchResult;
                pendingSignal = catchSignal;
            }
            else
            {
                unhandledException = ex;
            }
        }
        finally
        {
            if (expr.FinallyBody != null)
            {
                foreach (var stmt in expr.FinallyBody)
                {
                    Evaluate(stmt);
                }
            }
        }

        if (unhandledException != null)
            ExceptionDispatchInfo.Capture(unhandledException).Throw();

        if (pendingSignal != null)
            return pendingSignal;

        return result;
    }

    private (bool Handled, object? Result, ControlFlowSignal? Signal) TryMatchCatchClause(
        List<CatchClause> catchClauses, Exception ex)
    {
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.ExceptionTypeName != null)
            {
                var catchType = _context.TypeResolver.ResolveType(catchClause.ExceptionTypeName);
                if (!catchType.IsInstanceOfType(ex))
                    continue;
            }

            var previousContext = _context;
            _context = _context.CreateChild();
            try
            {
                if (catchClause.VariableName != null)
                    _context.DefineNew(catchClause.VariableName.Value.Lexeme, ex, ex.GetType());

                if (catchClause.WhenGuard != null)
                {
                    var guardResult = Evaluate(catchClause.WhenGuard);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                _caughtExceptions.Push(ex);
                try
                {
                    object? result = null;
                    ControlFlowSignal? signal = null;
                    foreach (var stmt in catchClause.Body)
                    {
                        result = Evaluate(stmt);
                        if (result is ControlFlowSignal sig)
                        {
                            signal = sig;
                            break;
                        }
                    }
                    return (true, result, signal);
                }
                finally
                {
                    _caughtExceptions.Pop();
                }
            }
            finally
            {
                _context = previousContext;
            }
        }
        return (false, null, null);
    }

    public object? VisitThrowStatement(ThrowStatementExpr expr)
    {
        if (_caughtExceptions.Count == 0)
            throw new CsEvalException(DiagnosticDescriptors.ThrowOutsideCatch);

        ExceptionDispatchInfo.Capture(_caughtExceptions.Peek()).Throw();
        return null; // Unreachable
    }

    #endregion

    #region Loops

    public object? VisitBreak(BreakExpr expr)
    {
        return ControlFlowSignal.Break;
    }

    public object? VisitContinue(ContinueExpr expr)
    {
        return ControlFlowSignal.Continue;
    }

    public object? VisitWhile(WhileStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            var previousContext = _context;
            _context = _context.CreateChild();
            ControlFlowSignal? signal = null;

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var result = Evaluate(stmt);
                    if (result is ControlFlowSignal s)
                    {
                        signal = s;
                        break; // break out of statement loop
                    }
                }
            }
            finally
            {
                _context = previousContext;
            }

            if (signal != null)
            {
                if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                return signal; // Return signal propagates upward
            }
        }

        return null;
    }

    public object? VisitFor(ForStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;
        var loopContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var init in expr.Initializers)
            {
                Evaluate(init);
            }

            while (expr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (maxIterations > 0 && ++_iterationCount > maxIterations)
                    throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

                var iterationContext = _context;
                _context = _context.CreateChild();
                ControlFlowSignal? signal = null;

                try
                {
                    foreach (var stmt in expr.Body)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        var result = Evaluate(stmt);
                        if (result is ControlFlowSignal s)
                        {
                            signal = s;
                            break; // break out of statement loop
                        }
                    }
                }
                finally
                {
                    _context = iterationContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return) return signal;
                    // Continue: fall through to increment
                }

                foreach (var inc in expr.Increments)
                {
                    Evaluate(inc);
                }
            }
        }
        finally
        {
            _context = loopContext;
        }

        return null;
    }

    public object? VisitDoWhile(DoWhileStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        do
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            var previousContext = _context;
            _context = _context.CreateChild();
            ControlFlowSignal? signal = null;

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var result = Evaluate(stmt);
                    if (result is ControlFlowSignal s)
                    {
                        signal = s;
                        break; // break out of statement loop
                    }
                }
            }
            finally
            {
                _context = previousContext;
            }

            if (signal != null)
            {
                if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                return signal; // Return signal propagates upward
            }
        } while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)));

        return null;
    }

    public object? VisitForEach(ForEachStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;
        var collection = Evaluate(expr.Collection);

        if (collection is not IEnumerable enumerable)
        {
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, collection?.GetType().Name ?? "null");
        }

        foreach (var item in enumerable)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            var previousContext = _context;
            _context = _context.CreateChild();
            ControlFlowSignal? signal = null;

            try
            {
                _context.DefineNew(expr.VariableName.Lexeme, item, typeof(object));

                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var result = Evaluate(stmt);
                    if (result is ControlFlowSignal s)
                    {
                        signal = s;
                        break; // break out of statement loop
                    }
                }
            }
            finally
            {
                _context = previousContext;
            }

            if (signal != null)
            {
                if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                return signal; // Return signal propagates upward
            }
        }

        return null;
    }

    #endregion

    #region Resource Management & Synchronization

    public object? VisitUsingStatement(UsingStatementExpr expr)
    {
        var resource = Evaluate(expr.ResourceDeclaration);
        try
        {
            return Evaluate(expr.Body);
        }
        finally
        {
            if (resource is IDisposable d)
                d.Dispose();
            else if (resource is IAsyncDisposable asyncD)
                asyncD.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public object? VisitLockStatement(LockStatementExpr expr)
    {
        var lockObj = Evaluate(expr.LockObject);
        if (lockObj == null)
            throw new CsEvalException("lock statement requires a non-null reference");
        lock (lockObj)
        {
            return Evaluate(expr.Body);
        }
    }

    #endregion

    #region Switch

    public object? VisitSwitch(SwitchStatementExpr expr)
    {
        var switchValue = Evaluate(expr.Expression);
        var matched = false;
        var defaultCaseIndex = -1;

        for (var i = 0; i < expr.Cases.Count; i++)
        {
            var switchCase = expr.Cases[i];

            if (switchCase.CasePattern == null)
            {
                defaultCaseIndex = i;
                continue;
            }

            if (!matched)
            {
                // Create child scope for pattern variable bindings
                var previousContext = _context;
                _context = _context.CreateChild();
                try
                {
                    if ((bool)MatchPattern(switchValue, switchCase.CasePattern)!)
                    {
                        // Check when guard if present
                        if (switchCase.WhenGuard != null)
                        {
                            var guardResult = Evaluate(switchCase.WhenGuard);
                            if (!TypeHelpers.RequireBoolean(guardResult))
                            {
                                _context = previousContext;
                                continue;
                            }
                        }

                        matched = true;
                        var signal = ExecuteCaseStatements(expr.Cases, i);
                        if (signal != null)
                            return signal.SignalKind == ControlFlowSignal.Kind.Break ? null : signal;
                    }
                }
                finally
                {
                    _context = previousContext;
                }
            }
        }

        if (!matched && defaultCaseIndex >= 0)
        {
            var signal = ExecuteCaseStatements(expr.Cases, defaultCaseIndex);
            if (signal != null && signal.SignalKind != ControlFlowSignal.Kind.Break)
                return signal;
        }

        return null;
    }

    private ControlFlowSignal? ExecuteCaseStatements(List<SwitchCaseExpr> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];

            if (switchCase.Statements.Count == 0)
                continue;

            foreach (var stmt in switchCase.Statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = Evaluate(stmt);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new CsEvalException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    #endregion

    #region Member Access Helpers


    private object? GetMember(object obj, string name)
    {
        if (obj is ModuleInfo module)
        {
            if (module.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(module, _context.ServiceProvider, m),
                    PropertyInfo p => TypeHelpers.GuardReflectionLeak(
                        _context.TypeCache.GetPropertyValue(p, p.GetMethod?.IsStatic == true ? null : module.Resolve(_context.ServiceProvider)),
                        $"property {name}"),
                    FieldInfo f => TypeHelpers.GuardReflectionLeak(
                        f.GetValue(f.IsStatic ? null : module.Resolve(_context.ServiceProvider)),
                        $"field {name}"),
                    _ => throw new CsEvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new CsEvalException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
        }

        // Handle static member access on Type objects (e.g., double.NaN)
        if (obj is Type staticType)
        {
            var staticBindingFlags = BindingFlags.Public | BindingFlags.Static;
            if (!_options.IsCaseSensitive)
                staticBindingFlags |= BindingFlags.IgnoreCase;

            var staticProp = staticType.GetProperty(name, staticBindingFlags);
            if (staticProp != null)
                return TypeHelpers.GuardReflectionLeak(staticProp.GetValue(null), $"static property {name}");

            var staticField = staticType.GetField(name, staticBindingFlags);
            if (staticField != null)
                return TypeHelpers.GuardReflectionLeak(staticField.GetValue(null), $"static field {name}");

            // Check if this is a static method before falling through to instance members
            var staticMethods = staticType.GetMethods(staticBindingFlags);
            if (staticMethods.Any(m => string.Equals(m.Name, name, _options.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)))
                return new StaticMethodRef(staticType, name);

            // Fall through to instance member access on the Type object itself
            // (e.g., typeof(int).Name accesses instance property Type.Name)
        }

        if (!_options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        var caseInsensitive = !_options.IsCaseSensitive;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return TypeHelpers.GuardReflectionLeak(value, $"property {name}");

            if (caseInsensitive)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return TypeHelpers.GuardReflectionLeak(dict[key], $"property {name}");
                }
            }

            throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, obj.GetType().Name, name);
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = _context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeHelpers.GuardReflectionLeak(_context.TypeCache.GetPropertyValue(prop, obj), $"property {name}");

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return TypeHelpers.GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        return new MethodRef(obj, name);
    }

    private object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return TypeHelpers.GuardReflectionLeak(value, $"index [{strKey}]");
            return null;
        }

        if (obj is string str && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= str.Length)
                throw new ArgumentOutOfRangeException("index", idx,
                    "Index was out of range. Must be non-negative and less than the size of the collection.");
            return (object)str[idx]; // Returns boxed char
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new ArgumentOutOfRangeException("index", idx, "Index was out of range. Must be non-negative and less than the size of the collection.");
            return TypeHelpers.GuardReflectionLeak(list[idx], $"index [{idx}]");
        }

        var type = obj.GetType();
        var indexer = _context.TypeCache.GetIndexer(type);
        if (indexer != null)
            return TypeHelpers.GuardReflectionLeak(indexer.GetValue(obj, [index]), $"indexer access");

        throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    private void SetIndex(object obj, object? index, object? value)
    {
        if (!_options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");

        switch (obj)
        {
            case IDictionary<string, object?> dict when index is string strKey:
                dict[strKey] = value;
                return;
            case IList list when index != null:
            {
                var idx = Convert.ToInt32(index);
                if (idx < 0 || idx >= list.Count)
                    throw new ArgumentOutOfRangeException("index", idx, "Index was out of range. Must be non-negative and less than the size of the collection.");
                list[idx] = value;
                return;
            }
        }

        var type = obj.GetType();
        var indexer = _context.TypeCache.GetIndexer(type);
        if (indexer != null && indexer.CanWrite)
        {
            indexer.SetValue(obj, value, [index]);
            return;
        }

        throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    private void SetMember(object obj, string name, object? value)
    {
        if (!_options.Sandbox.AllowPropertySet)
            throw new CsEvalException($"Property assignment blocked by sandbox: {name} = ...");

        var caseInsensitive = !_options.IsCaseSensitive;

        if (obj is IDictionary<string, object?> dict)
        {
            if (caseInsensitive)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        dict[key] = value;
                        return;
                    }
                }
            }
            dict[name] = value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = _context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new CsEvalException(DiagnosticDescriptors.ReadonlyAssignment);
            prop.SetValue(obj, value);
            return;
        }

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new CsEvalException(DiagnosticDescriptors.ReadonlyAssignment);
            field.SetValue(obj, value);
            return;
        }

        throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, type.Name, name);
    }

    #endregion

    #region Operator Registries

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?, object?>> BinaryOperators = new()
    {
        { TokenType.Plus, (e, l, r) => Operators.Add(l, r, e._options, e._context) },
        { TokenType.Minus, (_, l, r) => Operators.Subtract(l, r) },
        { TokenType.Star, (_, l, r) => Operators.Multiply(l, r) },
        { TokenType.Slash, (_, l, r) => Operators.Divide(l, r) },
        { TokenType.Percent, (_, l, r) => Operators.Modulo(l, r) },
        { TokenType.EqualEqual, (_, l, r) => Operators.Equals(l, r) },
        { TokenType.BangEqual, (_, l, r) => Operators.NotEquals(l, r) },
        { TokenType.EqualEqualEqual, (_, l, r) => Operators.Equals(l, r) },
        { TokenType.BangEqualEqual, (_, l, r) => Operators.NotEquals(l, r) },
        { TokenType.Less, (e, l, r) => Operators.LessThan(l, r, e._options) },
        { TokenType.LessEqual, (e, l, r) => Operators.LessThanOrEqual(l, r, e._options) },
        { TokenType.Greater, (e, l, r) => Operators.GreaterThan(l, r, e._options) },
        { TokenType.GreaterEqual, (e, l, r) => Operators.GreaterThanOrEqual(l, r, e._options) },
        { TokenType.Amp, (_, l, r) => Operators.BitwiseAnd(l, r) },
        { TokenType.Pipe, (_, l, r) => Operators.BitwiseOr(l, r) },
        { TokenType.Caret, (_, l, r) => Operators.BitwiseXor(l, r) },
        { TokenType.LessLess, (_, l, r) => Operators.LeftShift(l, r) },
        { TokenType.GreaterGreater, (_, l, r) => Operators.RightShift(l, r) },
        { TokenType.GreaterGreaterGreater, (_, l, r) => Operators.UnsignedRightShift(l, r) },
    };

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, (_, v) => Operators.Negate(v) },
        { TokenType.Plus, (_, v) => Operators.UnaryPlus(v) },
        { TokenType.Bang, (_, v) => Operators.LogicalNot(v) },
        { TokenType.Tilde, (_, v) => Operators.BitwiseNot(v) },
    };

    private static readonly Dictionary<TokenType, TokenType> CompoundToBaseOperator = new()
    {
        { TokenType.PlusEqual, TokenType.Plus },
        { TokenType.MinusEqual, TokenType.Minus },
        { TokenType.StarEqual, TokenType.Star },
        { TokenType.SlashEqual, TokenType.Slash },
        { TokenType.PercentEqual, TokenType.Percent },
        { TokenType.AmpEqual, TokenType.Amp },
        { TokenType.PipeEqual, TokenType.Pipe },
        { TokenType.CaretEqual, TokenType.Caret },
        { TokenType.LessLessEqual, TokenType.LessLess },
        { TokenType.GreaterGreaterEqual, TokenType.GreaterGreater },
        { TokenType.GreaterGreaterGreaterEqual, TokenType.GreaterGreaterGreater },
    };

    #endregion
}

/// <summary>
/// Reference to a registered function, used by IL-compiled and interpreted expressions.
/// </summary>
public sealed record FunctionRef(string Name, Func<object?[], object?> Function)
{
    public object? Invoke(object?[] args) => Function(args);
}

internal sealed record LambdaValue(List<string> Parameters, Expr Body, CsEvalContext Closure);

/// <summary>
/// Compiled lambda with IL-compiled body delegate.
/// </summary>
internal sealed record CompiledLambdaValue(
    List<string> Parameters,
    Func<object?[], CsEvalContext, object?> CompiledBody,
    CsEvalContext Closure);

internal sealed record MethodRef(object Target, string MethodName);

internal sealed record StaticMethodRef(Type Type, string MethodName);

internal sealed record ModuleMethodRef(ModuleInfo Module, IServiceProvider? ServiceProvider, MethodInfo Method);

/// <summary>
/// Wrapper for a named argument value. Used to pass parameter name information
/// through the method invocation stack.
/// </summary>
internal sealed record NamedArg(string Name, object? Value);
