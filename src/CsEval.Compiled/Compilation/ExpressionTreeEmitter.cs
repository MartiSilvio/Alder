using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

/// <summary>
/// Translates bound CsEval nodes into typed System.Linq.Expressions trees.
/// Produces provider-transparent expression trees suitable for Entity Framework,
/// IQueryable providers, and in-memory compilation via .Compile().
/// </summary>
internal sealed class ExpressionTreeEmitter
{
    private readonly Dictionary<string, ParameterExpression> _parameterScope;
    private readonly Dictionary<string, object?> _engineVariables;
    private readonly TypeResolver _typeResolver;
    private bool _isChecked;

    private static readonly MethodInfo StringConcat2 =
        typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;

    public ExpressionTreeEmitter(
        Dictionary<string, ParameterExpression> parameterScope,
        Dictionary<string, object?> engineVariables,
        TypeResolver typeResolver)
    {
        _parameterScope = parameterScope;
        _engineVariables = engineVariables;
        _typeResolver = typeResolver;
    }

    public LinqExpression Emit(BoundExpr expr)
    {
        return expr.Kind switch
        {
            BoundNodeKind.Literal => EmitLiteral((BoundLiteralExpr)expr),
            BoundNodeKind.Identifier => EmitIdentifier((BoundIdentifierExpr)expr),
            BoundNodeKind.BinaryOperator => EmitBinary((BoundBinaryExpr)expr),
            BoundNodeKind.LogicalOperator => EmitLogical((BoundLogicalExpr)expr),
            BoundNodeKind.UnaryOperator => EmitUnary((BoundUnaryExpr)expr),
            BoundNodeKind.MemberAccess => EmitMemberAccess((BoundMemberAccessExpr)expr),
            BoundNodeKind.Call => EmitCall((BoundCallExpr)expr),
            BoundNodeKind.ConditionalOperator => EmitConditional((BoundConditionalExpr)expr),
            BoundNodeKind.NullCoalescingOperator => EmitNullCoalesce((BoundNullCoalesceExpr)expr),
            BoundNodeKind.Conversion => EmitCast((BoundCastExpr)expr),
            BoundNodeKind.CheckedExpression => EmitChecked((BoundCheckedExpr)expr),
            BoundNodeKind.IsPatternExpression => EmitIsPattern((BoundIsPatternExpr)expr),
            BoundNodeKind.IndexerAccess => EmitIndexAccess((BoundIndexAccessExpr)expr),
            BoundNodeKind.ObjectCreationExpression => EmitObjectCreation((BoundObjectCreationExpr)expr),

            BoundNodeKind.SwitchExpression => throw UnsupportedNode("a switch expression"),
            BoundNodeKind.Block => throw UnsupportedNode("a block"),
            BoundNodeKind.IfStatement => throw UnsupportedNode("an if statement"),
            BoundNodeKind.WhileStatement => throw UnsupportedNode("a while loop"),
            BoundNodeKind.ForStatement => throw UnsupportedNode("a for loop"),
            BoundNodeKind.ForEachStatement => throw UnsupportedNode("a foreach loop"),
            BoundNodeKind.DoStatement => throw UnsupportedNode("a do-while loop"),
            BoundNodeKind.AssignmentOperator or
            BoundNodeKind.MemberAssignment or
            BoundNodeKind.IndexAssignment or
            BoundNodeKind.CompoundAssignmentOperator or
            BoundNodeKind.MemberCompoundAssignment or
            BoundNodeKind.IndexCompoundAssignment or
            BoundNodeKind.NullCoalescingAssignmentOperator or
            BoundNodeKind.MemberNullCoalesceAssignment or
            BoundNodeKind.IndexNullCoalesceAssignment or
            BoundNodeKind.IncrementOperator or
            BoundNodeKind.MemberIncrement or
            BoundNodeKind.IndexIncrement or
            BoundNodeKind.MultiDimIndexAssignment => throw UnsupportedNode("an assignment"),
            BoundNodeKind.VariableDeclaration => throw UnsupportedNode("a variable declaration"),
            BoundNodeKind.TryStatement => throw UnsupportedNode("try/catch"),
            BoundNodeKind.ArrayLiteral => throw UnsupportedNode("a collection expression"),
            BoundNodeKind.ObjectLiteral => throw UnsupportedNode("an object literal"),
            BoundNodeKind.SpreadElement => throw UnsupportedNode("spread"),
            BoundNodeKind.SliceExpression => throw UnsupportedNode("slice"),
            BoundNodeKind.Lambda => throw UnsupportedNode("a nested lambda"),
            BoundNodeKind.InterpolatedString => throw UnsupportedNode("an interpolated string"),
            BoundNodeKind.ThrowExpression => throw UnsupportedNode("a throw expression"),
            BoundNodeKind.ThrowStatement => throw UnsupportedNode("a throw statement"),
            BoundNodeKind.TupleLiteral => throw UnsupportedNode("a tuple expression"),
            BoundNodeKind.DeconstructionAssignment => throw UnsupportedNode("deconstruction"),
            BoundNodeKind.SwitchStatement => throw UnsupportedNode("a switch statement"),
            BoundNodeKind.ReturnStatement => throw UnsupportedNode("a return statement"),
            BoundNodeKind.BreakStatement => throw UnsupportedNode("a break statement"),
            BoundNodeKind.ContinueStatement => throw UnsupportedNode("a continue statement"),
            BoundNodeKind.GotoStatement => throw UnsupportedNode("a goto statement"),
            BoundNodeKind.GotoCaseStatement => throw UnsupportedNode("a goto case statement"),
            BoundNodeKind.GotoDefaultStatement => throw UnsupportedNode("a goto default statement"),
            BoundNodeKind.Label => throw UnsupportedNode("a label"),
            BoundNodeKind.AsOperator => throw UnsupportedNode("an 'as' expression"),
            BoundNodeKind.TypedArrayCreation or
            BoundNodeKind.TypedArrayLiteral => throw UnsupportedNode("an array creation expression"),
            BoundNodeKind.MultiDimIndexAccess => throw UnsupportedNode("multi-dimensional indexing"),
            BoundNodeKind.MultiDimTypedArrayCreation or
            BoundNodeKind.MultiDimArrayInit => throw UnsupportedNode("multi-dimensional array creation"),
            BoundNodeKind.NamedArgument => throw UnsupportedNode("a named argument"),
            BoundNodeKind.OutArgument => throw UnsupportedNode("an out argument"),
            BoundNodeKind.UsingStatement => throw UnsupportedNode("a using statement"),
            BoundNodeKind.LockStatement => throw UnsupportedNode("a lock statement"),
            BoundNodeKind.RangeExpression => throw UnsupportedNode("range literals"),
            BoundNodeKind.PipelineExpression => throw UnsupportedNode("pipeline operator"),
            BoundNodeKind.ChainedComparisonOperator => throw UnsupportedNode("chained comparison"),
            BoundNodeKind.Invoke => throw UnsupportedCallShape(DescribeInvokeShape((BoundInvokeExpr)expr)),
            BoundNodeKind.FromEndIndexExpression => throw UnsupportedNode("index from end"),

            _ => throw UnsupportedNode($"expression type '{expr.GetType().Name}'")
        };
    }

