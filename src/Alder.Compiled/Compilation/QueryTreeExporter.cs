using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.OverloadResolution;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed class QueryTreeExporter
{
    private readonly Dictionary<string, ParameterExpression> _parameters;
    private readonly IReadOnlyDictionary<string, object?> _capturedVariables;
    private bool _isChecked;

    internal QueryTreeExporter(
        IReadOnlyList<ParameterExpression> parameters,
        IReadOnlyDictionary<string, object?> capturedVariables)
    {
        _parameters = parameters.ToDictionary(static parameter => parameter.Name!, StringComparer.Ordinal);
        _capturedVariables = capturedVariables;
    }

    internal LinqExpression Export(BoundExpr expr) => expr.Kind switch
    {
        BoundNodeKind.Literal => EmitLiteral((BoundLiteralExpr)expr),
        BoundNodeKind.Identifier => EmitIdentifier((BoundIdentifierExpr)expr),
        BoundNodeKind.TypeReference => Expression.Constant(((BoundTypeRefExpr)expr).TargetType, typeof(Type)),
        BoundNodeKind.ObjectLiteral => EmitObjectLiteral((BoundObjectLiteralExpr)expr),
        BoundNodeKind.BinaryOperator => EmitBinary((BoundBinaryExpr)expr),
        BoundNodeKind.LogicalOperator => EmitLogical((BoundLogicalExpr)expr),
        BoundNodeKind.UnaryOperator => EmitUnary((BoundUnaryExpr)expr),
        BoundNodeKind.PropertyAccess => EmitPropertyAccess((BoundPropertyAccessExpr)expr),
        BoundNodeKind.FieldAccess => EmitFieldAccess((BoundFieldAccessExpr)expr),
        BoundNodeKind.ResolvedCall => EmitResolvedCall((BoundResolvedCallExpr)expr),
        BoundNodeKind.DynamicCall => throw QueryTreeSupport.Unsupported(DescribeDynamicCallShape((BoundDynamicCallExpr)expr)),
        BoundNodeKind.ConditionalOperator => EmitConditional((BoundConditionalExpr)expr),
        BoundNodeKind.NullCoalescingOperator => EmitNullCoalesce((BoundNullCoalesceExpr)expr),
        BoundNodeKind.Conversion => EmitCast((BoundCastExpr)expr),
        BoundNodeKind.CheckedExpression => EmitChecked((BoundCheckedExpr)expr),
        BoundNodeKind.IsPatternExpression => EmitIsPattern((BoundIsPatternExpr)expr),
        BoundNodeKind.ResolvedIndexAccess => EmitResolvedIndexAccess((BoundResolvedIndexAccessExpr)expr),
        BoundNodeKind.ObjectCreationExpression => EmitObjectCreation((BoundObjectCreationExpr)expr),
        BoundNodeKind.AsOperator => EmitAs((BoundAsExpr)expr),
        _ => throw QueryTreeSupport.Unsupported(expr.Kind)
    };

    private static LinqExpression EmitLiteral(BoundLiteralExpr expr)
    {
        if (expr.Value != null)
            return Expression.Constant(expr.Value, expr.Value.GetType());

        var staticType = expr.StaticType.ClrType;
        if (staticType != typeof(object) &&
            (!staticType.IsValueType || Nullable.GetUnderlyingType(staticType) != null))
        {
            return Expression.Constant(null, staticType);
        }

        return Expression.Constant(null, typeof(object));
    }

    private LinqExpression EmitIdentifier(BoundIdentifierExpr expr)
    {
        if (_parameters.TryGetValue(expr.Name, out var parameter))
            return parameter;

        if (_capturedVariables.TryGetValue(expr.Name, out var captured))
            return CreateCapturedConstant(expr, captured);

        throw new AlderException(DiagnosticDescriptors.NameNotInContext, expr.Name);
    }

    private LinqExpression EmitObjectLiteral(BoundObjectLiteralExpr expr)
    {
        var structuralInfo = ((BoundStructuralType)expr.StaticType).StructuralInfo
            ?? throw new InvalidOperationException("Structural object literal missing runtime type metadata.");
        var memberNames = structuralInfo.Members.Select(static member => Expression.Constant(member.Name));
        var values = expr.Properties
            .Select(static property => property.Value)
            .Select(Export)
            .Select(static value => value.Type == typeof(object) ? value : Expression.Convert(value, typeof(object)));

        return Expression.Call(
            CreateUntypedStructuralObjectMethod,
            Expression.NewArrayInit(typeof(string), memberNames),
            Expression.NewArrayInit(typeof(object), values));
    }

    private LinqExpression EmitBinary(BoundBinaryExpr expr)
    {
        var left = Export(expr.Left);
        var right = Export(expr.Right);

        if (expr.Operator == TokenType.Plus && (left.Type == typeof(string) || right.Type == typeof(string)))
        {
            left = EnsureString(left);
            right = EnsureString(right);
            return Expression.Call(StringConcatTwoStringsMethod, left, right);
        }

        AlignBinaryOperands(expr.Operator, ref left, ref right);

        return expr.Operator switch
        {
            TokenType.Plus => _isChecked ? Expression.AddChecked(left, right) : Expression.Add(left, right),
            TokenType.Minus => _isChecked ? Expression.SubtractChecked(left, right) : Expression.Subtract(left, right),
            TokenType.Star => _isChecked ? Expression.MultiplyChecked(left, right) : Expression.Multiply(left, right),
            TokenType.Slash => Expression.Divide(left, right),
            TokenType.Percent => Expression.Modulo(left, right),
            TokenType.EqualEqual => Expression.Equal(left, right),
            TokenType.BangEqual => Expression.NotEqual(left, right),
            TokenType.Less => Expression.LessThan(left, right),
            TokenType.LessEqual => Expression.LessThanOrEqual(left, right),
            TokenType.Greater => Expression.GreaterThan(left, right),
            TokenType.GreaterEqual => Expression.GreaterThanOrEqual(left, right),
            TokenType.Amp => Expression.And(left, right),
            TokenType.Pipe => Expression.Or(left, right),
            TokenType.Caret => Expression.ExclusiveOr(left, right),
            TokenType.LessLess => Expression.LeftShift(left, EnsureIntIndex(right)),
            TokenType.GreaterGreater => Expression.RightShift(left, EnsureIntIndex(right)),
            _ => throw QueryTreeSupport.Unsupported($"operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitLogical(BoundLogicalExpr expr)
    {
        var left = Export(expr.Left);
        var right = Export(expr.Right);
        return expr.Operator switch
        {
            TokenType.AmpAmp => Expression.AndAlso(left, right),
            TokenType.PipePipe => Expression.OrElse(left, right),
            _ => throw QueryTreeSupport.Unsupported($"operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitUnary(BoundUnaryExpr expr)
    {
        var operand = EnsureTyped(Export(expr.Operand), expr.PromotedType ?? expr.Operand.StaticType.ClrType);
        return expr.Operator switch
        {
            TokenType.Minus => _isChecked ? Expression.NegateChecked(operand) : Expression.Negate(operand),
            TokenType.Plus => operand,
            TokenType.Bang => Expression.Not(operand),
            TokenType.Tilde => Expression.Not(operand),
            _ => throw QueryTreeSupport.Unsupported($"unary operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitPropertyAccess(BoundPropertyAccessExpr expr)
    {
        QueryTreeSupport.EnsureNoNullConditional(expr.NullSafe);
        QueryTreeSupport.EnsureNoForbiddenReflectionType(expr.StaticType.ClrType, $"property '{expr.MemberName}'");
        return expr.IsStatic
            ? Expression.Property(null, expr.Property)
            : Expression.Property(Export(expr.Target), expr.Property);
    }

    private LinqExpression EmitFieldAccess(BoundFieldAccessExpr expr)
    {
        QueryTreeSupport.EnsureNoNullConditional(expr.NullSafe);
        QueryTreeSupport.EnsureNoForbiddenReflectionType(expr.StaticType.ClrType, $"field '{expr.MemberName}'");
        return expr.IsStatic
            ? Expression.Field(null, expr.Field)
            : Expression.Field(Export(expr.Target), expr.Field);
    }

    private LinqExpression EmitResolvedCall(BoundResolvedCallExpr expr)
    {
        QueryTreeSupport.EnsureSupportedCall(expr);
        QueryTreeSupport.EnsureNoForbiddenReflectionType(expr.StaticType.ClrType, $"method '{expr.SelectedMethod.Name}'");

        var method = expr.SelectedMethod;
        var target = expr.Callee switch
        {
            BoundMemberAccessBase member when !expr.IsStaticCall => Export(member.Target),
            _ => null
        };
        var parameters = MethodDispatchCache.GetParameters(method);
        var args = BuildCallArguments(expr, parameters);

        return expr.IsStaticCall
            ? Expression.Call(method, args)
            : Expression.Call(target!, method, args);
    }

    private LinqExpression EmitConditional(BoundConditionalExpr expr)
    {
        var condition = Export(expr.Condition);
        var ifTrue = Export(expr.ThenBranch);
        var ifFalse = Export(expr.ElseBranch);
        var resultType = expr.StaticType.ClrType;

        if (ifTrue.Type != resultType)
            ifTrue = Expression.Convert(ifTrue, resultType);
        if (ifFalse.Type != resultType)
            ifFalse = Expression.Convert(ifFalse, resultType);

        return Expression.Condition(condition, ifTrue, ifFalse);
    }

    private LinqExpression EmitNullCoalesce(BoundNullCoalesceExpr expr)
    {
        var left = Export(expr.Left);
        var right = Export(expr.Right);
        var leftType = expr.Left.StaticType.ClrType;
        var rightType = expr.Right.StaticType.ClrType;

        if (left.Type != leftType)
            left = Expression.Convert(left, leftType);
        if (right.Type != rightType)
            right = Expression.Convert(right, rightType);

        if (left.Type != right.Type && expr.StaticType.ClrType != typeof(object) && right.Type != expr.StaticType.ClrType)
            right = Expression.Convert(right, expr.StaticType.ClrType);

        return Expression.Coalesce(left, right);
    }

    private LinqExpression EmitCast(BoundCastExpr expr)
    {
        var operand = Export(expr.Expression);
        return _isChecked
            ? Expression.ConvertChecked(operand, expr.TargetType)
            : Expression.Convert(operand, expr.TargetType);
    }

    private LinqExpression EmitChecked(BoundCheckedExpr expr)
    {
        var previous = _isChecked;
        _isChecked = expr.IsChecked;
        try
        {
            return Export(expr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    private LinqExpression EmitIsPattern(BoundIsPatternExpr expr)
    {
        if (expr.Pattern is TypePattern { VariableName: null } typePattern
            && TypeResolver.TryResolveKeywordType(typePattern.TypeToken.Lexeme, out var resolvedType))
        {
            return Expression.TypeIs(Export(expr.Expression), resolvedType);
        }

        throw QueryTreeSupport.Unsupported("pattern matching");
    }

    private LinqExpression EmitResolvedIndexAccess(BoundResolvedIndexAccessExpr expr)
    {
        QueryTreeSupport.EnsureNoNullConditional(expr.NullSafe);

        var target = Export(expr.Target);
        var index = Export(expr.Index);

        if (target.Type.IsArray)
            return Expression.ArrayIndex(target, EnsureIntIndex(index));

        var indexer = target.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property =>
            {
                if (!string.Equals(property.Name, "Item", StringComparison.Ordinal))
                    return false;

                var parameters = property.GetIndexParameters();
                if (parameters.Length != 1)
                    return false;

                var parameterType = parameters[0].ParameterType;
                return parameterType == index.Type || parameterType.IsAssignableFrom(index.Type);
            });
        if (indexer == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, target.Type.Name);

        var parameters = indexer.GetIndexParameters();
        if (parameters.Length == 1 && index.Type != parameters[0].ParameterType)
            index = Expression.Convert(index, parameters[0].ParameterType);

        return Expression.MakeIndex(target, indexer, [index]);
    }

    private LinqExpression EmitObjectCreation(BoundObjectCreationExpr expr)
    {
        var pure = TryEmitPureObjectCreation(expr);
        if (pure != null)
            return pure;

        throw QueryTreeSupport.Unsupported("an object creation expression");
    }

    private LinqExpression EmitAs(BoundAsExpr expr)
    {
        if (expr.TargetType.IsValueType)
            throw QueryTreeSupport.Unsupported("an 'as' expression");

        return Expression.TypeAs(EnsureObject(Export(expr.Expression)), expr.TargetType);
    }

    private LinqExpression[] BuildCallArguments(BoundResolvedCallExpr call, ParameterInfo[] parameters)
    {
        var emitted = new LinqExpression[parameters.Length];
        var resolved = call.Resolution;
        var sources = resolved.ArgMap.Sources;
        var conversions = resolved.Conversions;

        for (var paramIdx = 0; paramIdx < sources.Length; paramIdx++)
        {
            var source = sources[paramIdx];
            switch (source.Kind)
            {
                case ParameterSourceKind.Argument:
                {
                    var argIdx = source.ArgumentIndex;
                    var conversion = conversions[argIdx];
                    if (call.IsExtensionCall && argIdx == 0)
                    {
                        emitted[paramIdx] = EmitExtensionReceiverArgument(call.Arguments[argIdx], conversion);
                        break;
                    }

                    emitted[paramIdx] = EmitCallArgument(call.Arguments[argIdx], conversion);
                    break;
                }

                case ParameterSourceKind.Default:
                    emitted[paramIdx] = EmitDefaultArgument(parameters[paramIdx]);
                    break;

                case ParameterSourceKind.ParamsRange:
                {
                    var parameter = parameters[paramIdx];
                    var elementType = parameter.ParameterType.GetElementType()
                        ?? throw new BindingNotSupportedException("Params parameter must be an array type.");
                    var args = new LinqExpression[source.ParamsCount];
                    for (var i = 0; i < source.ParamsCount; i++)
                    {
                        var argIdx = source.ParamsStartIndex + i;
                        args[i] = EnsureTyped(
                            EmitCallArgument(call.Arguments[argIdx], conversions[argIdx]),
                            elementType);
                    }

                    emitted[paramIdx] = Expression.NewArrayInit(elementType, args);
                    break;
                }

                default:
                    throw new BindingNotSupportedException(
                        $"Parameter source kind '{source.Kind}' is not implemented");
            }
        }

        return emitted;
    }

    private LinqExpression EmitExtensionReceiverArgument(BoundExpr argument, ArgumentConversion conversion)
    {
        var emittedArgument = Export(argument);
        var targetType = conversion.TargetType;

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        return conversion.Kind switch
        {
            ArgumentConversionKind.ImplicitReference when targetType.IsAssignableFrom(emittedArgument.Type) => emittedArgument,
            ArgumentConversionKind.Boxing when targetType == typeof(object) => EnsureObject(emittedArgument),
            ArgumentConversionKind.LambdaToDelegate => throw QueryTreeSupport.Unsupported("lambda-to-delegate conversion"),
            _ => throw QueryTreeSupport.Unsupported("an extension receiver requiring runtime coercion")
        };
    }

    private LinqExpression EmitCallArgument(BoundExpr argument, ArgumentConversion conversion)
    {
        var emittedArgument = Export(argument);
        var targetType = conversion.TargetType;

        if (targetType == typeof(object))
            return EnsureObject(emittedArgument);

        if (emittedArgument.Type == targetType)
            return emittedArgument;

        if (conversion.Kind == ArgumentConversionKind.LambdaToDelegate)
            throw QueryTreeSupport.Unsupported("lambda-to-delegate conversion");

        return Expression.Convert(emittedArgument, targetType);
    }

    private static LinqExpression EmitDefaultArgument(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var defaultValue = parameter.DefaultValue;
        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
            return Expression.Default(parameterType);
        return Expression.Constant(defaultValue, parameterType);
    }

    private LinqExpression? TryEmitPureObjectCreation(BoundObjectCreationExpr expr)
    {
        var type = expr.StaticType.ClrType;
        if (type == typeof(object) || type.IsAbstract || type.IsInterface || !expr.InitializerEntries.IsDefaultOrEmpty)
            return null;

        if (expr.Arguments.Length == 0)
        {
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
                return Expression.New(defaultCtor);
            if (type.IsValueType)
                return Expression.New(type);
            return null;
        }

        var argTypes = new Type[expr.Arguments.Length];
        for (var i = 0; i < expr.Arguments.Length; i++)
        {
            var argType = expr.Arguments[i].StaticType.ClrType;
            if (argType == typeof(object))
                return null;
            argTypes[i] = argType;
        }

        var ctor = type.GetConstructor(argTypes);
        if (ctor == null)
            return null;

        var ctorParams = ctor.GetParameters();
        var args = new LinqExpression[expr.Arguments.Length];
        for (var i = 0; i < args.Length; i++)
            args[i] = EnsureTyped(Export(expr.Arguments[i]), ctorParams[i].ParameterType);

        return Expression.New(ctor, args);
    }

    private static LinqExpression CreateCapturedConstant(BoundIdentifierExpr expr, object? value)
    {
        if (value != null)
            return Expression.Constant(value, value.GetType());

        var staticType = expr.StaticType.ClrType;
        if (staticType != typeof(object) &&
            (!staticType.IsValueType || Nullable.GetUnderlyingType(staticType) != null))
        {
            return Expression.Constant(null, staticType);
        }

        return Expression.Constant(null, typeof(object));
    }

    private static void AlignBinaryOperands(TokenType op, ref LinqExpression left, ref LinqExpression right)
    {
        if (TryAlignNullOperand(ref left, ref right))
            return;

        if (NeedsNumericPromotion(op))
        {
            PromoteNumericOperands(ref left, ref right);
            return;
        }

        if (left.Type != right.Type && right.Type.IsAssignableFrom(left.Type))
        {
            left = Expression.Convert(left, right.Type);
            return;
        }

        if (left.Type != right.Type && left.Type.IsAssignableFrom(right.Type))
            right = Expression.Convert(right, left.Type);
    }

    private static bool TryAlignNullOperand(ref LinqExpression left, ref LinqExpression right)
    {
        if (IsNullLiteral(left) && CanRepresentNull(right.Type))
        {
            left = Expression.Constant(null, right.Type);
            return true;
        }

        if (IsNullLiteral(right) && CanRepresentNull(left.Type))
        {
            right = Expression.Constant(null, left.Type);
            return true;
        }

        return false;
    }

    private static bool NeedsNumericPromotion(TokenType op) =>
        op is TokenType.Plus or
            TokenType.Minus or
            TokenType.Star or
            TokenType.Slash or
            TokenType.Percent or
            TokenType.Less or
            TokenType.LessEqual or
            TokenType.Greater or
            TokenType.GreaterEqual;

    private static void PromoteNumericOperands(ref LinqExpression left, ref LinqExpression right)
    {
        var leftType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
        var rightType = Nullable.GetUnderlyingType(right.Type) ?? right.Type;

        if (!TypeHelpers.IsArithmetic(leftType) || !TypeHelpers.IsArithmetic(rightType))
            return;

        var promoted = PromoteNumericType(leftType, rightType);
        if (left.Type != promoted)
            left = Expression.Convert(left, promoted);
        if (right.Type != promoted)
            right = Expression.Convert(right, promoted);
    }

    private static Type PromoteNumericType(Type left, Type right)
    {
        if (left == typeof(double) || right == typeof(double)) return typeof(double);
        if (left == typeof(float) || right == typeof(float)) return typeof(float);
        if (left == typeof(decimal) || right == typeof(decimal)) return typeof(decimal);
        if (left == typeof(ulong) || right == typeof(ulong)) return typeof(ulong);
        if (left == typeof(long) || right == typeof(long)) return typeof(long);
        if (left == typeof(uint) || right == typeof(uint)) return typeof(uint);
        return typeof(int);
    }

    private static LinqExpression EnsureString(LinqExpression expression)
    {
        if (expression.Type == typeof(string))
            return expression;

        if (expression is ConstantExpression { Value: null })
            return Expression.Constant(string.Empty);

        return Expression.Call(StringConcatObjectMethod, EnsureObject(expression));
    }

    private static LinqExpression EnsureTyped(LinqExpression expression, Type targetType) =>
        expression.Type == targetType ? expression : Expression.Convert(expression, targetType);

    private static LinqExpression EnsureObject(LinqExpression expression) =>
        expression.Type == typeof(object) ? expression : Expression.Convert(expression, typeof(object));

    private static LinqExpression EnsureIntIndex(LinqExpression index)
    {
        if (index.Type == typeof(int))
            return index;

        var underlying = Nullable.GetUnderlyingType(index.Type) ?? index.Type;
        return Type.GetTypeCode(underlying) switch
        {
            TypeCode.SByte or
            TypeCode.Byte or
            TypeCode.Int16 or
            TypeCode.UInt16 or
            TypeCode.Int32 or
            TypeCode.UInt32 or
            TypeCode.Int64 or
            TypeCode.UInt64 => Expression.Convert(index, typeof(int)),
            _ => index
        };
    }

    private static bool IsNullLiteral(LinqExpression expression) =>
        expression is ConstantExpression { Value: null };

    private static bool CanRepresentNull(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static class QueryTreeSupport
    {
        internal static void EnsureNoNullConditional(bool nullSafe)
        {
            if (nullSafe)
                throw Unsupported("null-conditional access");
        }

        internal static void EnsureNoForbiddenReflectionType(Type type, string context)
        {
            if (TypeHelpers.IsForbiddenReflectionType(type))
                throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);
        }

        internal static void EnsureSupportedCall(BoundResolvedCallExpr expr)
        {
            if (!EmitHelpers.CanEmitDirectMethodCall(expr, expr.Arguments.Length))
                throw Unsupported("dynamic or unresolved call target");

            if (expr.Callee is not BoundMemberAccessBase memberCallee)
                throw Unsupported("non-member call target");

            EnsureNoNullConditional(memberCallee.NullSafe);

            if (expr.Arguments.Any(static arg => arg is BoundNamedArgumentExpr))
                throw Unsupported("named argument");
            if (expr.Arguments.Any(static arg => arg is BoundOutArgExpr))
                throw Unsupported("out argument");
        }

        internal static AlderException Unsupported(BoundNodeKind kind) =>
            Unsupported(DescribeUnsupportedFeature(kind));

        internal static AlderException Unsupported(string feature) =>
            new(DiagnosticDescriptors.FeatureNotValidInExpressionTree, feature);

        private static string DescribeUnsupportedFeature(BoundNodeKind kind) => kind switch
        {
            BoundNodeKind.MethodGroup => "unresolved method group",
            BoundNodeKind.DynamicMemberAccess => "dynamic member access",
            BoundNodeKind.DynamicIndexAccess => "dynamic index access",
            BoundNodeKind.SwitchExpression => "a switch expression",
            BoundNodeKind.Block => "a block",
            BoundNodeKind.IfStatement => "an if statement",
            BoundNodeKind.WhileStatement => "a while loop",
            BoundNodeKind.ForStatement => "a for loop",
            BoundNodeKind.ForEachStatement => "a foreach loop",
            BoundNodeKind.DoStatement => "a do-while loop",
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
            BoundNodeKind.MultiDimIndexAssignment => "an assignment",
            BoundNodeKind.VariableDeclaration => "a variable declaration",
            BoundNodeKind.TryStatement => "try/catch",
            BoundNodeKind.CollectionCreation => "a collection expression",
            BoundNodeKind.SpreadElement => "spread",
            BoundNodeKind.SliceExpression => "slice",
            BoundNodeKind.Lambda => "a nested lambda",
            BoundNodeKind.InterpolatedString => "an interpolated string",
            BoundNodeKind.ThrowExpression => "a throw expression",
            BoundNodeKind.TupleLiteral => "a tuple expression",
            BoundNodeKind.DeconstructionAssignment => "deconstruction",
            BoundNodeKind.SwitchStatement => "a switch statement",
            BoundNodeKind.ReturnStatement => "a return statement",
            BoundNodeKind.BreakStatement => "a break statement",
            BoundNodeKind.ContinueStatement => "a continue statement",
            BoundNodeKind.GotoStatement => "a goto statement",
            BoundNodeKind.GotoCaseStatement => "a goto case statement",
            BoundNodeKind.GotoDefaultStatement => "a goto default statement",
            BoundNodeKind.Label => "a label",
            BoundNodeKind.ArrayAllocation => "an array creation expression",
            BoundNodeKind.ResolvedMultiDimIndexAccess or
            BoundNodeKind.DynamicMultiDimIndexAccess => "multi-dimensional indexing",
            BoundNodeKind.MultiDimArrayInit => "multi-dimensional array creation",
            BoundNodeKind.NamedArgument => "a named argument",
            BoundNodeKind.OutArgument => "an out argument",
            BoundNodeKind.UsingStatement => "a using statement",
            BoundNodeKind.LockStatement => "a lock statement",
            BoundNodeKind.RangeExpression => "range literals",
            BoundNodeKind.PipelineExpression => "pipeline operator",
            BoundNodeKind.ChainedComparisonOperator => "chained comparison",
            BoundNodeKind.FromEndIndexExpression => "index from end",
            _ => $"expression type '{kind}'"
        };
    }

    private static string DescribeDynamicCallShape(BoundDynamicCallExpr expr)
    {
        foreach (var argument in expr.Arguments)
        {
            if (argument is BoundNamedArgumentExpr)
                return "named argument";
            if (argument is BoundOutArgExpr)
                return "out argument";
        }

        return "direct invocation";
    }
}
