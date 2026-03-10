using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Binding.Plans;
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
        return expr switch
        {
            BoundLiteralExpr e => EmitLiteral(e),
            BoundIdentifierExpr e => EmitIdentifier(e),
            BoundBinaryExpr e => EmitBinary(e),
            BoundLogicalExpr e => EmitLogical(e),
            BoundUnaryExpr e => EmitUnary(e),
            BoundMemberAccessExpr e => EmitMemberAccess(e),
            BoundCallExpr e => EmitCall(e),
            BoundConditionalExpr e => EmitConditional(e),
            BoundNullCoalesceExpr e => EmitNullCoalesce(e),
            BoundCastExpr e => EmitCast(e),
            BoundCheckedExpr e => EmitChecked(e),
            BoundIsPatternExpr e => EmitIsPattern(e),
            BoundIndexAccessExpr e => EmitIndexAccess(e),
            BoundObjectCreationExpr e => EmitObjectCreation(e),

            // Unsupported nodes with descriptive messages
            BoundSwitchExpressionExpr => throw new CsEvalException(
                "Expression tree cannot contain a switch expression"),
            BoundBlockExpr => throw new CsEvalException(
                "Expression tree cannot contain a block"),
            BoundIfStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain an if statement"),
            BoundWhileExpr => throw new CsEvalException(
                "Expression tree cannot contain a while loop"),
            BoundForExpr => throw new CsEvalException(
                "Expression tree cannot contain a for loop"),
            BoundForEachExpr => throw new CsEvalException(
                "Expression tree cannot contain a foreach loop"),
            BoundDoWhileExpr => throw new CsEvalException(
                "Expression tree cannot contain a do-while loop"),
            BoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundMemberAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundIndexAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundCompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundMemberCompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundIndexCompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundNullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundMemberNullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundIndexNullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundIncrementDecrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundMemberIncrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundIndexIncrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundVariableDeclExpr => throw new CsEvalException(
                "Expression tree cannot contain a variable declaration"),
            BoundTryCatchFinallyExpr => throw new CsEvalException(
                "Expression tree cannot contain try/catch"),
            BoundArrayLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain a collection expression"),
            BoundObjectLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain an object literal"),
            BoundSpreadExpr => throw new CsEvalException(
                "Expression tree cannot contain spread"),
            BoundSliceExpr => throw new CsEvalException(
                "Expression tree cannot contain slice"),
            BoundLambdaExpr => throw new CsEvalException(
                "Expression tree cannot contain a nested lambda"),
            BoundInterpolatedStringExpr => throw new CsEvalException(
                "Expression tree cannot contain an interpolated string"),
            BoundThrowExpr => throw new CsEvalException(
                "Expression tree cannot contain a throw expression"),
            BoundThrowStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a throw statement"),
            BoundTupleExpr => throw new CsEvalException(
                "Expression tree cannot contain a tuple expression"),
            BoundDeconstructionExpr => throw new CsEvalException(
                "Expression tree cannot contain deconstruction"),
            BoundSwitchStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a switch statement"),
            BoundReturnExpr => throw new CsEvalException(
                "Expression tree cannot contain a return statement"),
            BoundBreakExpr => throw new CsEvalException(
                "Expression tree cannot contain a break statement"),
            BoundContinueExpr => throw new CsEvalException(
                "Expression tree cannot contain a continue statement"),
            BoundAsExpr => throw new CsEvalException(
                "Expression tree cannot contain an 'as' expression"),
            BoundTypedArrayCreationExpr => throw new CsEvalException(
                "Expression tree cannot contain an array creation expression"),
            BoundTypedArrayLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain an array creation expression"),
            BoundMultiDimIndexAccessExpr => throw new CsEvalException(
                "Expression tree cannot contain multi-dimensional indexing"),
            BoundMultiDimTypedArrayCreationExpr => throw new CsEvalException(
                "Expression tree cannot contain multi-dimensional array creation"),
            BoundMultiDimIndexAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            BoundNamedArgumentExpr => throw new CsEvalException(
                "Expression tree cannot contain a named argument"),
            BoundOutArgExpr => throw new CsEvalException(
                "Expression tree cannot contain an out argument"),
            BoundUsingStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a using statement"),
            BoundLockStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a lock statement"),

            // Polyglot Extended Features -- explicit rejection with descriptive messages
            BoundRangeExpr => throw new CsEvalException(
                "Expression tree output not supported for range literals"),
            BoundPipelineExpr => throw new CsEvalException(
                "Expression tree output not supported for pipeline operator"),
            BoundChainedComparisonExpr => throw new CsEvalException(
                "Expression tree output not supported for chained comparison"),
            BoundInvokeExpr => throw new CsEvalException(
                "Expression tree cannot contain this call expression"),

            _ => throw new CsEvalException(
                $"Expression tree cannot contain this expression type: {expr.GetType().Name}")
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

        throw new CsEvalException($"The name '{name}' does not exist in the current context");
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
            _ => throw new CsEvalException(
                $"Expression tree cannot contain operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
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
            _ => throw new CsEvalException(
                $"Expression tree cannot contain operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
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
            _ => throw new CsEvalException(
                $"Expression tree cannot contain unary operator '{TokenLexemes.GetCanonical(expr.Operator)}'")
        };
    }

    private LinqExpression EmitMemberAccess(BoundMemberAccessExpr expr)
    {
        if (expr.NullSafe)
            throw new CsEvalException("Expression tree cannot contain null-conditional access");

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
            throw new CsEvalException(
                "Expression tree cannot contain unresolved method groups");

        throw new CsEvalException(
            $"'{target.Type.Name}' does not contain a definition for '{expr.MemberName}'");
    }

    private LinqExpression EmitCall(BoundCallExpr expr)
    {
        var method = expr.Plan.SelectedMethod;
        var args = new LinqExpression[expr.Arguments.Length];
        for (var i = 0; i < expr.Arguments.Length; i++)
        {
            var arg = Emit(expr.Arguments[i]);
            var conversion = expr.Plan.ArgumentConversions[i];
            args[i] = conversion.IsIdentity || arg.Type == conversion.TargetType
                ? arg
                : LinqExpression.Convert(arg, conversion.TargetType);
        }

        if (expr.Plan.IsStaticCall)
            return LinqExpression.Call(method, args);

        if (expr.Callee is not BoundMemberAccessExpr memberCallee)
            throw new CsEvalException("Expression tree cannot contain this call expression");

        if (memberCallee.NullSafe)
            throw new CsEvalException("Expression tree cannot contain null-conditional access");

        var target = Emit(memberCallee.Target);
        return LinqExpression.Call(target, method, args);
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

        throw new CsEvalException("Expression tree cannot contain pattern matching");
    }

    private LinqExpression EmitIndexAccess(BoundIndexAccessExpr expr)
    {
        if (expr.NullSafe)
            throw new CsEvalException("Expression tree cannot contain null-conditional access");

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

        throw new CsEvalException($"'{obj.Type.Name}' does not have an indexer");
    }

    private LinqExpression EmitObjectCreation(BoundObjectCreationExpr expr)
    {
        if (expr.InitializerEntries.Length > 0)
            throw new CsEvalException("Expression tree cannot contain object initializer");

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
                    $"'{targetType.Name}' does not contain a constructor matching the given arguments");
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
}
