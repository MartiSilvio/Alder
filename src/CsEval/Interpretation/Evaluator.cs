using System.Collections.Frozen;
using System.Dynamic;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

public sealed class Evaluator : IExprVisitor<object?>
{
    private CsEvalContext _context;
    private readonly CsEvalOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly Func<MethodInfo, object?[], object?[]>? _argumentTransformer;
    private readonly TypeInferrer _typeInferrer;

    private long _iterationCount;

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
        return TypeHelpers.ExplicitCast(value, expr.TargetType.Lexeme, sourceStaticType);
    }

    public object? VisitIs(IsExpr expr)
    {
        var value = Evaluate(expr.Expression);

        if (expr.TargetType == null)
        {
            var isNull = value == null;
            return expr.IsNegated ? !isNull : isNull;
        }

        // var pattern (x is var y) - always matches (ECMA-334 §11.2.4)
        if (expr.TargetType.Value.Type == TokenType.Var)
        {
            if (expr.VariableName != null)
            {
                var runtimeType = value?.GetType() ?? typeof(object);
                _context.DefineNew(expr.VariableName.Value.Lexeme, value, runtimeType);
            }
            return true;
        }

        var isMatch = TypeHelpers.IsType(value, expr.TargetType.Value.Lexeme);

        if (isMatch && expr.VariableName != null)
        {
            var targetType = TypeHelpers.ResolveTypeName(expr.TargetType.Value.Lexeme);
            _context.DefineNew(expr.VariableName.Value.Lexeme, value, targetType);
        }

        return expr.IsNegated ? !isMatch : isMatch;
    }

    public object? VisitAs(AsExpr expr)
    {
        var value = Evaluate(expr.Expression);
        return TypeHelpers.TryAs(value, expr.TargetType.Lexeme);
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
        return TypeHelpers.ResolveTypeName(expr.TypeToken.Lexeme);
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
        return new LambdaValue(expr.Parameters.Select(p => p.Lexeme).ToList(), expr.Body, _context);
    }

    public object? VisitConditional(ConditionalExpr expr)
    {
        var condition = Evaluate(expr.Condition);
        return TypeHelpers.RequireBoolean(condition) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);
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
            throw new CsEvalException($"Operator '??=' cannot be applied to operand of type '{varType.Name}'");

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
        return RuntimeHelpers.CreateTypedList(result);
    }

    public object? VisitObjectLiteral(ObjectLiteralExpr expr)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDotDot && value is SpreadExpr spread)
            {
                var spreadValue = Evaluate(spread.Expression);
                if (spreadValue is IDictionary<string, object?> dict)
                {
                    foreach (var kvp in dict)
                        result[kvp.Key] = kvp.Value;
                }
                else if (spreadValue != null)
                {
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

        if (expr.DeclaredType != null)
            value = TypeHelpers.ValidateAndCoerceType(expr.DeclaredType.Value.Lexeme, value, expr.Name.Lexeme);

        var inferredType = expr.DeclaredType != null
            ? TypeHelpers.ResolveTypeName(expr.DeclaredType.Value.Lexeme)
            : value?.GetType() ?? typeof(object);

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

        var type = TypeHelpers.ResolveTypeName(expr.TypeToken.Value.Lexeme);
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public object? VisitNameof(NameofExpr expr) => expr.Name;

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

    #endregion

    #region Loops

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
        var maxIterations = _options.MaxIterations;

        while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            var previousContext = _context;
            _context = _context.CreateChild();

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
            finally
            {
                _context = previousContext;
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
            if (expr.Initializer != null)
            {
                Evaluate(expr.Initializer);
            }

            while (expr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (maxIterations > 0 && ++_iterationCount > maxIterations)
                    throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

                var iterationContext = _context;
                _context = _context.CreateChild();

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
                }
                finally
                {
                    _context = iterationContext;
                }

                if (expr.Increment != null)
                {
                    Evaluate(expr.Increment);
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
            finally
            {
                _context = previousContext;
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
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");
        }

        foreach (var item in enumerable)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                _context.DefineNew(expr.VariableName.Lexeme, item, typeof(object));

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
            finally
            {
                _context = previousContext;
            }
        }

        return null;
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

            if (switchCase.Pattern == null)
            {
                defaultCaseIndex = i;
                continue;
            }

            if (!matched)
            {
                var caseValue = Evaluate(switchCase.Pattern);
                if ((bool)Operators.Equals(switchValue, caseValue))
                {
                    matched = true;
                    if (ExecuteCaseStatements(expr.Cases, i))
                        return null;
                }
            }
        }

        if (!matched && defaultCaseIndex >= 0)
        {
            ExecuteCaseStatements(expr.Cases, defaultCaseIndex);
        }

        return null;
    }

    private bool ExecuteCaseStatements(List<SwitchCaseExpr> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];

            if (switchCase.Statements.Count == 0)
                continue;

            try
            {
                foreach (var stmt in switchCase.Statements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                return true;
            }

            throw new CsEvalException("CS0163: Control cannot fall through from one case label to another");
        }

        return false;
    }

    #endregion

    #region Member Access Helpers

    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (TypeHelpers.IsForbiddenReflectionType(type))
        {
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name} ({context})");
        }

        return value;
    }

    private object? GetMember(object obj, string name)
    {
        if (obj is ModuleInfo module)
        {
            if (module.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(module, _context.ServiceProvider, m),
                    PropertyInfo p => GuardReflectionLeak(
                        _context.TypeCache.GetPropertyValue(p, p.GetMethod?.IsStatic == true ? null : module.Resolve(_context.ServiceProvider)),
                        $"property {name}"),
                    FieldInfo f => GuardReflectionLeak(
                        f.GetValue(f.IsStatic ? null : module.Resolve(_context.ServiceProvider)),
                        $"field {name}"),
                    _ => throw new CsEvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new CsEvalException($"Member '{name}' not found on module '{module.Type.Name}'");
        }

        // Handle static member access on Type objects (e.g., double.NaN)
        if (obj is Type staticType)
        {
            var staticBindingFlags = BindingFlags.Public | BindingFlags.Static;
            if (!_options.IsCaseSensitive)
                staticBindingFlags |= BindingFlags.IgnoreCase;

            var staticProp = staticType.GetProperty(name, staticBindingFlags);
            if (staticProp != null)
                return GuardReflectionLeak(staticProp.GetValue(null), $"static property {name}");

            var staticField = staticType.GetField(name, staticBindingFlags);
            if (staticField != null)
                return GuardReflectionLeak(staticField.GetValue(null), $"static field {name}");

            // Return a static method reference for method calls
            return new StaticMethodRef(staticType, name);
        }

        if (!_options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        var caseInsensitive = !_options.IsCaseSensitive;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return GuardReflectionLeak(value, $"property {name}");

            if (caseInsensitive)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return GuardReflectionLeak(dict[key], $"property {name}");
                }
            }

            throw new CsEvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = _context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return GuardReflectionLeak(_context.TypeCache.GetPropertyValue(prop, obj), $"property {name}");

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        return new MethodRef(obj, name);
    }

    private object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return GuardReflectionLeak(value, $"index [{strKey}]");
            return null;
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new ArgumentOutOfRangeException("index", idx, "Index was out of range. Must be non-negative and less than the size of the collection.");
            return GuardReflectionLeak(list[idx], $"index [{idx}]");
        }

        var type = obj.GetType();
        var indexer = _context.TypeCache.GetIndexer(type);
        if (indexer != null)
            return GuardReflectionLeak(indexer.GetValue(obj, [index]), $"indexer access");

        throw new CsEvalException($"Cannot index type '{type.Name}'");
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

        throw new CsEvalException($"Cannot set index on type '{type.Name}'");
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
                throw new CsEvalException($"Property '{name}' is read-only");
            prop.SetValue(obj, value);
            return;
        }

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new CsEvalException($"Field '{name}' is read-only");
            field.SetValue(obj, value);
            return;
        }

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
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
    };

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, (_, v) => Operators.Negate(v) },
        { TokenType.Plus, (_, v) => Operators.UnaryPlus(v) },
        { TokenType.Bang, (_, v) => !TypeHelpers.RequireBoolean(v) },
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
