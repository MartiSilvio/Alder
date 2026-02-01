using CsEval.Extensions;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal sealed partial class ILCompiler
{
    #region Expression Compilation

    private LinqExpression CompileLiteral(LiteralExpr lit)
    {
        if (lit.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        // Box value types to object
        return LinqExpression.Convert(
            LinqExpression.Constant(lit.Value, lit.Value.GetType()),
            typeof(object));
    }

    private LinqExpression CompileIdentifier(IdentifierExpr id)
    {
        return LinqExpression.Call(
            ResolveIdentifierMethod,
            LinqExpression.Constant(id.Name.Lexeme),
            _currentContext,
            _functionsParam);
    }

    private LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        return u.Op.Type switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand),
            TokenType.Bang => LinqExpression.Convert(
                LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, operand)),
                typeof(object)),
            TokenType.Tilde => LinqExpression.Call(BitwiseNotMethod, operand),
            _ => throw new NotSupportedException($"Unary operator {u.Op.Type}")
        };
    }

    private LinqExpression CompileCast(CastExpr cast)
    {
        var value = Compile(cast.Expression);
        return LinqExpression.Call(
            ExplicitCastMethod,
            value,
            LinqExpression.Constant(cast.TargetType.Lexeme));
    }

    private LinqExpression CompileIs(IsExpr isExpr)
    {
        var value = Compile(isExpr.Expression);

        // x is null / x is not null
        if (isExpr.TargetType == null)
        {
            var isNull = LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object)));
            LinqExpression result = isExpr.IsNegated
                ? LinqExpression.Not(isNull)
                : isNull;
            return LinqExpression.Convert(result, typeof(object));
        }

        // x is type / x is type name
        var typeCheck = LinqExpression.Call(
            IsTypeMethod,
            value,
            LinqExpression.Constant(isExpr.TargetType.Value.Lexeme));

        if (isExpr.VariableName == null)
            return LinqExpression.Convert(typeCheck, typeof(object));

        // x is type name - declare variable if match succeeds
        var valueVar = LinqExpression.Variable(typeof(object), "isValue");
        var matchVar = LinqExpression.Variable(typeof(bool), "isMatch");

        return LinqExpression.Block(
            typeof(object),
            [valueVar, matchVar],
            LinqExpression.Assign(valueVar, value),
            LinqExpression.Assign(matchVar, LinqExpression.Call(
                IsTypeMethod,
                valueVar,
                LinqExpression.Constant(isExpr.TargetType.Value.Lexeme))),
            LinqExpression.IfThen(
                matchVar,
                LinqExpression.Call(_currentContext, DefineMethod,
                    LinqExpression.Constant(isExpr.VariableName.Value.Lexeme),
                    valueVar)),
            LinqExpression.Convert(matchVar, typeof(object)));
    }

    private LinqExpression CompileAs(AsExpr asExpr)
    {
        var value = Compile(asExpr.Expression);
        return LinqExpression.Call(
            TryAsMethod,
            value,
            LinqExpression.Constant(asExpr.TargetType.Lexeme));
    }

    private LinqExpression CompileBinary(BinaryExpr b)
    {
        var left = Compile(b.Left);
        var right = Compile(b.Right);

        if (b.Op.Type == TokenType.Plus)
            return LinqExpression.Call(AddMethod, left, right, _optionsParam, _currentContext);

        // Shift operators don't take options
        if (b.Op.Type == TokenType.LessLess)
            return LinqExpression.Call(LeftShiftMethod, left, right);
        if (b.Op.Type == TokenType.GreaterGreater)
            return LinqExpression.Call(RightShiftMethod, left, right);

        // Check extension operators
        if (_extensionILOperators.TryGetValue(b.Op.Type, out var extOp))
            return CompileExtensionBinaryOp(extOp, left, right);

        var method = b.Op.Type switch
        {
            TokenType.Minus => SubtractMethod,
            TokenType.Star => MultiplyMethod,
            TokenType.Slash => DivideMethod,
            TokenType.Percent => ModuloMethod,
            TokenType.EqualEqual or TokenType.EqualEqualEqual => EqualsMethod,
            TokenType.BangEqual or TokenType.BangEqualEqual => NotEqualsMethod,
            TokenType.Less => LessThanMethod,
            TokenType.LessEqual => LessThanOrEqualMethod,
            TokenType.Greater => GreaterThanMethod,
            TokenType.GreaterEqual => GreaterThanOrEqualMethod,
            TokenType.Amp => BitwiseAndMethod,
            TokenType.Pipe => BitwiseOrMethod,
            TokenType.Caret => BitwiseXorMethod,
            _ => throw new NotSupportedException($"Binary operator {b.Op.Type}")
        };

        return LinqExpression.Call(method, left, right, _optionsParam);
    }

    private LinqExpression CompileExtensionBinaryOp(ILBinaryOperator op, LinqExpression left, LinqExpression right)
    {
        var (l, r) = op.SwapOperands ? (right, left) : (left, right);

        LinqExpression call = op.PassOptions
            ? LinqExpression.Call(op.Method, l, r, _optionsParam)
            : LinqExpression.Call(op.Method, l, r);

        return op.BoxResult
            ? LinqExpression.Convert(call, typeof(object))
            : call;
    }

    private LinqExpression CompileLogical(LogicalExpr l)
    {
        var left = Compile(l.Left);
        var right = Compile(l.Right);

        var leftTruthy = LinqExpression.Call(RequireBooleanMethod, left);
        var rightTruthy = LinqExpression.Call(RequireBooleanMethod, right);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    private LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(RequireBooleanMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        return LinqExpression.Condition(condition, thenBranch, elseBranch);
    }

    private LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = Compile(n.Left);
        var right = Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }

    private LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var obj = Compile(m.Object);

        return LinqExpression.Call(
            GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _optionsParam,
            LinqExpression.Constant(m.NullSafe),
            _currentContext);
    }

    private LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        return LinqExpression.Call(GetIndexMethod, target, index, _optionsParam);
    }

    private LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var inferredType = LinqExpression.Variable(typeof(Type), "inferredType");

        if (v.DeclaredType != null)
        {
            value = LinqExpression.Call(
                ValidateAndCoerceTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme),
                value,
                LinqExpression.Constant(v.Name.Lexeme));
        }

        LinqExpression getInferredType;
        if (v.DeclaredType != null)
        {
            getInferredType = LinqExpression.Call(ResolveTypeNameMethod, LinqExpression.Constant(v.DeclaredType.Value.Lexeme));
        }
        else
        {
            getInferredType = LinqExpression.Condition(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Call(temp, typeof(object).GetMethod("GetType")!),
                LinqExpression.Constant(typeof(object), typeof(Type)));
        }

        return LinqExpression.Block(
            new[] { temp, inferredType },
            LinqExpression.Assign(temp, value),
            LinqExpression.Assign(inferredType, getInferredType),
            LinqExpression.Call(_currentContext, DefineWithTypeMethod,
                LinqExpression.Constant(v.Name.Lexeme), temp, inferredType),
            temp);
    }

    private LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValueExpr = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");

        var opCall = ca.Op.Type switch
        {
            TokenType.PlusEqual => LinqExpression.Call(AddMethod, currentValue, rightTemp, _optionsParam, _currentContext),
            TokenType.MinusEqual => LinqExpression.Call(SubtractMethod, currentValue, rightTemp, _optionsParam),
            TokenType.StarEqual => LinqExpression.Call(MultiplyMethod, currentValue, rightTemp, _optionsParam),
            TokenType.SlashEqual => LinqExpression.Call(DivideMethod, currentValue, rightTemp, _optionsParam),
            TokenType.PercentEqual => LinqExpression.Call(ModuloMethod, currentValue, rightTemp, _optionsParam),
            TokenType.AmpEqual => LinqExpression.Call(BitwiseAndMethod, currentValue, rightTemp, _optionsParam),
            TokenType.PipeEqual => LinqExpression.Call(BitwiseOrMethod, currentValue, rightTemp, _optionsParam),
            TokenType.CaretEqual => LinqExpression.Call(BitwiseXorMethod, currentValue, rightTemp, _optionsParam),
            TokenType.LessLessEqual => LinqExpression.Call(LeftShiftMethod, currentValue, rightTemp),
            TokenType.GreaterGreaterEqual => LinqExpression.Call(RightShiftMethod, currentValue, rightTemp),
            _ => throw new NotSupportedException($"Compound operator {ca.Op.Type}")
        };

        var validateCall = LinqExpression.Call(ValidateCompoundAssignmentMethod,
            LinqExpression.Constant(name), opCall, rightTemp, _currentContext);

        return LinqExpression.Block(
            new[] { temp, rightTemp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, validateCall),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);

        // Use a temp for index since we need it for both the check and the set
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var check = LinqExpression.Call(CheckAllowIndexSetMethod, _optionsParam, indexTemp);
        var set = LinqExpression.Call(SetIndexMethod, target, indexTemp, value);

        return LinqExpression.Block(
            new[] { indexTemp },
            LinqExpression.Assign(indexTemp, index),
            check,
            set,
            value);
    }

    private LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? LinqExpression.Call(AddMethod, left, one, _optionsParam, _currentContext)
            : LinqExpression.Call(SubtractMethod, left, one, _optionsParam);

        var checkExpr = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { temp, original },
                checkExpr,
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    private LinqExpression CompileCall(CallExpr call)
    {
        // Compile arguments into an object[] array, wrapping named arguments in NamedArg
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            call.Arguments.Select(CompileArgument));

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                LinqExpression.Call(
                    InvokeMemberCallMethod,
                    target,
                    LinqExpression.Constant(methodName),
                    argsVar,
                    LinqExpression.Constant(memberAccess.NullSafe),
                    _currentContext,
                    _optionsParam,
                    _ctParam,
                    _functionsParam,
                    _argumentTransformerParam));
        }

        // General call: evaluate callee and invoke
        var callee = Compile(call.Callee);
        return LinqExpression.Block(
            new[] { argsVar },
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Call(
                InvokeCallMethod,
                callee,
                argsVar,
                _functionsParam,
                _currentContext,
                _optionsParam,
                _ctParam,
                _argumentTransformerParam));
    }

    private LinqExpression CompileArgument(Expr arg)
    {
        if (arg is NamedArgumentExpr namedArg)
        {
            // Wrap named argument in NamedArg: new NamedArg(name, value)
            return LinqExpression.Convert(
                LinqExpression.New(
                    NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    Compile(namedArg.Value)),
                typeof(object));
        }
        return Compile(arg);
    }

    private static readonly ConstructorInfo CompiledLambdaValueCtor =
        typeof(CompiledLambdaValue).GetConstructor([
            typeof(List<string>),
            typeof(Func<object?[], CsEvalContext, object?>),
            typeof(CsEvalContext)
        ])!;

    private static readonly MethodInfo GetLambdaArgMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetLambdaArg))!;

    private LinqExpression CompileLambda(LambdaExpr lambda)
    {
        var parameterNames = lambda.Parameters.Select(p => p.Lexeme).ToList();

        // Create parameter list constant
        var listInit = LinqExpression.ListInit(
            LinqExpression.New(typeof(List<string>)),
            parameterNames.Select(p => LinqExpression.ElementInit(
                typeof(List<string>).GetMethod("Add")!,
                LinqExpression.Constant(p))));

        // Create the compiled lambda body
        var argsParam = LinqExpression.Parameter(typeof(object?[]), "args");
        var closureParam = LinqExpression.Parameter(typeof(CsEvalContext), "closure");

        // Create a child context for the lambda body
        var childContextVar = LinqExpression.Variable(typeof(CsEvalContext), "childContext");

        // Build statements to:
        // 1. Create child context from closure
        // 2. Define each parameter in the child context
        // 3. Execute the body
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(childContextVar,
                LinqExpression.Call(closureParam, CreateChildMethod))
        };

        // Define each parameter in the child context
        for (var i = 0; i < parameterNames.Count; i++)
        {
            statements.Add(LinqExpression.Call(childContextVar, DefineMethod,
                LinqExpression.Constant(parameterNames[i]),
                LinqExpression.Call(GetLambdaArgMethod, argsParam, LinqExpression.Constant(i))));
        }

        // Save the current context and swap to the child context for compiling the body
        var savedContext = _currentContext;
        _currentContext = childContextVar;

        try
        {
            // Compile the lambda body
            var compiledBody = Compile(lambda.Body);
            statements.Add(compiledBody);
        }
        finally
        {
            _currentContext = savedContext;
        }

        var lambdaBody = LinqExpression.Block(
            typeof(object),
            [childContextVar],
            statements);

        // Create the delegate: Func<object?[], CsEvalContext, object?>
        var compiledDelegate = LinqExpression.Lambda<Func<object?[], CsEvalContext, object?>>(
            lambdaBody,
            argsParam,
            closureParam);

        // Create CompiledLambdaValue(parameters, compiledBody, closure)
        return LinqExpression.New(
            CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _currentContext);
    }

    private LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, ListAddMethod, Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CreateTypedListMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    private LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr)
    {
        var dictVar = LinqExpression.Variable(typeof(IDictionary<string, object?>), "dict");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(dictVar, LinqExpression.New(ExpandoObjectCtor))
        };

        var dictItemProperty = typeof(IDictionary<string, object?>).GetProperty("Item")!;

        foreach (var (key, value) in expr.Properties)
        {
            if (key.Type == TokenType.DotDotDot && value is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(SpreadIntoDictMethod, dictVar, spreadValue, _currentContext));
            }
            else
            {
                statements.Add(LinqExpression.Assign(
                    LinqExpression.Property(dictVar, dictItemProperty, LinqExpression.Constant(key.Lexeme)),
                    Compile(value)));
            }
        }

        statements.Add(LinqExpression.Convert(dictVar, typeof(object)));
        return LinqExpression.Block(new[] { dictVar }, statements);
    }

    private LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = Compile(exprPart.Expression);
                    var valueAsString = LinqExpression.Condition(
                        LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Constant(""),
                        LinqExpression.Call(value, ObjectToStringMethod));
                    statements.Add(LinqExpression.Call(sbVar, StringBuilderAppendMethod, valueAsString));
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    private LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var value = Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{expr.Name.Lexeme} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _optionsParam, _currentContext),
            temp);
    }

    private LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _currentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_currentContext, SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }

    #endregion
}