    private static LinqExpression EmitLiteral(BoundLiteralExpr expr)
    {
        if (expr.Value is null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(expr.Value, expr.Value.GetType());
    }

    private LinqExpression EmitIdentifier(BoundIdentifierExpr expr)
    {
        var name = expr.Name;

        if (_parameterScope.TryGetValue(name, out var param))
            return param;

        if (_engineVariables.TryGetValue(name, out var value))
            return LinqExpression.Constant(value, value?.GetType() ?? typeof(object));

        throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, name);
    }

    private LinqExpression EmitBinary(BoundBinaryExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);

        if (expr.Operator == TokenType.Plus && IsStringConcatenation(left, right))
        {
            var leftStr = EnsureString(left);
            var rightStr = EnsureString(right);
            return LinqExpression.Call(StringConcat2, leftStr, rightStr);
        }

        if (NeedsNumericPromotion(expr.Operator))
            PromoteNumericOperands(ref left, ref right);

        return expr.Operator switch
        {
            TokenType.Plus => _isChecked ? LinqExpression.AddChecked(left, right) : LinqExpression.Add(left, right),
            TokenType.Minus => _isChecked ? LinqExpression.SubtractChecked(left, right) : LinqExpression.Subtract(left, right),
            TokenType.Star => _isChecked ? LinqExpression.MultiplyChecked(left, right) : LinqExpression.Multiply(left, right),
            TokenType.Slash => LinqExpression.Divide(left, right),
            TokenType.Percent => LinqExpression.Modulo(left, right),
            TokenType.EqualEqual => LinqExpression.Equal(left, right),
            TokenType.BangEqual => LinqExpression.NotEqual(left, right),
            TokenType.Less => LinqExpression.LessThan(left, right),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(left, right),
            TokenType.Greater => LinqExpression.GreaterThan(left, right),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(left, right),
            TokenType.Amp => LinqExpression.And(left, right),
            TokenType.Pipe => LinqExpression.Or(left, right),
            TokenType.Caret => LinqExpression.ExclusiveOr(left, right),
            TokenType.LessLess => LinqExpression.LeftShift(left, right),
            TokenType.GreaterGreater => LinqExpression.RightShift(left, right),
            _ => throw UnsupportedNode($"operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitLogical(BoundLogicalExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);

        return expr.Operator switch
        {
            TokenType.AmpAmp => LinqExpression.AndAlso(left, right),
            TokenType.PipePipe => LinqExpression.OrElse(left, right),
            _ => throw UnsupportedNode($"operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitUnary(BoundUnaryExpr expr)
    {
        var operand = Emit(expr.Operand);

        return expr.Operator switch
        {
            TokenType.Minus => _isChecked ? LinqExpression.NegateChecked(operand) : LinqExpression.Negate(operand),
            TokenType.Plus => operand,
            TokenType.Bang => LinqExpression.Not(operand),
            TokenType.Tilde => LinqExpression.Not(operand),
            _ => throw UnsupportedNode($"unary operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitMemberAccess(BoundMemberAccessExpr expr)
    {
        if (expr.NullSafe)
            throw UnsupportedNode("null-conditional access");

        var target = Emit(expr.Target);
        var plan = expr.Plan;
        if (plan?.Member is PropertyInfo property)
            return plan.IsStatic
                ? LinqExpression.Property(null, property)
                : LinqExpression.Property(target, property);

        if (plan?.Member is FieldInfo field)
            return plan.IsStatic
                ? LinqExpression.Field(null, field)
                : LinqExpression.Field(target, field);

        if (plan?.IsMethodGroup == true)
            throw UnsupportedCallShape("unresolved method group");

        throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, target.Type.Name, expr.MemberName);
    }

    private LinqExpression EmitCall(BoundCallExpr expr)
    {
        for (var i = 0; i < expr.Arguments.Length; i++)
        {
            if (expr.Arguments[i] is BoundNamedArgumentExpr)
                throw UnsupportedCallShape("named argument");
            if (expr.Arguments[i] is BoundOutArgExpr)
                throw UnsupportedCallShape("out argument");
        }

        var method = expr.Plan.SelectedMethod;
        if (!EmitHelpers.CanEmitDirectMethodCall(expr.Plan, expr.Arguments.Length))
            throw UnsupportedCallShape("dynamic or unresolved call target");

        var parameters = MethodDispatchCache.GetParameters(method);
        var args = EmitPlannedCallArguments(expr, parameters);

        if (expr.Plan.IsStaticCall)
            return LinqExpression.Call(method, args);

        if (expr.Callee is not BoundMemberAccessExpr memberCallee)
            throw UnsupportedCallShape("non-member call target");

        if (memberCallee.NullSafe)
            throw UnsupportedNode("null-conditional access");

        var target = Emit(memberCallee.Target);
        return LinqExpression.Call(target, method, args);
    }

    private LinqExpression[] EmitPlannedCallArguments(BoundCallExpr expr, ParameterInfo[] parameters)
    {
        var emitted = new LinqExpression[parameters.Length];
        var conversions = expr.Plan.ArgumentConversions;

        foreach (var binding in expr.Plan.ParameterBindings)
        {
            switch (binding.Kind)
            {
                case BoundParameterBindingKind.Argument:
                {
                    var sourceIndex = binding.SourceArgumentIndex;
                    var conversion = conversions[sourceIndex];
                    var argument = Emit(expr.Arguments[sourceIndex]);
                    emitted[binding.ParameterIndex] = conversion.IsIdentity || argument.Type == conversion.TargetType
                        ? argument
                        : LinqExpression.Convert(argument, conversion.TargetType);
                    break;
                }

                case BoundParameterBindingKind.DefaultValue:
                    emitted[binding.ParameterIndex] = EmitDefaultArgument(parameters[binding.ParameterIndex]);
                    break;

                case BoundParameterBindingKind.ParamsArray:
                {
                    var parameter = parameters[binding.ParameterIndex];
                    var elementType = parameter.ParameterType.GetElementType()
                                     ?? throw UnsupportedCallShape("params argument with non-array parameter");
                    var args = new LinqExpression[binding.SourceArgumentCount];

                    for (var i = 0; i < binding.SourceArgumentCount; i++)
                    {
                        var sourceIndex = binding.SourceArgumentIndex + i;
                        var conversion = conversions[sourceIndex];
                        var argument = Emit(expr.Arguments[sourceIndex]);
                        var converted = conversion.IsIdentity || argument.Type == conversion.TargetType
                            ? argument
                            : LinqExpression.Convert(argument, conversion.TargetType);
                        args[i] = converted.Type == elementType
                            ? converted
                            : LinqExpression.Convert(converted, elementType);
                    }

                    emitted[binding.ParameterIndex] = LinqExpression.NewArrayInit(elementType, args);
                    break;
                }

                default:
                    throw UnsupportedCallShape($"parameter binding kind '{binding.Kind}'");
            }
        }

        for (var i = 0; i < emitted.Length; i++)
        {
            if (emitted[i] == null)
                throw UnsupportedCallShape("incomplete argument emission");
        }

        return emitted;
    }

    private static LinqExpression EmitDefaultArgument(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var defaultValue = parameter.DefaultValue;

        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
            return LinqExpression.Default(parameterType);

        return LinqExpression.Constant(defaultValue, parameterType);
    }

    private LinqExpression EmitConditional(BoundConditionalExpr expr)
    {
        var test = Emit(expr.Condition);
        var ifTrue = Emit(expr.ThenBranch);
        var ifFalse = Emit(expr.ElseBranch);

        if (ifTrue.Type != ifFalse.Type)
        {
            var commonType = GetCommonType(ifTrue.Type, ifFalse.Type);
            if (commonType != null)
            {
                if (ifTrue.Type != commonType)
                    ifTrue = LinqExpression.Convert(ifTrue, commonType);
                if (ifFalse.Type != commonType)
                    ifFalse = LinqExpression.Convert(ifFalse, commonType);
            }
        }

        return LinqExpression.Condition(test, ifTrue, ifFalse);
    }

    private LinqExpression EmitNullCoalesce(BoundNullCoalesceExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);
        return LinqExpression.Coalesce(left, right);
    }

    private LinqExpression EmitChecked(BoundCheckedExpr expr)
    {
        var previous = _isChecked;
        _isChecked = expr.IsChecked;
        try
        {
            return Emit(expr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    private LinqExpression EmitCast(BoundCastExpr expr)
    {
        var operand = Emit(expr.Expression);
        return _isChecked
            ? LinqExpression.ConvertChecked(operand, expr.TargetType)
            : LinqExpression.Convert(operand, expr.TargetType);
    }

    private LinqExpression EmitIsPattern(BoundIsPatternExpr expr)
    {
        if (expr.Pattern is TypePattern typePattern)
        {
            var operand = Emit(expr.Expression);
            var type = _typeResolver.ResolveType(typePattern.TypeToken.Lexeme);
            return LinqExpression.TypeIs(operand, type);
        }

        throw UnsupportedNode("pattern matching");
    }

    private LinqExpression EmitIndexAccess(BoundIndexAccessExpr expr)
    {
        if (expr.NullSafe)
            throw UnsupportedNode("null-conditional access");

        var obj = Emit(expr.Target);
        var index = Emit(expr.Index);

        if (obj.Type.IsArray)
            return LinqExpression.ArrayIndex(obj, EnsureIndexType(index));

        var indexer = obj.Type.GetProperty("Item");
        if (indexer != null)
        {
            var indexerParams = indexer.GetIndexParameters();
            if (indexerParams.Length == 1 && index.Type != indexerParams[0].ParameterType)
                index = LinqExpression.Convert(index, indexerParams[0].ParameterType);

            return LinqExpression.MakeIndex(obj, indexer, [index]);
        }

        throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, obj.Type.Name);
    }

    private LinqExpression EmitObjectCreation(BoundObjectCreationExpr expr)
    {
        if (expr.InitializerEntries.Length > 0)
            throw UnsupportedNode("object initializer");

        var targetType = expr.StaticType != typeof(object)
            ? expr.StaticType
            : _typeResolver.ResolveType(expr.TypeName);

        var args = expr.Arguments.Select(Emit).ToArray();
        var argTypes = args.Select(a => a.Type).ToArray();

        var ctor = targetType.GetConstructor(argTypes);
        if (ctor == null)
        {
            ctor = FindCompatibleConstructor(targetType, argTypes);
            if (ctor == null)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.NoMatchingConstructor,
                    targetType.Name,
                    args.Length);
            }

            args = CoerceConstructorArguments(ctor, args);
        }

        return LinqExpression.New(ctor, args);
    }

    private static LinqExpression EnsureIndexType(LinqExpression index)
    {
        if (index.Type == typeof(int))
            return index;

        if (IsIntegralType(index.Type))
            return LinqExpression.Convert(index, typeof(int));

        return index;
    }

    private static bool IsIntegralType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.SByte or
            TypeCode.Byte or
            TypeCode.Int16 or
            TypeCode.UInt16 or
            TypeCode.Int32 or
            TypeCode.UInt32 or
            TypeCode.Int64 or
            TypeCode.UInt64 => true,
            _ => false
        };
    }

    private static LinqExpression[] CoerceConstructorArguments(
        ConstructorInfo ctor, LinqExpression[] args)
    {
        var parameters = ctor.GetParameters();
        var result = new LinqExpression[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Type != parameters[i].ParameterType)
                result[i] = LinqExpression.Convert(args[i], parameters[i].ParameterType);
            else
                result[i] = args[i];
        }
        return result;
    }

    private static ConstructorInfo? FindCompatibleConstructor(Type type, Type[] argTypes)
    {
        foreach (var ctor in type.GetConstructors())
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length != argTypes.Length)
                continue;

            var compatible = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!TypeHelpers.CanAssignOrImplicitlyConvert(argTypes[i], parameters[i].ParameterType))
                {
                    compatible = false;
                    break;
                }
            }

