using System.Linq.Expressions;
using System.Reflection;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Translates CsEval AST nodes into typed System.Linq.Expressions trees.
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

    public LinqExpression Emit(Expr expr)
    {
        return expr switch
        {
            LiteralExpr e => EmitLiteral(e),
            IdentifierExpr e => EmitIdentifier(e),
            BinaryExpr e => EmitBinary(e),
            LogicalExpr e => EmitLogical(e),
            UnaryExpr e => EmitUnary(e),
            MemberAccessExpr e => EmitMemberAccess(e),
            CallExpr e => EmitCall(e),
            ConditionalExpr e => EmitConditional(e),
            NullCoalesceExpr e => EmitNullCoalesce(e),
            CastExpr e => EmitCast(e),
            CheckedExpr e => EmitChecked(e),
            IsPatternExpr e => EmitIsPattern(e),
            IndexAccessExpr e => EmitIndexAccess(e),
            ObjectCreationExpr e => EmitObjectCreation(e),
            TypeReferenceExpr => throw new CsEvalException(
                "Expression tree cannot contain a standalone type reference"),

            // Unsupported nodes with descriptive messages
            SwitchExpressionExpr => throw new CsEvalException(
                "Expression tree cannot contain a switch expression"),
            BlockExpr => throw new CsEvalException(
                "Expression tree cannot contain a block"),
            IfStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain an if statement"),
            WhileStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a while loop"),
            ForStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a for loop"),
            ForEachStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a foreach loop"),
            DoWhileStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a do-while loop"),
            AssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            MemberAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            IndexAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            CompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            MemberCompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            IndexCompoundAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            NullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            MemberNullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            IndexNullCoalesceAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            IncrementDecrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            MemberIncrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            IndexIncrementExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            VariableDeclExpr => throw new CsEvalException(
                "Expression tree cannot contain a variable declaration"),
            TryCatchFinallyExpr => throw new CsEvalException(
                "Expression tree cannot contain try/catch"),
            ArrayLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain a collection expression"),
            ObjectLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain an object literal"),
            SpreadExpr => throw new CsEvalException(
                "Expression tree cannot contain spread"),
            SliceExpr => throw new CsEvalException(
                "Expression tree cannot contain slice"),
            LambdaExpr => throw new CsEvalException(
                "Expression tree cannot contain a nested lambda"),
            InterpolatedStringExpr => throw new CsEvalException(
                "Expression tree cannot contain an interpolated string"),
            ThrowExpr => throw new CsEvalException(
                "Expression tree cannot contain a throw expression"),
            ThrowStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a throw statement"),
            NewExpr => throw new CsEvalException(
                "Expression tree cannot contain an anonymous object creation"),
            TupleExpr => throw new CsEvalException(
                "Expression tree cannot contain a tuple expression"),
            DeconstructionExpr => throw new CsEvalException(
                "Expression tree cannot contain deconstruction"),
            SwitchStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a switch statement"),
            ReturnExpr => throw new CsEvalException(
                "Expression tree cannot contain a return statement"),
            BreakExpr => throw new CsEvalException(
                "Expression tree cannot contain a break statement"),
            ContinueExpr => throw new CsEvalException(
                "Expression tree cannot contain a continue statement"),
            AsExpr => throw new CsEvalException(
                "Expression tree cannot contain an 'as' expression"),
            DefaultExpr => throw new CsEvalException(
                "Expression tree cannot contain a default expression"),
            NameofExpr => throw new CsEvalException(
                "Expression tree cannot contain a nameof expression"),
            TypeofExpr => throw new CsEvalException(
                "Expression tree cannot contain a typeof expression"),
            SizeofExpr => throw new CsEvalException(
                "Expression tree cannot contain a sizeof expression"),
            TypedArrayCreationExpr => throw new CsEvalException(
                "Expression tree cannot contain an array creation expression"),
            TypedArrayLiteralExpr => throw new CsEvalException(
                "Expression tree cannot contain an array creation expression"),
            MultiDimIndexAccessExpr => throw new CsEvalException(
                "Expression tree cannot contain multi-dimensional indexing"),
            MultiDimTypedArrayCreationExpr => throw new CsEvalException(
                "Expression tree cannot contain multi-dimensional array creation"),
            MultiDimIndexAssignExpr => throw new CsEvalException(
                "Expression tree cannot contain an assignment"),
            NamedArgumentExpr => throw new CsEvalException(
                "Expression tree cannot contain a named argument"),
            OutArgExpr => throw new CsEvalException(
                "Expression tree cannot contain an out argument"),
            UsingStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a using statement"),
            LockStatementExpr => throw new CsEvalException(
                "Expression tree cannot contain a lock statement"),

            // Polyglot Extended Features -- explicit rejection with descriptive messages
            RangeExpr => throw new CsEvalException(
                "Expression tree output not supported for range literals"),
            PipelineExpr => throw new CsEvalException(
                "Expression tree output not supported for pipeline operator"),
            ChainedComparisonExpr => throw new CsEvalException(
                "Expression tree output not supported for chained comparison"),

            _ => throw new CsEvalException(
                $"Expression tree cannot contain this expression type: {expr.GetType().Name}")
        };
    }

    private LinqExpression EmitLiteral(LiteralExpr expr)
    {
        if (expr.Value is null)
            return LinqExpression.Constant(null, typeof(object));

        return LinqExpression.Constant(expr.Value, expr.Value.GetType());
    }

    private LinqExpression EmitIdentifier(IdentifierExpr expr)
    {
        var name = expr.Name.Lexeme;

        if (_parameterScope.TryGetValue(name, out var param))
            return param;

        if (_engineVariables.TryGetValue(name, out var value))
            return LinqExpression.Constant(value, value?.GetType() ?? typeof(object));

        throw new CsEvalException($"The name '{name}' does not exist in the current context");
    }

    private LinqExpression EmitBinary(BinaryExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);

        // String concatenation: if either operand is string, use string.Concat
        if (expr.Op.Type == TokenType.Plus && IsStringConcatenation(left, right))
        {
            var leftStr = EnsureString(left);
            var rightStr = EnsureString(right);
            return LinqExpression.Call(StringConcat2, leftStr, rightStr);
        }

        // Numeric promotion for arithmetic and comparison operations
        if (NeedsNumericPromotion(expr.Op.Type))
            PromoteNumericOperands(ref left, ref right);

        return expr.Op.Type switch
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
                $"Expression tree cannot contain operator '{expr.Op.Lexeme}'")
        };
    }

    private LinqExpression EmitLogical(LogicalExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);

        return expr.Op.Type switch
        {
            TokenType.AmpAmp => LinqExpression.AndAlso(left, right),
            TokenType.PipePipe => LinqExpression.OrElse(left, right),
            _ => throw new CsEvalException(
                $"Expression tree cannot contain operator '{expr.Op.Lexeme}'")
        };
    }

    private LinqExpression EmitUnary(UnaryExpr expr)
    {
        var operand = Emit(expr.Right);

        return expr.Op.Type switch
        {
            TokenType.Minus => _isChecked ? LinqExpression.NegateChecked(operand) : LinqExpression.Negate(operand),
            TokenType.Plus => operand,
            TokenType.Bang => LinqExpression.Not(operand),
            TokenType.Tilde => LinqExpression.Not(operand),
            _ => throw new CsEvalException(
                $"Expression tree cannot contain unary operator '{expr.Op.Lexeme}'")
        };
    }

    private LinqExpression EmitMemberAccess(MemberAccessExpr expr)
    {
        if (expr.NullSafe)
            throw new CsEvalException("Expression tree cannot contain null-conditional access");

        // Handle static member access: TypeReferenceExpr.Member
        if (expr.Object is TypeReferenceExpr typeRef)
        {
            var type = ResolveTypeFromToken(typeRef.TypeToken);
            return EmitStaticMemberAccess(type, expr.Name.Lexeme);
        }

        var obj = Emit(expr.Object);
        var memberName = expr.Name.Lexeme;

        var property = obj.Type.GetProperty(memberName,
            BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
            return LinqExpression.Property(obj, property);

        var field = obj.Type.GetField(memberName,
            BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
            return LinqExpression.Field(obj, field);

        // Try static members on the instance type
        var staticProp = obj.Type.GetProperty(memberName,
            BindingFlags.Public | BindingFlags.Static);
        if (staticProp != null)
            return LinqExpression.Property(null, staticProp);

        var staticField = obj.Type.GetField(memberName,
            BindingFlags.Public | BindingFlags.Static);
        if (staticField != null)
            return LinqExpression.Field(null, staticField);

        throw new CsEvalException(
            $"'{obj.Type.Name}' does not contain a definition for '{memberName}'");
    }

    private LinqExpression EmitStaticMemberAccess(Type type, string memberName)
    {
        var property = type.GetProperty(memberName,
            BindingFlags.Public | BindingFlags.Static);
        if (property != null)
            return LinqExpression.Property(null, property);

        var field = type.GetField(memberName,
            BindingFlags.Public | BindingFlags.Static);
        if (field != null)
            return LinqExpression.Field(null, field);

        throw new CsEvalException(
            $"'{type.Name}' does not contain a static definition for '{memberName}'");
    }

    private LinqExpression EmitCall(CallExpr expr)
    {
        var args = expr.Arguments.Select(Emit).ToArray();
        var argTypes = args.Select(a => a.Type).ToArray();

        // Instance method call: callee is MemberAccessExpr
        if (expr.Callee is MemberAccessExpr memberAccess)
        {
            if (memberAccess.NullSafe)
                throw new CsEvalException(
                    "Expression tree cannot contain null-conditional access");

            var methodName = memberAccess.Name.Lexeme;

            // Static method call: object is TypeReferenceExpr
            if (memberAccess.Object is TypeReferenceExpr typeRef)
            {
                var type = ResolveTypeFromToken(typeRef.TypeToken);
                return EmitStaticMethodCall(type, methodName, args, argTypes);
            }

            // Static method call: chained member access reaching a type (e.g., System.Math.Abs)
            if (TryResolveTypeFromMemberChain(memberAccess.Object, out var staticType))
            {
                return EmitStaticMethodCall(staticType!, methodName, args, argTypes);
            }

            var obj = Emit(memberAccess.Object);
            return EmitInstanceMethodCall(obj, methodName, args, argTypes);
        }

        // Direct function call via identifier -- check if it resolves to a static type method
        if (expr.Callee is IdentifierExpr idExpr)
        {
            var name = idExpr.Name.Lexeme;

            // Try as a delegate variable
            if (_parameterScope.TryGetValue(name, out var paramExpr) &&
                typeof(Delegate).IsAssignableFrom(paramExpr.Type))
            {
                var invokeMethod = paramExpr.Type.GetMethod("Invoke")!;
                return LinqExpression.Invoke(paramExpr, args);
            }

            throw new CsEvalException(
                $"Expression tree cannot contain a call to unresolved function '{name}'");
        }

        throw new CsEvalException(
            "Expression tree cannot contain this call expression");
    }

    private LinqExpression EmitInstanceMethodCall(
        LinqExpression obj, string methodName,
        LinqExpression[] args, Type[] argTypes)
    {
        var method = ResolveMethod(obj.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance, argTypes);

        if (method == null)
            throw new CsEvalException(
                $"'{obj.Type.Name}' does not contain a method '{methodName}' " +
                $"matching the given arguments");

        args = CoerceArguments(method, args);
        return LinqExpression.Call(obj, method, args);
    }

    private LinqExpression EmitStaticMethodCall(
        Type type, string methodName,
        LinqExpression[] args, Type[] argTypes)
    {
        var method = ResolveMethod(type, methodName,
            BindingFlags.Public | BindingFlags.Static, argTypes);

        if (method == null)
            throw new CsEvalException(
                $"'{type.Name}' does not contain a static method '{methodName}' " +
                $"matching the given arguments");

        args = CoerceArguments(method, args);
        return LinqExpression.Call(method, args);
    }

    private LinqExpression EmitConditional(ConditionalExpr expr)
    {
        var test = Emit(expr.Condition);
        var ifTrue = Emit(expr.ThenBranch);
        var ifFalse = Emit(expr.ElseBranch);

        // Ensure both branches have compatible types
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

    private LinqExpression EmitNullCoalesce(NullCoalesceExpr expr)
    {
        var left = Emit(expr.Left);
        var right = Emit(expr.Right);
        return LinqExpression.Coalesce(left, right);
    }

    private LinqExpression EmitChecked(CheckedExpr expr)
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

    private LinqExpression EmitCast(CastExpr expr)
    {
        var operand = Emit(expr.Expression);
        var targetType = ResolveTypeFromToken(expr.TargetType);
        return _isChecked
            ? LinqExpression.ConvertChecked(operand, targetType)
            : LinqExpression.Convert(operand, targetType);
    }

    private LinqExpression EmitIsPattern(IsPatternExpr expr)
    {
        if (expr.Pattern is TypePattern typePattern)
        {
            var operand = Emit(expr.Expression);
            var type = ResolveTypeFromToken(typePattern.TypeToken);
            return LinqExpression.TypeIs(operand, type);
        }

        throw new CsEvalException("Expression tree cannot contain pattern matching");
    }

    private LinqExpression EmitIndexAccess(IndexAccessExpr expr)
    {
        if (expr.NullSafe)
            throw new CsEvalException("Expression tree cannot contain null-conditional access");

        var obj = Emit(expr.Object);
        var index = Emit(expr.Index);

        if (obj.Type.IsArray)
            return LinqExpression.ArrayIndex(obj, index);

        // Look for an indexer property (Item)
        var indexer = obj.Type.GetProperty("Item");
        if (indexer != null)
        {
            // Coerce index type to match indexer parameter
            var indexerParams = indexer.GetIndexParameters();
            if (indexerParams.Length == 1 && index.Type != indexerParams[0].ParameterType)
                index = LinqExpression.Convert(index, indexerParams[0].ParameterType);

            return LinqExpression.MakeIndex(obj, indexer, [index]);
        }

        throw new CsEvalException(
            $"'{obj.Type.Name}' does not have an indexer");
    }

    private LinqExpression EmitObjectCreation(ObjectCreationExpr expr)
    {
        if (expr.Initializer != null)
            throw new CsEvalException(
                "Expression tree cannot contain object initializer");

        var type = ResolveTypeByName(expr.TypeName);

        var args = expr.Arguments.Select(Emit).ToArray();
        var argTypes = args.Select(a => a.Type).ToArray();

        var ctor = type.GetConstructor(argTypes);
        if (ctor == null)
        {
            // Try with implicit numeric conversions
            ctor = FindCompatibleConstructor(type, argTypes);
            if (ctor == null)
                throw new CsEvalException(
                    $"'{type.Name}' does not contain a constructor matching the given arguments");

            args = CoerceConstructorArguments(ctor, args);
        }

        return LinqExpression.New(ctor, args);
    }

    #region Type Resolution Helpers

    private Type ResolveTypeFromToken(Token token)
    {
        return ResolveTypeByName(token.Lexeme);
    }

    private Type ResolveTypeByName(string typeName)
    {
        var resolved = _typeResolver.TryResolveType(typeName);
        if (resolved != null)
            return resolved;

        throw new CsEvalException(
            $"The type or namespace name '{typeName}' could not be found");
    }

    /// <summary>
    /// Tries to resolve a chain of MemberAccessExpr nodes to a static type.
    /// For example, System.Math resolves to typeof(Math).
    /// </summary>
    private bool TryResolveTypeFromMemberChain(Expr expr, out Type? type)
    {
        type = null;
        var name = BuildMemberChainName(expr);
        if (name == null)
            return false;

        type = _typeResolver.TryResolveType(name);
        return type != null;
    }

    private static string? BuildMemberChainName(Expr expr)
    {
        return expr switch
        {
            IdentifierExpr id => id.Name.Lexeme,
            TypeReferenceExpr typeRef => typeRef.TypeToken.Lexeme,
            MemberAccessExpr ma => BuildMemberChainName(ma.Object) is { } objName
                ? $"{objName}.{ma.Name.Lexeme}"
                : null,
            _ => null
        };
    }

    #endregion

    #region Method Resolution Helpers

    private static MethodInfo? ResolveMethod(
        Type type, string name, BindingFlags flags, Type[] argTypes)
    {
        // Try exact match first
        var method = type.GetMethod(name, flags, argTypes);
        if (method != null)
            return method;

        // Fall back to scanning methods with compatible parameter counts
        var candidates = type.GetMethods(flags)
            .Where(m => m.Name == name && m.GetParameters().Length == argTypes.Length)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var parameters = candidate.GetParameters();
            var compatible = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!IsAssignableOrConvertible(argTypes[i], parameters[i].ParameterType))
                {
                    compatible = false;
                    break;
                }
            }
            if (compatible)
                return candidate;
        }

        return null;
    }

    private static bool IsAssignableOrConvertible(Type from, Type to)
    {
        return TypeHelpers.CanAssignOrImplicitlyConvert(from, to);
    }

    private static LinqExpression[] CoerceArguments(MethodInfo method, LinqExpression[] args)
    {
        var parameters = method.GetParameters();
        var result = new LinqExpression[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Type != parameters[i].ParameterType &&
                !parameters[i].ParameterType.IsAssignableFrom(args[i].Type))
            {
                result[i] = LinqExpression.Convert(args[i], parameters[i].ParameterType);
            }
            else
            {
                result[i] = args[i];
            }
        }
        return result;
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
            if (parameters.Length != argTypes.Length) continue;

            var compatible = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!IsAssignableOrConvertible(argTypes[i], parameters[i].ParameterType))
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

    #endregion

    #region Numeric Promotion Helpers

    private static bool NeedsNumericPromotion(TokenType op) => op is
        TokenType.Plus or TokenType.Minus or TokenType.Star or
        TokenType.Slash or TokenType.Percent or
        TokenType.EqualEqual or TokenType.BangEqual or
        TokenType.Less or TokenType.LessEqual or
        TokenType.Greater or TokenType.GreaterEqual;

    private static void PromoteNumericOperands(
        ref LinqExpression left, ref LinqExpression right)
    {
        if (left.Type == right.Type)
            return;

        var promoted = GetNumericPromotionType(left.Type, right.Type);
        if (promoted == null)
            return;

        if (left.Type != promoted)
            left = LinqExpression.Convert(left, promoted);
        if (right.Type != promoted)
            right = LinqExpression.Convert(right, promoted);
    }

    /// <summary>
    /// Returns the promoted type for binary operations following C# numeric promotion rules.
    /// Returns null if the types are not both numeric.
    /// </summary>
    private static Type? GetNumericPromotionType(Type left, Type right)
    {
        return TypeHelpers.TryGetBinaryNumericPromotionType(left, right);
    }

    #endregion

    #region String Helpers

    private static bool IsStringConcatenation(LinqExpression left, LinqExpression right)
    {
        return left.Type == typeof(string) || right.Type == typeof(string);
    }

    /// <summary>
    /// Ensures an expression produces a string, inserting a ToString() call if needed.
    /// </summary>
    private static LinqExpression EnsureString(LinqExpression expr)
    {
        if (expr.Type == typeof(string))
            return expr;

        // For value types, box first then call ToString; for reference types, call directly
        if (expr.Type.IsValueType)
        {
            var boxed = LinqExpression.Convert(expr, typeof(object));
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
            return LinqExpression.Call(boxed, toStringMethod);
        }

        var objToString = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
        return LinqExpression.Call(expr, objToString);
    }

    #endregion

    #region Type Compatibility Helpers

    /// <summary>
    /// Finds a common type for conditional expression branches.
    /// Uses numeric promotion for numeric types, or IsAssignableFrom for reference types.
    /// </summary>
    private static Type? GetCommonType(Type left, Type right)
    {
        // Numeric promotion
        var promoted = GetNumericPromotionType(left, right);
        if (promoted != null)
            return promoted;

        // Reference type compatibility
        if (left.IsAssignableFrom(right))
            return left;
        if (right.IsAssignableFrom(left))
            return right;

        // Both are reference types -- try common base
        if (!left.IsValueType && !right.IsValueType)
            return typeof(object);

        return null;
    }

    #endregion
}
