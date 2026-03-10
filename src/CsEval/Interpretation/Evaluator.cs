using System.Collections.Frozen;
using System.Runtime.ExceptionServices;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;
using CsEval.Interpretation.Extensions;

namespace CsEval.Interpretation;

internal sealed class Evaluator : IExprVisitor<object?>
{
    private CsEvalContext _context;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly TypeInferrer _typeInferrer;

    private int _depth;
    private readonly int _maxDepth;
    private readonly Stack<Exception> _caughtExceptions = new();
    private bool _isChecked;
    private int _breakContextDepth;
    private int _loopDepth;

    public Evaluator(
        CsEvalContext context,
        CsEvalOptions? options = null,
        TypeInferrer? typeInferrer = null,
        CancellationToken cancellationToken = default)
    {
        _context = context;
        _options = options ?? CsEvalOptions.Default;
        _typeInferrer = typeInferrer ?? new TypeInferrer(context, _options.MaxExpressionDepth);
        _cancellationToken = cancellationToken;
        _maxDepth = _options.MaxExpressionDepth;
    }

    private FrozenDictionary<string, Func<object?[], object?>> Functions => _context.Functions;

    public object? Evaluate(Expr expr)
    {
        _depth++;
        if (_depth > _maxDepth)
            throw new CsEvalDepthException("evaluation", _maxDepth);
        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return expr.Accept(this);
        }
        finally
        {
            _depth--;
        }
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
        var targetType = _context.TypeResolver.ResolveType(expr.TargetType.Lexeme);

        // Only enforce unboxing semantics when the source expression is a simple identifier
        // with a known explicit type (e.g., object x = 42). For complex expressions (binary,
        // grouping, index access, etc.), the TypeInferrer defaults to typeof(object) which would
        // incorrectly block valid numeric conversions like (int)dynamicDouble.
        Type? sourceStaticType = null;
        if (expr.Expression is IdentifierExpr)
            sourceStaticType = _typeInferrer.Infer(expr.Expression);
        return TypeHelpers.ExplicitCast(value, targetType, sourceStaticType, _isChecked);
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
                // If the constant expression evaluates to a Type, treat as a type check
                // This handles: x is Exception (bare identifier type pattern without binding)
                // where the parser couldn't disambiguate type vs constant at parse time
                if (constantValue is Type typeValue)
                    return TypeHelpers.IsType(value, typeValue);
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

                if (!BinaryOperators.TryGetValue(rp.Operator.Type, out var op))
                    throw new CsEvalException($"Unknown relational pattern operator '{rp.Operator.Lexeme}'");

                return TypeHelpers.RequireBoolean(op(this, value, operand));
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
        var opLexeme = expr.Op.Lexeme;