            if (compatible)
                return ctor;
        }

        return null;
    }

    private static bool NeedsNumericPromotion(TokenType op) => op is
        TokenType.Plus or TokenType.Minus or TokenType.Star or
        TokenType.Slash or TokenType.Percent or
        TokenType.EqualEqual or TokenType.BangEqual or
        TokenType.Less or TokenType.LessEqual or
        TokenType.Greater or TokenType.GreaterEqual;

    private static void PromoteNumericOperands(ref LinqExpression left, ref LinqExpression right)
    {
        if (left.Type == right.Type)
            return;

        var promoted = TypeHelpers.TryGetBinaryNumericPromotionType(left.Type, right.Type);
        if (promoted == null)
            return;

        if (left.Type != promoted)
            left = LinqExpression.Convert(left, promoted);
        if (right.Type != promoted)
            right = LinqExpression.Convert(right, promoted);
    }

    private static bool IsStringConcatenation(LinqExpression left, LinqExpression right)
        => left.Type == typeof(string) || right.Type == typeof(string);

    private static LinqExpression EnsureString(LinqExpression expr)
    {
        if (expr.Type == typeof(string))
            return expr;

        if (expr.Type.IsValueType)
        {
            var boxed = LinqExpression.Convert(expr, typeof(object));
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
            return LinqExpression.Call(boxed, toStringMethod);
        }

        var objToString = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        return LinqExpression.Call(expr, objToString);
    }

    private static Type? GetCommonType(Type left, Type right)
    {
        var promoted = TypeHelpers.TryGetBinaryNumericPromotionType(left, right);
        if (promoted != null)
            return promoted;

        if (left.IsAssignableFrom(right))
            return left;
        if (right.IsAssignableFrom(left))
            return right;

        if (!left.IsValueType && !right.IsValueType)
            return typeof(object);

        return null;
    }

    private static CsEvalException UnsupportedNode(string feature) =>
        new(DiagnosticDescriptors.ExpressionTreeUnsupportedNode, feature);

    private static CsEvalException UnsupportedCallShape(string shape) =>
        new(DiagnosticDescriptors.ExpressionTreeUnsupportedCallShape, shape);

    private static string DescribeInvokeShape(BoundInvokeExpr invoke)
    {
        for (var i = 0; i < invoke.Arguments.Length; i++)
        {
            if (invoke.Arguments[i] is BoundNamedArgumentExpr)
                return "named argument";
            if (invoke.Arguments[i] is BoundOutArgExpr)
                return "out argument";
        }

        return "direct invocation";
    }
}