        if (left is not bool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                TypeNameFormatter.Of(left),
                GetExpressionTypeName(expr.Right));
        }

        if (expr.Op.Type == TokenType.PipePipe)
        {
            if ((bool)left) return true;
        }
        else
        {
            if (!(bool)left) return false;
        }

        var right = Evaluate(expr.Right);
        if (right is not bool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        }

        return (bool)right;
    }

    private static string GetExpressionTypeName(Expr expr)
    {
        return expr switch
        {
            LiteralExpr { Value: null } => TypeNameFormatter.Null,
            LiteralExpr { Value: { } v } => v.GetType().Name,
            _ => "unknown"
        };
    }

    public object? VisitIdentifier(IdentifierExpr expr)
    {
        var name = expr.Name.Lexeme;

        if (Functions.TryGetValue(name, out var func))
            return new FunctionRef(name, func);

        if (_context.Modules.TryGetValue(name, out var module))
            return module;

        // Try variable lookup first (fast path for common case)
        if (_context.TryGet(name, out var value))
            return value;

        // If not a variable/function/module, check if it's a namespace prefix.
        // This enables FQN type access like System.Linq.Enumerable.Where(...)
        if (_context.TypeResolver.IsNamespaceOrPrefix(name))
            return new NamespaceRef(name);

        // Try resolving as a type name for static member access
        // Enables: Array.Empty<int>(), Math.Max(1, 2), Enumerable.Range(0, 10)
        var resolvedType = _context.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return resolvedType;

        // Bare math constants in Extended mode (pi, e, tau, infinity, nan)
        // User variables shadow these -- checked above via TryGet
        if (_options.LanguageMode == LanguageMode.Extended &&
            BareMathNames.TryGetConstant(name, out var constant))
            return constant;

        // Fall through to context.Get which throws CS0103 with proper error message
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
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(expr.Index);
        return GetIndex(obj, index);
    }

    public object? VisitSlice(SliceExpr expr)
    {
        var target = Evaluate(expr.Target);
        var start = expr.Start != null ? Evaluate(expr.Start) : null;
        var end = expr.End != null ? Evaluate(expr.End) : null;
        var step = expr.Step != null ? Evaluate(expr.Step) : null;
        return MemberAccess.GetSlice(target, start, end, step, _options);
    }

    public object? VisitNamedArgument(NamedArgumentExpr expr)
    {
        throw new CsEvalException("Named arguments can only be used in method calls");
    }

    public object? VisitOutArg(OutArgExpr expr)
    {
        // Returns a marker that VisitCall detects and uses for ByRef parameter handling
        return new OutArgMarker(expr.VariableName, expr.TypeName, expr.IsDiscard);
    }

    public object? VisitCall(CallExpr expr)
    {
        var args = new object?[expr.Arguments.Count];
        List<OutVariableBinding>? outBindings = null;
        for (var i = 0; i < expr.Arguments.Count; i++)
        {
            var argument = expr.Arguments[i];
            if (argument is NamedArgumentExpr namedArg)
            {
                args[i] = new NamedArg(namedArg.Name.Lexeme, Evaluate(namedArg.Value));
                continue;
            }

            if (argument is OutArgExpr outArgExpr)
            {
                if (!outArgExpr.IsDiscard)
                {
                    outBindings ??= [];
                    outBindings.Add(new OutVariableBinding(i, outArgExpr.VariableName, outArgExpr.TypeName));
                }
            }

            var evaluated = Evaluate(argument);
            args[i] = evaluated;
        }

        object? result;

        if (expr.Callee is MemberAccessExpr memberAccess)
        {
            var target = Evaluate(memberAccess.Object);
            result = Runtime.MethodInvoker.InvokeMemberCall(
                target, memberAccess.Name.Lexeme, args, memberAccess.NullSafe,
                _context, _options, _cancellationToken, expr.TypeArguments);
        }
        else if (expr.Callee is IdentifierExpr id)
        {
            result = IdentifierRuntime.InvokeIdentifierCall(
                id.Name.Lexeme,
                args,
                _context,
                _options,
                _cancellationToken,
                expr.TypeArguments);
        }
        else
        {
            var callee = Evaluate(expr.Callee);
            result = Runtime.MethodInvoker.InvokeCall(callee, args, _context, _options, _cancellationToken, expr.TypeArguments);
        }

        // After method invocation, MethodInvoker.CopyBackOutArgs has replaced OutArgMarker
        // entries in the args array with the actual values produced by the method.
        // Define out variables in the current scope.
        if (outBindings is { Count: > 0 })
            IdentifierRuntime.DefineOutVariables(args, outBindings, _context);

        return result;
    }

    public object? VisitLambda(LambdaExpr expr)
    {
        return new LambdaValue(expr.Parameters.Select(p => p.Name.Lexeme).ToList(), expr.Body, _context, _options);
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
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.QuestionQuestionEqual),
                varType.Name,
                varType.Name);

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
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

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
        var op = ResolveCompoundOperator(expr.Op.Type, expr.Op.Lexeme);

        var result = op(this, currentValue, rightValue);
        result = AssignmentRuntime.ValidateCompoundAssignment(name, result, rightValue, _context);

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
        var op = ResolveCompoundOperator(expr.Operator, expr.Operator.ToString());

        var result = op(this, currentValue, rightValue);
        SetMember(obj, expr.MemberName, result);
        return result;
    }

    public object? VisitIndexCompoundAssign(IndexCompoundAssignExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(expr.Index);
        var currentValue = GetIndex(obj, index);
        var rightValue = Evaluate(expr.Value);
        var op = ResolveCompoundOperator(expr.Operator, expr.Operator.ToString());

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
        var one = GetNumericOne(currentValue);

        var newValue = expr.Op.Type == TokenType.PlusPlus
            ? Operators.Add(currentValue, one, _options, _context, _isChecked)
            : Operators.Subtract(currentValue, one, _isChecked);

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
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

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
        var one = GetNumericOne(currentValue);

        var newValue = expr.IsIncrement
            ? Operators.Add(currentValue, one, _options, _context, _isChecked)
            : Operators.Subtract(currentValue, one, _isChecked);

        SetMember(obj, expr.MemberName, newValue);

        return expr.IsPrefix ? newValue : currentValue;
    }

    public object? VisitIndexIncrement(IndexIncrementExpr expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj == null)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = Evaluate(expr.Index);
        var currentValue = GetIndex(obj, index);
        var one = GetNumericOne(currentValue);

        var newValue = expr.IsIncrement
            ? Operators.Add(currentValue, one, _options, _context, _isChecked)
            : Operators.Subtract(currentValue, one, _isChecked);

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
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var stmt in expr.Statements)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
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
        return TypeHelpers.GetSizeOf(expr.TypeName);
    }

    public object? VisitObjectCreation(ObjectCreationExpr expr)
    {
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToArray();
        var type = _context.TypeResolver.ResolveType(expr.TypeName);
        var result = ConstructionRuntime.InvokeConstructor(type, args);

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
        if (obj != null && expr.Indices.Count > 1)
        {
            var hasMatchingIndexer = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.Name == "Item" && p.GetIndexParameters().Length == expr.Indices.Count);
            if (hasMatchingIndexer)
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, obj.GetType().Name);
        }
        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(obj));
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
        if (obj != null && expr.Indices.Count > 1)
        {
            var hasMatchingIndexer = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.Name == "Item" && p.GetIndexParameters().Length == expr.Indices.Count);
            if (hasMatchingIndexer)
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, obj.GetType().Name);
        }
        throw new CsEvalException(
            DiagnosticDescriptors.BadIndexerAccess,
            TypeNameFormatter.Of(obj));
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
        var ex = ExecutionRuntime.ValidateThrowOperand(result);
        throw ex;
    }

    public object? VisitTuple(TupleExpr expr)
    {
        var values = new object?[expr.Elements.Count];
        for (var i = 0; i < expr.Elements.Count; i++)
            values[i] = Evaluate(expr.Elements[i].Expression);
        return ConstructionRuntime.CreateTuple(values);
    }

    public object? VisitDeconstruction(DeconstructionExpr expr)
    {
        var value = Evaluate(expr.ValueExpression);

        // ITuple path (ValueTuple)
        if (value is System.Runtime.CompilerServices.ITuple tuple)
        {
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

        // Deconstruct() method path -- types with public void Deconstruct(out T1, out T2, ...)
        if (value is not null)
        {
            var deconstructed = ConstructionRuntime.TryDeconstruct(value, expr.VariableNames.Count);
            if (deconstructed != null)
            {
                for (var i = 0; i < expr.VariableNames.Count; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    _context.DefineNew(expr.VariableNames[i], elementValue, elementType);
                }
                return value;
            }
        }

        throw new CsEvalException($"Cannot deconstruct value of type '{TypeNameFormatter.Of(value)}': no ITuple implementation or Deconstruct() method found");
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
                    bool guardMatched;
                    try
                    {
                        var guardResult = Evaluate(catchClause.WhenGuard);
                        guardMatched = TypeHelpers.RequireBoolean(guardResult);
                    }
                    catch
                    {
                        // ECMA-334: exceptions thrown while evaluating a catch filter
                        // are treated as filter-false and matching continues.
                        guardMatched = false;
                    }

                    if (!guardMatched)
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
        if (_breakContextDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);
        return ControlFlowSignal.Break;
    }

    public object? VisitContinue(ContinueExpr expr)
    {
        if (_loopDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);
        return ControlFlowSignal.Continue;
    }

    public object? VisitWhile(WhileStatementExpr expr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var previousContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(expr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    public object? VisitFor(ForStatementExpr expr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var loopContext = _context;
        _context = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var init in expr.Initializers)
            {
                Evaluate(init);
            }

            while (expr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var iterationContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(expr.Body);
                }
                finally
                {
                    _context = iterationContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return) return signal;
                }

                foreach (var inc in expr.Increments)
                {
                    Evaluate(inc);
                }
            }
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
            _context = loopContext;
        }

        return null;
    }

    public object? VisitDoWhile(DoWhileStatementExpr expr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var previousContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(expr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            } while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)));

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    public object? VisitForEach(ForEachStatementExpr expr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var collection = Evaluate(expr.Collection);

        if (collection is not IEnumerable enumerable)
        {
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));
        }

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var item in enumerable)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var previousContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    _context.DefineNew(expr.VariableName.Lexeme, item, typeof(object));
                    signal = ExecuteStatementBlock(expr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
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

        _breakContextDepth++;
        try
        {
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
        finally
        {
            _breakContextDepth--;
        }
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
        => MemberAccess.GetMember(obj, name, _options, nullSafe: false, _context);

    private object? GetIndex(object obj, object? index)
        => MemberAccess.GetIndex(obj, index, _options);

    private void SetIndex(object obj, object? index, object? value)
    {
        if (!_options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");
        MemberAccess.SetIndex(obj, index, value, _options);
    }

    private void SetMember(object obj, string name, object? value)
        => MemberAccess.SetMember(obj, name, value, _options, _context);

    #endregion

    #region Shared Helpers

    private static object GetNumericOne(object? value) => value switch
    {
        int => 1, long => 1L, double => 1.0, float => 1.0f, decimal => 1m,
        short => 1, byte => 1, sbyte => 1, ushort => 1, uint => 1u, ulong => 1ul,
        _ => 1
    };

    private Func<Evaluator, object?, object?, object?> ResolveCompoundOperator(TokenType compoundOp, string opLexeme)
    {
        if (!CompoundToBaseOperator.TryGetValue(compoundOp, out var baseOp))
            throw new CsEvalException($"Unknown compound assignment operator '{opLexeme}'");
        if (!BinaryOperators.TryGetValue(baseOp, out var op))
            throw new CsEvalException($"Unknown base operator for '{opLexeme}'");
        return op;
    }

    private ControlFlowSignal? ExecuteStatementBlock(List<Expr> statements)
    {
        foreach (var stmt in statements)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var result = Evaluate(stmt);
            if (result is ControlFlowSignal s)
                return s;
        }
        return null;
    }

    #endregion

    #region Operator Registries

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?, object?>> BinaryOperators = new()
    {
        { TokenType.Plus, (e, l, r) => Operators.Add(l, r, e._options, e._context, e._isChecked) },
        { TokenType.Minus, (e, l, r) => Operators.Subtract(l, r, e._isChecked) },
        { TokenType.Star, (e, l, r) => Operators.Multiply(l, r, e._options, e._isChecked) },
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
        { TokenType.StarStar, (_, l, r) => Operators.Power(l, r) },
        { TokenType.In, (_, l, r) => Operators.InOperator(l, r) },
        { TokenType.Like, (_, l, r) => Operators.Like(l, r) },
        { TokenType.EqualTilde, (_, l, r) => Operators.RegexMatch(l, r) },
        { TokenType.BangTilde, (_, l, r) => Operators.RegexNotMatch(l, r) },
        { TokenType.LessEqualGreater, (_, l, r) => Operators.Spaceship(l, r) },
    };

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, (e, v) => Operators.Negate(v, e._isChecked) },
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
        { TokenType.StarStarEqual, TokenType.StarStar },
    };

    #endregion

    #region Checked/Unchecked

    public object? VisitChecked(CheckedExpr expr)
    {
        var previous = _isChecked;
        _isChecked = expr.IsChecked;
        try
        {
            return Evaluate(expr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    #endregion

    #region Polyglot Extended Features (stubs)

    public object? VisitRange(RangeExpr expr)
    {
        var start = Evaluate(expr.Start);
        var end = Evaluate(expr.End);
        int startInt = Convert.ToInt32(start);
        int endInt = Convert.ToInt32(end);
        return RangeHelpers.GenerateRange(startInt, endInt, expr.ExclusiveEnd);
    }

    public object? VisitPipeline(PipelineExpr expr)
    {
        if (expr.Right is IdentifierExpr rightIdentifier)
        {
            var leftValue = Evaluate(expr.Left);
            return IdentifierRuntime.InvokePipelineIdentifier(
                leftValue,
                rightIdentifier.Name.Lexeme,
                _context,
                _options,
                _cancellationToken);
        }

        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);
        return PipelineOperator.InvokePipeline(left, right, _context, _options, _cancellationToken);
    }

    public object? VisitChainedComparison(ChainedComparisonExpr expr)
    {
        // Evaluate first operand
        var prevValue = Evaluate(expr.Operands[0]);

        for (int i = 0; i < expr.Operators.Count; i++)
        {
            var nextValue = Evaluate(expr.Operands[i + 1]);

            if (!Runtime.Extensions.ChainedComparisonHelper.PerformComparison(
                    prevValue, nextValue, expr.Operators[i].Type, _options))
                return (object)false; // Short-circuit: remaining operands not evaluated

            prevValue = nextValue; // Reuse evaluated value for next comparison
        }

        return (object)true;
    }

    #endregion
}
