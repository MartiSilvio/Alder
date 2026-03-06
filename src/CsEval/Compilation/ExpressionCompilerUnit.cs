using System.Reflection;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles expression nodes (literals, binary, unary, member access, calls, lambdas, etc.)
/// to Expression Trees. Receives shared state via CompilerContext.
/// </summary>
internal sealed class ExpressionCompilerUnit
{
    private readonly CompilerContext _ctx;
    private readonly PatternCompilerUnit _patternUnit;

    // Lazily set references for cross-unit dispatch
    private ControlFlowCompilerUnit? _controlUnit;

    private static readonly MethodInfo InOperatorMethod =
        typeof(Operators).GetMethod(nameof(Operators.InOperator), [typeof(object), typeof(object)])!;
    private static readonly MethodInfo LikeMethod =
        typeof(Operators).GetMethod(nameof(Operators.Like), [typeof(object), typeof(object)])!;
    private static readonly MethodInfo StringStartsWithOrdinalMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringEndsWithOrdinalMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringContainsOrdinalMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StringEqualsOrdinalMethod =
        typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;

    internal ExpressionCompilerUnit(CompilerContext ctx, PatternCompilerUnit patternUnit)
    {
        _ctx = ctx;
        _patternUnit = patternUnit;
    }

    internal void SetControlFlowUnit(ControlFlowCompilerUnit controlUnit)
    {
        _controlUnit = controlUnit;
    }

    /// <summary>
    /// Compile a sub-expression by dispatching through the central Compile method.
    /// </summary>
    internal LinqExpression Compile(Expr expr) =>
        CompilerContext.Compile(_ctx, expr, this, _controlUnit!, _patternUnit);

    private (LinqExpression Expression, Type KnownType) CompileTyped(Expr expr)
    {
        var knownType = _ctx.TypeInferrer.Infer(expr);
        if (TryCompileTypedIdentifier(expr, knownType, out var typedIdentifier))
            return (typedIdentifier, knownType);

        var compiled = Compile(expr);
        if (knownType == typeof(object))
            return (compiled, compiled.Type);

        if (TryUnboxObjectConversion(compiled, out var unboxed))
            return (unboxed, knownType);

        if (compiled.Type == knownType || compiled.Type == typeof(object))
            return (compiled, knownType);

        return (compiled, compiled.Type);
    }

    private LinqExpression EmitBinaryOpCall(OperatorRegistry.BinaryOpInfo info, LinqExpression left, LinqExpression right)
    {
        left = EnsureObjectExpression(left);
        right = EnsureObjectExpression(right);

        var checkedConst = LinqExpression.Constant(_ctx.IsChecked);
        LinqExpression call = info.Signature switch
        {
            OperatorRegistry.BinaryOpSignature.TwoArgs =>
                LinqExpression.Call(info.Method, left, right),
            OperatorRegistry.BinaryOpSignature.WithOptions =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContext =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam, _ctx.CurrentContext),
            OperatorRegistry.BinaryOpSignature.TwoArgsChecked =>
                LinqExpression.Call(info.Method, left, right, checkedConst),
            OperatorRegistry.BinaryOpSignature.WithOptionsChecked =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam, checkedConst),
            OperatorRegistry.BinaryOpSignature.WithOptionsAndContextChecked =>
                LinqExpression.Call(info.Method, left, right, _ctx.OptionsParam, _ctx.CurrentContext, checkedConst),
            _ => throw new NotSupportedException($"Unknown binary op signature {info.Signature}")
        };

        LinqExpression result = call;

        if (info.NegateBooleanResult)
        {
            var boolCall = result.Type == typeof(bool)
                ? result
                : LinqExpression.Convert(result, typeof(bool));
            result = LinqExpression.Not(boolCall);
        }

        return result.Type == typeof(object)
            ? result
            : LinqExpression.Convert(result, typeof(object));
    }

    private bool TryCompileTypedIdentifier(Expr expr, Type knownType, out LinqExpression compiled)
    {
        compiled = null!;
        if (knownType == typeof(object) || expr is not IdentifierExpr id)
            return false;

        var name = id.Name.Lexeme;
        var hasFunctionOrModuleShadow = _ctx.Context.Functions.ContainsKey(name) || _ctx.Context.Modules.ContainsKey(name);
        if (hasFunctionOrModuleShadow)
            return false;

        if (_ctx.Context.TryGetVariableType(name, out var variableType) &&
            variableType == knownType)
        {
            var typedVariableGetter = CompilerContext.GetVariableTypedMethodFor(knownType);
            var directRead = LinqExpression.Call(
                typedVariableGetter,
                LinqExpression.Constant(name),
                _ctx.CurrentContext);
            compiled = EmitLazyTypedIdentifierRead(name, knownType, directRead);
            return true;
        }

        var typedResolver = CompilerContext.GetResolveIdentifierTypedMethod(knownType);
        var resolvedRead = LinqExpression.Call(
            typedResolver,
            LinqExpression.Constant(name),
            _ctx.CurrentContext,
            _ctx.OptionsParam);
        compiled = EmitLazyTypedIdentifierRead(name, knownType, resolvedRead);
        return true;
    }

    private LinqExpression EmitLazyTypedIdentifierRead(string name, Type valueType, LinqExpression directRead)
    {
        if (!_ctx.TryGetOrCreateLazyIdentifierSlot(name, valueType, out var valueVar, out var initializedVar))
            return directRead;

        return LinqExpression.Condition(
            initializedVar,
            valueVar,
            LinqExpression.Block(
                LinqExpression.Assign(valueVar, directRead),
                LinqExpression.Assign(initializedVar, LinqExpression.Constant(true)),
                valueVar));
    }

    private static bool TryUnboxObjectConversion(LinqExpression compiled, out LinqExpression unboxed)
    {
        if (compiled is System.Linq.Expressions.UnaryExpression
            {
                NodeType: System.Linq.Expressions.ExpressionType.Convert,
                Type: var conversionType
            } unary &&
            conversionType == typeof(object))
        {
            unboxed = unary.Operand;
            return true;
        }

        unboxed = null!;
        return false;
    }

    private static LinqExpression EnsureObjectExpression(LinqExpression expression) =>
        expression.Type == typeof(object)
            ? expression
            : LinqExpression.Convert(expression, typeof(object));

    internal LinqExpression CompileLiteral(LiteralExpr lit)
    {
        if (lit.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        // Box value types to object
        return LinqExpression.Convert(
            LinqExpression.Constant(lit.Value, lit.Value.GetType()),
            typeof(object));
    }

    internal LinqExpression CompileIdentifier(IdentifierExpr id)
    {
        return LinqExpression.Call(
            CompilerContext.ResolveIdentifierMethod,
            LinqExpression.Constant(id.Name.Lexeme),
            _ctx.CurrentContext,
            _ctx.OptionsParam);
    }

    internal LinqExpression CompileTypeReference(TypeReferenceExpr typeRef)
    {
        // Return the Type object for static member access via context's TypeResolver
        return LinqExpression.Convert(
            LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(typeRef.TypeToken.Lexeme)),
            typeof(object));
    }

    internal LinqExpression CompileTypeof(TypeofExpr expr)
    {
        // Resolve the type at compile time using the context's TypeResolver and embed as constant
        var resolvedType = _ctx.Context.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
        return LinqExpression.Constant(resolvedType, typeof(object));
    }

    internal LinqExpression CompileSizeof(SizeofExpr expr)
    {
        int size = TypeHelpers.GetSizeOf(expr.TypeName);
        return LinqExpression.Constant((object)size, typeof(object));
    }

    internal LinqExpression CompileThrow(ThrowExpr expr)
    {
        var exceptionExpr = LinqExpression.Call(
            CompilerContext.ValidateThrowOperandMethod,
            Compile(expr.Expression));
        // LinqExpression.Throw returns void, but we need object return type.
        // Wrap in block with unreachable default value to satisfy the type system.
        return LinqExpression.Block(
            typeof(object),
            LinqExpression.Throw(exceptionExpr),
            LinqExpression.Default(typeof(object)));
    }

    /// <summary>
    /// Compiles parameterless throw; (rethrow) using the Expression Trees rethrow instruction.
    /// ECMA-334 §13.10.6 -- only valid inside a catch block body.
    /// </summary>
    internal LinqExpression CompileThrowStatement()
    {
        if (_ctx.CatchDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.ThrowOutsideCatch);

        // Expression.Rethrow generates the IL rethrow instruction.
        // Must be typed to match the try/catch return type (typeof(object)).
        return LinqExpression.Rethrow(typeof(object));
    }

    internal LinqExpression CompileObjectCreation(ObjectCreationExpr expr)
    {
        // Compile arguments into an object[] array
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Arguments.Select(Compile));

        // Resolve type via context's TypeResolver then call RuntimeHelpers.InvokeConstructor(type, args)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.TypeName));

        LinqExpression result = LinqExpression.Call(
            CompilerContext.InvokeConstructorMethod,
            resolvedType,
            argsInit);

        if (expr.Initializer != null)
        {
            // Apply each initializer entry sequentially
            var objVar = LinqExpression.Variable(typeof(object), "initObj");
            var statements = new List<LinqExpression> { LinqExpression.Assign(objVar, result) };

            foreach (var entry in expr.Initializer.Entries)
            {
                var value = Compile(entry.Value);
                if (entry.PropertyName != null)
                {
                    statements.Add(LinqExpression.Call(
                        CompilerContext.ApplyPropertyInitializerMethod,
                        objVar,
                        LinqExpression.Constant(entry.PropertyName),
                        value,
                        _ctx.OptionsParam,
                        _ctx.CurrentContext));
                }
                else
                {
                    statements.Add(LinqExpression.Call(
                        CompilerContext.ApplyCollectionInitializerMethod,
                        objVar,
                        value));
                }
            }

            statements.Add(objVar);
            result = LinqExpression.Block(typeof(object), [objVar], statements);
        }

        return result;
    }

    internal LinqExpression CompileTypedArrayCreation(TypedArrayCreationExpr expr)
    {
        var size = Compile(expr.Size);

        // Resolve element type via context's TypeResolver then call RuntimeHelpers.CreateTypedArray(elementType, sizeValue)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        return LinqExpression.Call(
            CompilerContext.CreateTypedArrayFromTypeNameMethod,
            resolvedType,
            size);
    }

    internal LinqExpression CompileMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr)
    {
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        var sizesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Sizes.Select(s => Compile(s)));

        return LinqExpression.Call(
            CompilerContext.CreateMultiDimArrayMethod,
            resolvedType,
            sizesArray);
    }

    internal LinqExpression CompileMultiDimIndexAccess(MultiDimIndexAccessExpr expr)
    {
        var obj = Compile(expr.Object);
        var indicesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Indices.Select(i => Compile(i)));

        if (expr.NullSafe)
        {
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, obj),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(CompilerContext.MultiDimArrayGetMethod, targetVar, indicesArray)));
        }

        return LinqExpression.Call(CompilerContext.MultiDimArrayGetMethod, obj, indicesArray);
    }

    internal LinqExpression CompileMultiDimIndexAssign(MultiDimIndexAssignExpr expr)
    {
        var obj = Compile(expr.Object);
        var indicesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Indices.Select(i => Compile(i)));
        var value = Compile(expr.Value);

        return LinqExpression.Call(CompilerContext.MultiDimArraySetMethod, obj, indicesArray, value);
    }

    internal LinqExpression CompileTypedArrayLiteral(TypedArrayLiteralExpr expr)
    {
        // Compile the array literal elements (returns a typed array from CreateTypedArray)
        var arrayLiteral = CompileArrayLiteral(expr.Elements);

        // Resolve the target element type
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        // Convert the source array to the typed array T[]
        // ConvertArrayToTyped accepts object (any Array) so no cast to object?[] needed
        return LinqExpression.Call(
            CompilerContext.ConvertArrayToTypedMethod,
            arrayLiteral,
            resolvedType);
    }

    internal LinqExpression CompileTuple(TupleExpr expr)
    {
        // Compile each element expression into an object[] array
        var elementsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Elements.Select(e => Compile(e.Expression)));

        // Call RuntimeHelpers.CreateTuple(elements)
        return LinqExpression.Call(
            CompilerContext.CreateTupleMethod,
            elementsInit);
    }

    internal LinqExpression CompileDeconstruction(DeconstructionExpr expr)
    {
        // Compile the value expression
        var value = Compile(expr.ValueExpression);

        // Create string[] of variable names
        var variableNamesArray = LinqExpression.NewArrayInit(
            typeof(string),
            expr.VariableNames.Select(n => LinqExpression.Constant(n)));

        // Call RuntimeHelpers.DeconstructTuple(value, variableNames, context)
        return LinqExpression.Call(
            CompilerContext.DeconstructTupleMethod,
            value,
            variableNamesArray,
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileDefault(DefaultExpr def)
    {
        if (def.TypeToken == null)
            return LinqExpression.Constant(null, typeof(object));

        // Resolve type via context's TypeResolver then call TypeHelpers.GetDefaultValue(Type)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(def.TypeToken.Value.Lexeme));

        return LinqExpression.Call(
            CompilerContext.GetDefaultValueMethod,
            resolvedType);
    }

    internal LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        var opInfo = OperatorRegistry.GetUnaryOperator(u.Op.Type);
        if (opInfo == null)
            throw new NotSupportedException($"Unary operator {u.Op.Type}");

        var info = opInfo.Value;
        return info.HasCheckedParam
            ? LinqExpression.Call(info.Method, operand, LinqExpression.Constant(_ctx.IsChecked))
            : LinqExpression.Call(info.Method, operand);
    }

    internal LinqExpression CompileCast(CastExpr cast)
    {
        var value = Compile(cast.Expression);
        var sourceStaticType = _ctx.TypeInferrer.Infer(cast.Expression);

        // Only enforce unboxing semantics when the source expression is a simple identifier
        // with a known explicit type (e.g., object x = 42). For complex expressions (binary,
        // grouping, index access, etc.), the TypeInferrer defaults to typeof(object) which would
        // incorrectly block valid numeric conversions like (int)dynamicDouble.
        var effectiveSourceType = cast.Expression is IdentifierExpr ? sourceStaticType : null;

        // Resolve target type via context's TypeResolver
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(cast.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerContext.ExplicitCastMethod,
            value,
            resolvedType,
            LinqExpression.Constant(effectiveSourceType, typeof(Type)),
            LinqExpression.Constant(_ctx.IsChecked));
    }

    internal LinqExpression CompileAs(AsExpr asExpr)
    {
        var value = Compile(asExpr.Expression);

        // Resolve target type via context's TypeResolver
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerContext.ResolveTypeMethod,
            LinqExpression.Constant(asExpr.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerContext.TryAsMethod,
            value,
            resolvedType);
    }

    internal LinqExpression CompileBinary(BinaryExpr b)
    {
        if (TryFoldPureConstantExpression(b, out var folded))
            return folded;

        if (TryEmitDirectBinary(b) is { } direct)
            return direct;

        var left = Compile(b.Left);
        var right = Compile(b.Right);

        // ECMA-334 §10.2.11: Implicit constant expression conversions.
        ApplyConstantPromotion(b, ref left, ref right);

        var opInfo = OperatorRegistry.GetBinaryOperator(b.Op.Type);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator {b.Op.Type}");

        return EmitBinaryOpCall(opInfo.Value, left, right);
    }

    private bool TryFoldPureConstantExpression(Expr expr, out LinqExpression folded)
    {
        folded = null!;
        if (!IsPureConstantExpression(expr))
            return false;

        try
        {
            // Evaluate constant-only subtrees once at compile time to remove runtime dispatch
            // overhead on hot paths like arithmetic precedence expressions.
            var evaluator = new Evaluator(_ctx.Context, _ctx.Options, CancellationToken.None);
            var value = evaluator.Evaluate(expr);
            folded = LinqExpression.Constant(value, typeof(object));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPureConstantExpression(Expr expr) => expr switch
    {
        LiteralExpr => true,
        UnaryExpr u => IsPureConstantExpression(u.Right),
        BinaryExpr b => IsPureConstantExpression(b.Left) && IsPureConstantExpression(b.Right),
        LogicalExpr l => IsPureConstantExpression(l.Left) && IsPureConstantExpression(l.Right),
        ConditionalExpr c => IsPureConstantExpression(c.Condition) &&
                             IsPureConstantExpression(c.ThenBranch) &&
                             IsPureConstantExpression(c.ElseBranch),
        NullCoalesceExpr n => IsPureConstantExpression(n.Left) && IsPureConstantExpression(n.Right),
        CastExpr c => IsPureConstantExpression(c.Expression),
        CheckedExpr c => IsPureConstantExpression(c.Expression),
        _ => false
    };

    private static bool IsDirectableNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(short) || type == typeof(ushort) || type == typeof(byte) ||
        type == typeof(sbyte) || type == typeof(decimal) || type == typeof(char);

    private static bool IsIntegerType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(uint) ||
        type == typeof(ulong) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(char);

    private LinqExpression? TryEmitDirectContainsBinary(
        TokenType op,
        LinqExpression leftExpr,
        Type leftType,
        LinqExpression rightExpr,
        Type rightType)
    {
        if (op is not TokenType.In and not TokenType.NotIn)
            return null;

        if (leftType == typeof(object) || rightType == typeof(object))
            return null;

        var containsMethod = FindContainsMethod(rightType, leftType);
        if (containsMethod == null)
            return null;

        var parameterType = containsMethod.GetParameters()[0].ParameterType;
        if (!CanDirectlyBindContainsParameter(parameterType, leftType))
            return null;

        var typedLeft = EnsureTypedExpression(leftExpr, leftType);
        var typedRight = EnsureTypedExpression(rightExpr, rightType);

        var leftVar = LinqExpression.Variable(leftType, "containsLeft");
        var rightVar = LinqExpression.Variable(rightType, "containsRight");

        var containsArg = ConvertExpressionForParameter(leftVar, parameterType);
        var containsCall = LinqExpression.Call(rightVar, containsMethod, containsArg);
        LinqExpression boolResult = op == TokenType.NotIn
            ? LinqExpression.Not(containsCall)
            : containsCall;

        // Preserve "in" null-collection diagnostics: null collection is not a NullReferenceException.
        var nullFallback = LinqExpression.Call(
            InOperatorMethod,
            EnsureObjectExpression(leftVar),
            LinqExpression.Constant(null, typeof(object)));

        var guardedResult = LinqExpression.Condition(
            LinqExpression.Equal(
                LinqExpression.Convert(rightVar, typeof(object)),
                LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Convert(nullFallback, typeof(bool)),
            boolResult);

        return LinqExpression.Block(
            typeof(object),
            [leftVar, rightVar],
            LinqExpression.Assign(leftVar, typedLeft),
            LinqExpression.Assign(rightVar, typedRight),
            LinqExpression.Convert(guardedResult, typeof(object)));
    }

    private LinqExpression? TryEmitDirectLikeBinary(
        BinaryExpr binary,
        TokenType op,
        LinqExpression leftExpr,
        Type leftType)
    {
        if (op is not TokenType.Like and not TokenType.NotLike)
            return null;

        if (leftType != typeof(string))
            return null;

        if (binary.Right is not LiteralExpr { Value: string pattern })
            return null;

        var patternMode = Operators.ClassifyLikePattern(pattern);
        if (patternMode == Operators.LikePatternMode.General)
            return null;

        var typedLeft = EnsureTypedExpression(leftExpr, typeof(string));
        var leftVar = LinqExpression.Variable(typeof(string), "likeLeft");

        var patternConstant = LinqExpression.Constant(pattern, typeof(string));
        var comparisonConstant = LinqExpression.Constant(StringComparison.Ordinal);

        LinqExpression optimized = patternMode switch
        {
            Operators.LikePatternMode.Exact => LinqExpression.Call(
                StringEqualsOrdinalMethod,
                leftVar,
                patternConstant,
                comparisonConstant),
            Operators.LikePatternMode.Prefix => LinqExpression.Call(
                leftVar,
                StringStartsWithOrdinalMethod,
                LinqExpression.Constant(pattern[..^1], typeof(string)),
                comparisonConstant),
            Operators.LikePatternMode.Suffix => LinqExpression.Call(
                leftVar,
                StringEndsWithOrdinalMethod,
                LinqExpression.Constant(pattern[1..], typeof(string)),
                comparisonConstant),
            Operators.LikePatternMode.Contains => LinqExpression.Call(
                leftVar,
                StringContainsOrdinalMethod,
                LinqExpression.Constant(pattern[1..^1], typeof(string)),
                comparisonConstant),
            _ => throw new InvalidOperationException($"Unexpected LIKE mode: {patternMode}")
        };

        // Preserve Like() operand validation semantics for null LHS.
        var nullFallback = LinqExpression.Call(
            LikeMethod,
            EnsureObjectExpression(leftVar),
            LinqExpression.Constant((object)pattern, typeof(object)));

        LinqExpression guarded = LinqExpression.Condition(
            LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(string))),
            LinqExpression.Convert(nullFallback, typeof(bool)),
            optimized);

        if (op == TokenType.NotLike)
            guarded = LinqExpression.Not(guarded);

        return LinqExpression.Block(
            typeof(object),
            [leftVar],
            LinqExpression.Assign(leftVar, typedLeft),
            LinqExpression.Convert(guarded, typeof(object)));
    }

    private static MethodInfo? FindContainsMethod(Type collectionType, Type valueType)
    {
        if (collectionType == typeof(string))
        {
            if (valueType == typeof(string))
                return typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
            if (valueType == typeof(char))
                return typeof(string).GetMethod(nameof(string.Contains), [typeof(char)]);
            return null;
        }

        var candidates = collectionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == nameof(List<int>.Contains) && m.ReturnType == typeof(bool))
            .Select(m => (Method: m, Params: m.GetParameters()))
            .Where(x => x.Params.Length == 1 && CanDirectlyBindContainsParameter(x.Params[0].ParameterType, valueType))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var exact = candidates.FirstOrDefault(x => x.Params[0].ParameterType == valueType).Method;
        if (exact != null)
            return exact;

        var assignableReference = candidates.FirstOrDefault(x =>
                x.Params[0].ParameterType != typeof(object) &&
                !x.Params[0].ParameterType.IsValueType &&
                x.Params[0].ParameterType.IsAssignableFrom(valueType))
            .Method;
        if (assignableReference != null)
            return assignableReference;

        return candidates.FirstOrDefault(x => x.Params[0].ParameterType == typeof(object)).Method;
    }

    private static bool CanDirectlyBindContainsParameter(Type parameterType, Type valueType)
    {
        if (parameterType == valueType || parameterType == typeof(object))
            return true;

        return !parameterType.IsValueType && parameterType.IsAssignableFrom(valueType);
    }

    private static LinqExpression EnsureTypedExpression(LinqExpression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;

        return LinqExpression.Convert(expression, targetType);
    }

    private static LinqExpression ConvertExpressionForParameter(LinqExpression expression, Type parameterType)
    {
        if (expression.Type == parameterType)
            return expression;

        return parameterType == typeof(object)
            ? EnsureObjectExpression(expression)
            : LinqExpression.Convert(expression, parameterType);
    }

    private LinqExpression? TryEmitDirectBinary(BinaryExpr b)
    {
        var op = b.Op.Type;

        var (leftExpr, leftType) = CompileTyped(b.Left);
        var (rightExpr, rightType) = CompileTyped(b.Right);

        if (TryEmitDirectContainsBinary(op, leftExpr, leftType, rightExpr, rightType) is { } containsDirect)
            return containsDirect;
        if (TryEmitDirectLikeBinary(b, op, leftExpr, leftType) is { } likeDirect)
            return likeDirect;

        // ECMA-334 §10.2.11 constant expression promotions (e.g., uint + 1).
        // Keep direct codegen active by applying the promotion eagerly for literal operands.
        if (b is { Left: LiteralExpr { Value: not null } leftLiteral, Right: LiteralExpr { Value: not null } rightLiteral })
        {
            var promoted = NumericDispatch.TryConstantPromotion(
                leftLiteral.Value, leftLiteral.IsConstant,
                rightLiteral.Value, rightLiteral.IsConstant);
            if (promoted != null)
            {
                leftType = promoted.Value.Left.GetType();
                rightType = promoted.Value.Right.GetType();
                leftExpr = LinqExpression.Constant(promoted.Value.Left, leftType);
                rightExpr = LinqExpression.Constant(promoted.Value.Right, rightType);
            }
        }

        // For variable + constant forms where ECMA constant promotion depends on runtime value
        // (e.g., uint + const-int), keep runtime dispatch to preserve exact semantics.
        var leftIsConst = b.Left is LiteralExpr { IsConstant: true };
        var rightIsConst = b.Right is LiteralExpr { IsConstant: true };
        if (leftIsConst ^ rightIsConst)
        {
            var constType = leftIsConst ? leftType : rightType;
            var otherType = leftIsConst ? rightType : leftType;
            var mayNeedRuntimeConstantPromotion =
                (otherType == typeof(uint) && constType == typeof(int)) ||
                (otherType == typeof(ulong) && (constType == typeof(int) || constType == typeof(long)));
            if (mayNeedRuntimeConstantPromotion)
                return null;
        }

        // Both must be known numeric types
        if (!IsDirectableNumericType(leftType) || !IsDirectableNumericType(rightType))
            return null;

        // decimal + float/double has no implicit conversion in IL
        if (leftType == typeof(decimal) && (rightType == typeof(double) || rightType == typeof(float)))
            return null;
        if (rightType == typeof(decimal) && (leftType == typeof(double) || leftType == typeof(float)))
            return null;

        // Bitwise/shift ops require integer operands
        bool isBitwiseOrShift = op is TokenType.Amp or TokenType.Pipe or TokenType.Caret
            or TokenType.LessLess or TokenType.GreaterGreater;
        if (isBitwiseOrShift && (!IsIntegerType(leftType) || !IsIntegerType(rightType)))
            return null;

        // Shift ops: right operand must be int per ECMA-334
        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            if (rightType != typeof(int))
                return null;
        }

        var resultType = NumericDispatch.GetResultType(leftType, rightType);

        // Ensure expressions have their concrete types.
        // For object-typed operands, apply runtime numeric coercion before cast so
        // compiled delegates remain stable when variable numeric runtime types change.
        var typedLeft = leftExpr.Type == leftType
            ? leftExpr
            : leftExpr.Type == typeof(object)
                ? LinqExpression.Convert(
                    LinqExpression.Call(
                        CompilerContext.CoerceNumericMethod,
                        leftExpr,
                        LinqExpression.Constant(leftType, typeof(Type))),
                    leftType)
                : leftExpr;
        var typedRight = rightExpr.Type == rightType
            ? rightExpr
            : rightExpr.Type == typeof(object)
                ? LinqExpression.Convert(
                    LinqExpression.Call(
                        CompilerContext.CoerceNumericMethod,
                        rightExpr,
                        LinqExpression.Constant(rightType, typeof(Type))),
                    rightType)
                : rightExpr;

        // Promote to result type if needed (e.g., int + double → both become double)
        if (typedLeft.Type != resultType)
            typedLeft = LinqExpression.Convert(typedLeft, resultType);
        if (typedRight.Type != resultType)
            typedRight = LinqExpression.Convert(typedRight, resultType);

        // Shift ops: right operand stays int regardless of result type
        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            typedRight = rightExpr.Type == typeof(int)
                ? rightExpr
                : rightExpr.Type == typeof(object)
                    ? LinqExpression.Convert(
                        LinqExpression.Call(
                            CompilerContext.CoerceNumericMethod,
                            rightExpr,
                            LinqExpression.Constant(typeof(int), typeof(Type))),
                        typeof(int))
                    : LinqExpression.Convert(rightExpr, typeof(int));
        }

        var isChecked = _ctx.IsChecked;
        LinqExpression? result = op switch
        {
            TokenType.Plus => isChecked ? LinqExpression.AddChecked(typedLeft, typedRight) : LinqExpression.Add(typedLeft, typedRight),
            TokenType.Minus => isChecked ? LinqExpression.SubtractChecked(typedLeft, typedRight) : LinqExpression.Subtract(typedLeft, typedRight),
            TokenType.Star => isChecked ? LinqExpression.MultiplyChecked(typedLeft, typedRight) : LinqExpression.Multiply(typedLeft, typedRight),
            TokenType.Slash => LinqExpression.Divide(typedLeft, typedRight),
            TokenType.Percent => LinqExpression.Modulo(typedLeft, typedRight),
            TokenType.Less => LinqExpression.LessThan(typedLeft, typedRight),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(typedLeft, typedRight),
            TokenType.Greater => LinqExpression.GreaterThan(typedLeft, typedRight),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(typedLeft, typedRight),
            TokenType.EqualEqual => LinqExpression.Equal(typedLeft, typedRight),
            TokenType.BangEqual => LinqExpression.NotEqual(typedLeft, typedRight),
            TokenType.LessEqualGreater => CreateSpaceshipNumericResult(typedLeft, typedRight),
            TokenType.Amp => LinqExpression.And(typedLeft, typedRight),
            TokenType.Pipe => LinqExpression.Or(typedLeft, typedRight),
            TokenType.Caret => LinqExpression.ExclusiveOr(typedLeft, typedRight),
            TokenType.LessLess => LinqExpression.LeftShift(typedLeft, typedRight),
            TokenType.GreaterGreater => LinqExpression.RightShift(typedLeft, typedRight),
            _ => null
        };

        if (result == null) return null;

        return LinqExpression.Convert(result, typeof(object));
    }

    /// <summary>
    /// ECMA-334 §10.2.11: At IL-compile time, pre-promote constant literal operands.
    /// Since literal values are known at compile time, we can replace the compiled
    /// LinqExpression.Constant with a promoted-type constant (e.g., int 3 -> uint 3).
    /// </summary>
    private static void ApplyConstantPromotion(BinaryExpr b, ref LinqExpression left, ref LinqExpression right)
    {
        var leftLiteral = b.Left as LiteralExpr;
        var rightLiteral = b.Right as LiteralExpr;

        bool leftIsConstant = leftLiteral is { IsConstant: true };
        bool rightIsConstant = rightLiteral is { IsConstant: true };

        if (!leftIsConstant && !rightIsConstant)
            return;

        // We need both operand values to call TryConstantPromotion.
        // Both sides must be non-null literals for this compile-time optimization.
        object? leftVal = leftLiteral?.Value;
        object? rightVal = rightLiteral?.Value;

        if (leftVal == null || rightVal == null) return;

        var promoted = NumericDispatch.TryConstantPromotion(
            leftVal, leftIsConstant, rightVal, rightIsConstant);

        if (promoted != null)
        {
            left = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Left, promoted.Value.Left.GetType()),
                typeof(object));
            right = LinqExpression.Convert(
                LinqExpression.Constant(promoted.Value.Right, promoted.Value.Right.GetType()),
                typeof(object));
        }
    }

    internal LinqExpression CompileLogical(LogicalExpr l)
    {
        var opLexeme = l.Op.Lexeme;
        var leftTypeName = _ctx.TypeInferrer.Infer(l.Left).Name;
        var rightTypeName = _ctx.TypeInferrer.Infer(l.Right).Name;

        var leftTruthy = CompileLogicalOperandAsBoolean(l.Left, opLexeme, rightTypeName);
        var rightTruthy = CompileLogicalOperandAsBoolean(l.Right, opLexeme, leftTypeName);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    private LinqExpression CompileLogicalOperandAsBoolean(
        Expr operand,
        string opLexeme,
        string otherOperandTypeName)
    {
        var (compiled, knownType) = CompileTyped(operand);

        if (knownType == typeof(bool) && compiled.Type == typeof(bool))
            return compiled;

        var boxed = compiled.Type == typeof(object)
            ? compiled
            : LinqExpression.Convert(compiled, typeof(object));

        return LinqExpression.Call(
            CompilerContext.RequireBooleanForLogicalOperatorMethod,
            boxed,
            LinqExpression.Constant(opLexeme),
            LinqExpression.Constant(otherOperandTypeName));
    }

    internal LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        // Get static types for promotion check (ECMA-334 §12.18)
        var thenType = _ctx.TypeInferrer.Infer(c.ThenBranch);
        var elseType = _ctx.TypeInferrer.Infer(c.ElseBranch);

        var result = LinqExpression.Condition(condition, thenBranch, elseBranch);

        // Apply type promotion at compile time if both branches are numeric with different types
        if (thenType != typeof(object) && elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) && TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var promotionType = NumericDispatch.GetResultType(thenType, elseType);
            var promoteMethod = typeof(NumericDispatch).GetMethod(nameof(NumericDispatch.PromoteToType))!;
            return LinqExpression.Call(promoteMethod, result, LinqExpression.Constant(promotionType, typeof(Type)));
        }

        return result;
    }

    internal LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = Compile(n.Left);
        var right = Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }

    internal LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var directResult = TryEmitDirectMemberAccess(m);
        if (directResult != null)
            return directResult;

        var obj = Compile(m.Object);

        return LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _ctx.OptionsParam,
            LinqExpression.Constant(m.NullSafe),
            _ctx.CurrentContext);
    }

    private LinqExpression? TryEmitDirectMemberAccess(MemberAccessExpr m)
    {
        // Keep direct member access to simple targets only.
        // Chained targets can build very deep compile-time recursion and should use runtime dispatch.
        if (m.Object is MemberAccessExpr or CallExpr)
            return null;

        var objectType = _ctx.TypeInferrer.Infer(m.Object);

        Type? targetType;
        BindingFlags flags;
        bool isStatic;

        if (objectType == typeof(Type) && m.Object is TypeReferenceExpr typeRef)
        {
            targetType = _ctx.Context.TypeResolver.TryResolveType(typeRef.TypeToken.Lexeme);
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;
        }
        else if (objectType == typeof(Type) && m.Object is IdentifierExpr idExpr)
        {
            targetType = _ctx.Context.TypeResolver.TryResolveType(idExpr.Name.Lexeme);
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;
        }
        else if (objectType != typeof(object) && !objectType.IsArray)
        {
            targetType = objectType;
            flags = BindingFlags.Public | BindingFlags.Instance;
            isStatic = false;
        }
        else return null;

        if (!_ctx.Options.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        if (targetType == null) return null;

        var prop = _ctx.Context.TypeCache.GetProperty(targetType, m.Name.Lexeme, flags);
        if (prop != null)
        {
            if (isStatic && !_ctx.Options.Sandbox.AllowStaticPropertyRead) return null;
            if (!isStatic && !_ctx.Options.Sandbox.AllowPropertyRead) return null;
            return EmitDirectAccess(m, isStatic, obj => LinqExpression.Property(obj, prop));
        }

        var field = _ctx.Context.TypeCache.GetField(targetType, m.Name.Lexeme, flags);
        if (field != null)
        {
            if (isStatic && !_ctx.Options.Sandbox.AllowStaticFieldRead) return null;
            if (!isStatic && !_ctx.Options.Sandbox.AllowPropertyRead) return null;
            return EmitDirectAccess(m, isStatic, obj => LinqExpression.Field(obj, field));
        }

        return null;
    }

    private LinqExpression EmitDirectAccess(
        MemberAccessExpr m, bool isStatic, Func<LinqExpression?, LinqExpression> buildAccess)
    {
        if (isStatic)
            return LinqExpression.Convert(WrapGuard(buildAccess(null)), typeof(object));

        if (!m.NullSafe)
        {
            var obj = Compile(m.Object);
            var objectType = _ctx.TypeInferrer.Infer(m.Object);
            var typedObj = LinqExpression.Convert(obj, objectType);
            return LinqExpression.Convert(WrapGuard(buildAccess(typedObj)), typeof(object));
        }

        var objVar = LinqExpression.Variable(typeof(object), "nullSafeTarget");
        var objectType2 = _ctx.TypeInferrer.Infer(m.Object);
        var typedVar = LinqExpression.Convert(objVar, objectType2);
        return LinqExpression.Block(
            typeof(object),
            [objVar],
            LinqExpression.Assign(objVar, Compile(m.Object)),
                LinqExpression.Condition(
                    LinqExpression.Equal(objVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Convert(WrapGuard(buildAccess(typedVar)), typeof(object))));

        LinqExpression WrapGuard(LinqExpression value)
        {
            if (value.Type.IsValueType)
                return value;

            var typedGuard = CompilerContext.GetGuardReflectionLeakTypedMethod(value.Type);
            return LinqExpression.Call(
                typedGuard,
                value,
                LinqExpression.Constant($"direct member access {m.Name.Lexeme}"));
        }
    }

    internal LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var target = Compile(expr.Object);

        if (expr.NullSafe)
        {
            // arr?[i] - null-safe index access
            var targetVar = LinqExpression.Variable(typeof(object), "target");
            var index = Compile(expr.Index);
            return LinqExpression.Block(
                typeof(object),
                [targetVar],
                LinqExpression.Assign(targetVar, target),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    LinqExpression.Call(CompilerContext.GetIndexMethod, targetVar, index, _ctx.OptionsParam)));
        }

        var indexValue = Compile(expr.Index);
        return LinqExpression.Call(CompilerContext.GetIndexMethod, target, indexValue, _ctx.OptionsParam);
    }

    internal LinqExpression CompileSlice(SliceExpr expr)
    {
        var target = Compile(expr.Target);
        var start = expr.Start != null
            ? Compile(expr.Start)
            : LinqExpression.Constant(null, typeof(object));
        var end = expr.End != null
            ? Compile(expr.End)
            : LinqExpression.Constant(null, typeof(object));

        if (expr.Step != null)
        {
            var step = Compile(expr.Step);
            return LinqExpression.Call(CompilerContext.GetSliceStepMethod, target, start, end, step, _ctx.OptionsParam);
        }

        return LinqExpression.Call(CompilerContext.GetSliceMethod, target, start, end, _ctx.OptionsParam);
    }

    internal LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var inferredType = LinqExpression.Variable(typeof(Type), "inferredType");

        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            var resolvedDeclType = LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme));

            var declTypeVar = LinqExpression.Variable(typeof(Type), "declType");

            value = LinqExpression.Block(
                new[] { declTypeVar },
                LinqExpression.Assign(declTypeVar, resolvedDeclType),
                LinqExpression.Call(
                    CompilerContext.ValidateAndCoerceTypeMethod,
                    declTypeVar,
                    value,
                    LinqExpression.Constant(v.Name.Lexeme)));
        }

        LinqExpression getInferredType;
        if (v.DeclaredType != null)
        {
            // Resolve type via context's TypeResolver
            getInferredType = LinqExpression.Call(
                _ctx.TypeResolverExpr,
                CompilerContext.ResolveTypeMethod,
                LinqExpression.Constant(v.DeclaredType.Value.Lexeme));
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
            LinqExpression.Condition(
                LinqExpression.Equal(
                    LinqExpression.Constant(v.Name.Lexeme),
                    LinqExpression.Constant("_")),
                LinqExpression.Block(
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp),
                    temp),
                LinqExpression.Block(
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                        LinqExpression.Constant(v.Name.Lexeme), temp, inferredType),
                    temp)));
    }

    internal LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValueExpr = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(ca.Op.Type, out var baseOp))
            throw new NotSupportedException($"Compound operator {ca.Op.Type}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {ca.Op.Type}");

        LinqExpression opCall = EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        var validateCall = LinqExpression.Call(CompilerContext.ValidateCompoundAssignmentMethod,
            LinqExpression.Constant(name), opCall, rightTemp, _ctx.CurrentContext);

        return LinqExpression.Block(
            new[] { temp, rightTemp },
            LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, validateCall),
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    internal LinqExpression CompileMemberCompoundAssign(MemberCompoundAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var rightValueExpr = Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetMember
        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        LinqExpression opCall = EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        // Set via MemberAccess.SetMember
        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _ctx.OptionsParam, _ctx.CurrentContext);

        return LinqExpression.Block(
            new[] { objTemp, rightTemp, temp },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, opCall),
            setCall,
            temp);
    }

    internal LinqExpression CompileIndexCompoundAssign(IndexCompoundAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var rightValueExpr = Compile(expr.Value);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var rightTemp = LinqExpression.Variable(typeof(object), "rightTemp");
        var temp = LinqExpression.Variable(typeof(object), "temp");

        // Get current value via MemberAccess.GetIndex
        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        // Map compound op to base binary op
        if (!OperatorRegistry.CompoundToBaseOperator.TryGetValue(expr.Operator, out var baseOp))
            throw new NotSupportedException($"Compound operator {expr.Operator}");

        var opInfo = OperatorRegistry.GetBinaryOperator(baseOp);
        if (opInfo == null)
            throw new NotSupportedException($"Binary operator for compound {expr.Operator}");

        LinqExpression opCall = EmitBinaryOpCall(opInfo.Value, currentValue, rightTemp);

        // Set via MemberAccess.SetIndex
        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, temp, _ctx.OptionsParam);

        return LinqExpression.Block(
            new[] { objTemp, indexTemp, rightTemp, temp },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(indexTemp, indexExpr),
            LinqExpression.Assign(rightTemp, rightValueExpr),
            LinqExpression.Assign(temp, opCall),
            setCall,
            temp);
    }

    internal LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);

        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var valueTemp = LinqExpression.Variable(typeof(object), "val");
        var check = LinqExpression.Call(CompilerContext.CheckAllowIndexSetMethod, _ctx.OptionsParam, indexTemp);
        var set = LinqExpression.Call(CompilerContext.SetIndexMethod, target, indexTemp, valueTemp, _ctx.OptionsParam);

        return LinqExpression.Block(
            new[] { indexTemp, valueTemp },
            LinqExpression.Assign(indexTemp, index),
            LinqExpression.Assign(valueTemp, value),
            check,
            set,
            valueTemp);
    }

    internal LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        // Get Add/Subtract info from registry
        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => isIncrement
            ? EmitBinaryOpCall(addInfo, left, one)
            : EmitBinaryOpCall(subInfo, left, one);

        var checkExpr = LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
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
                LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    internal LinqExpression CompileMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        var newValue = Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), result, _ctx.OptionsParam, _ctx.CurrentContext);

        return LinqExpression.Block(
            new[] { objTemp, temp, result },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Assign(result, newValue),
                    setCall)),
            result);
    }

    internal LinqExpression CompileIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        var newValue = Compile(expr.Value);

        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, result, _ctx.OptionsParam);

        return LinqExpression.Block(
            new[] { objTemp, indexTemp, temp, result },
            LinqExpression.Assign(objTemp, objExpr),
            LinqExpression.Assign(indexTemp, indexExpr),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Assign(result, newValue),
                    setCall)),
            result);
    }

    internal LinqExpression CompileMemberIncrement(MemberIncrementExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerContext.GetMemberMethod,
            objTemp,
            LinqExpression.Constant(expr.MemberName),
            _ctx.OptionsParam,
            LinqExpression.Constant(false),
            _ctx.CurrentContext);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? EmitBinaryOpCall(addInfo, left, one)
            : EmitBinaryOpCall(subInfo, left, one);

        var setCall = LinqExpression.Call(CompilerContext.SetMemberMethod,
            objTemp, LinqExpression.Constant(expr.MemberName), temp, _ctx.OptionsParam, _ctx.CurrentContext);

        if (expr.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { objTemp, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                setCall,
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { objTemp, original, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                setCall,
                original);
        }
    }

    internal LinqExpression CompileIndexIncrement(IndexIncrementExpr expr)
    {
        var objExpr = Compile(expr.Object);
        var indexExpr = Compile(expr.Index);
        var objTemp = LinqExpression.Variable(typeof(object), "obj");
        var indexTemp = LinqExpression.Variable(typeof(object), "idx");
        var original = LinqExpression.Variable(typeof(object), "original");
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));

        var currentValue = LinqExpression.Call(
            CompilerContext.GetIndexMethod,
            objTemp, indexTemp, _ctx.OptionsParam);

        var addInfo = OperatorRegistry.GetBinaryOperator(TokenType.Plus)!.Value;
        var subInfo = OperatorRegistry.GetBinaryOperator(TokenType.Minus)!.Value;

        LinqExpression MakeOpCall(LinqExpression left) => expr.IsIncrement
            ? EmitBinaryOpCall(addInfo, left, one)
            : EmitBinaryOpCall(subInfo, left, one);

        var setCall = LinqExpression.Call(CompilerContext.SetIndexMethod,
            objTemp, indexTemp, temp, _ctx.OptionsParam);

        if (expr.IsPrefix)
        {
            return LinqExpression.Block(
                new[] { objTemp, indexTemp, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(indexTemp, indexExpr),
                LinqExpression.Assign(temp, MakeOpCall(currentValue)),
                setCall,
                temp);
        }
        else
        {
            return LinqExpression.Block(
                new[] { objTemp, indexTemp, original, temp },
                LinqExpression.Assign(objTemp, objExpr),
                LinqExpression.Assign(indexTemp, indexExpr),
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, MakeOpCall(original)),
                setCall,
                original);
        }
    }

    internal LinqExpression CompileCall(CallExpr call)
    {
        if (call.Callee is MemberAccessExpr ma)
        {
            var directResult = TryEmitDirectCall(call, ma);
            if (directResult != null) return directResult;
        }

        // Compile arguments into an object[] array, wrapping named arguments in NamedArg
        var argsVar = LinqExpression.Variable(typeof(object?[]), "args");
        var argsInit = LinqExpression.NewArrayInit(
            typeof(object),
            call.Arguments.Select(CompileArgument));

        var typeArgsExpr = call.TypeArguments != null
            ? LinqExpression.Constant(call.TypeArguments, typeof(IReadOnlyList<string>))
            : LinqExpression.Constant(null, typeof(IReadOnlyList<string>));

        // Check if any arguments are out parameters (need post-call variable definition)
        var hasOutArgs = call.Arguments.Any(a => a is OutArgExpr);

        LinqExpression callExpr;

        // Check if this is a member access call (target.Method(args))
        if (call.Callee is MemberAccessExpr memberAccess)
        {
            var target = Compile(memberAccess.Object);
            var methodName = memberAccess.Name.Lexeme;

            callExpr = LinqExpression.Call(
                CompilerContext.InvokeMemberCallMethod,
                target,
                LinqExpression.Constant(methodName),
                argsVar,
                LinqExpression.Constant(memberAccess.NullSafe),
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }
        else if (call.Callee is IdentifierExpr calleeId)
        {
            // Identifier call target: use direct dispatcher for both Standard and Extended modes.
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeIdentifierCallMethod,
                LinqExpression.Constant(calleeId.Name.Lexeme),
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }
        else
        {
            // General call: evaluate callee and invoke
            var callee = Compile(call.Callee);
            callExpr = LinqExpression.Call(
                CompilerContext.InvokeCallMethod,
                callee,
                argsVar,
                _ctx.CurrentContext,
                _ctx.OptionsParam,
                _ctx.CtParam,
                typeArgsExpr);
        }

        if (!hasOutArgs)
        {
            // Simple case: no out args, just call and return
            return LinqExpression.Block(
                new[] { argsVar },
                LinqExpression.Assign(argsVar, argsInit),
                callExpr);
        }

        // Out args case: call, then define out variables from modified args array
        var resultVar = LinqExpression.Variable(typeof(object), "callResult");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(argsVar, argsInit),
            LinqExpression.Assign(resultVar, callExpr)
        };

        // After the call, MethodInvoker.CopyBackOutArgs has replaced OutArgMarker entries
        // in argsVar with the actual values. Define variables for non-discard out args.
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (call.Arguments[i] is OutArgExpr { IsDiscard: false } outArg)
            {
                // Read the out value from argsVar[i]
                var outValue = LinqExpression.ArrayIndex(argsVar, LinqExpression.Constant(i));

                // Resolve type: if explicit type specified, use it; otherwise use runtime type
                LinqExpression typeExpr;
                if (outArg.TypeName != null)
                {
                    typeExpr = LinqExpression.Call(
                        _ctx.TypeResolverExpr,
                        CompilerContext.ResolveTypeMethod,
                        LinqExpression.Constant(outArg.TypeName));
                }
                else
                {
                    // typeof(object) — the runtime type will be set by the value itself
                    // Use a conditional: value?.GetType() ?? typeof(object)
                    var getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
                    typeExpr = LinqExpression.Condition(
                        LinqExpression.NotEqual(outValue, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Call(outValue, getTypeMethod),
                        LinqExpression.Constant(typeof(object), typeof(Type)));
                }

                statements.Add(LinqExpression.Call(
                    _ctx.CurrentContext,
                    CompilerContext.DefineNewMethod,
                    LinqExpression.Constant(outArg.VariableName),
                    outValue,
                    typeExpr));
            }
        }

        // Return the call result
        statements.Add(resultVar);

        return LinqExpression.Block(
            new[] { argsVar, resultVar },
            statements);
    }

    private LinqExpression? TryEmitDirectCall(CallExpr call, MemberAccessExpr memberAccess)
    {
        if (call.TypeArguments is { Count: > 0 }) return null;
        if (call.Arguments.Any(a => a is NamedArgumentExpr or OutArgExpr)) return null;
        if (memberAccess.NullSafe) return null;
        if (!_ctx.Options.Sandbox.AllowMethodCalls) return null;
        // Keep direct calls to simple targets only.
        // Deep chained calls should remain on runtime dispatch to avoid compile-stack blowups.
        if (memberAccess.Object is MemberAccessExpr or CallExpr) return null;

        Type? targetType;
        BindingFlags flags;
        bool isStatic;

        if (memberAccess.Object is IdentifierExpr moduleId &&
            !_ctx.Context.Functions.ContainsKey(moduleId.Name.Lexeme) &&
            _ctx.Context.Modules.TryGetValue(moduleId.Name.Lexeme, out var moduleInfo) &&
            moduleInfo.Instance == null &&
            moduleInfo.Type is { IsAbstract: true, IsSealed: true })
        {
            // Built-in static modules (e.g., Math, Convert): bind directly to static methods.
            targetType = moduleInfo.Type;
            flags = BindingFlags.Public | BindingFlags.Static;
            isStatic = true;
        }
        else
        {
            var objectType = _ctx.TypeInferrer.Infer(memberAccess.Object);
            if (objectType == typeof(Type) && memberAccess.Object is TypeReferenceExpr typeRef)
            {
                targetType = _ctx.Context.TypeResolver.TryResolveType(typeRef.TypeToken.Lexeme);
                flags = BindingFlags.Public | BindingFlags.Static;
                isStatic = true;
            }
            else if (objectType == typeof(Type) && memberAccess.Object is IdentifierExpr idExpr)
            {
                targetType = _ctx.Context.TypeResolver.TryResolveType(idExpr.Name.Lexeme);
                flags = BindingFlags.Public | BindingFlags.Static;
                isStatic = true;
            }
            else if (objectType != typeof(object) && !objectType.IsArray)
            {
                targetType = objectType;
                flags = BindingFlags.Public | BindingFlags.Instance;
                isStatic = false;
            }
            else return null;
        }

        if (!_ctx.Options.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        if (targetType == null) return null;

        var argTypes = new Type[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            argTypes[i] = _ctx.TypeInferrer.Infer(call.Arguments[i]);
            if (argTypes[i] == typeof(object) || argTypes[i].IsArray) return null;
        }

        var method = MethodResolver.TryResolveMethod(targetType, memberAccess.Name.Lexeme, argTypes, flags);
        if (method == null) return null;

        var parameters = method.GetParameters();
        var typedArgs = new LinqExpression[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var (compiled, _) = CompileTyped(call.Arguments[i]);
            if (compiled.Type == parameters[i].ParameterType)
            {
                // Expression type already matches parameter type — no conversion needed
                typedArgs[i] = compiled;
            }
            else if (compiled.Type == typeof(object))
            {
                // Use runtime numeric coercion before cast so compiled delegates remain stable
                // when variable runtime numeric types differ across evaluations.
                var coerced = LinqExpression.Call(
                    CompilerContext.CoerceNumericMethod,
                    compiled,
                    LinqExpression.Constant(parameters[i].ParameterType, typeof(Type)));
                typedArgs[i] = LinqExpression.Convert(coerced, parameters[i].ParameterType);
            }
            else
            {
                typedArgs[i] = LinqExpression.Convert(compiled, parameters[i].ParameterType);
            }
        }

        LinqExpression directCall;
        if (isStatic)
        {
            directCall = LinqExpression.Call(method, typedArgs);
        }
        else
        {
            var target = Compile(memberAccess.Object);
            var typedTarget = LinqExpression.Convert(target, targetType);
            directCall = LinqExpression.Call(typedTarget, method, typedArgs);
        }

        if (method.ReturnType == typeof(void))
            return LinqExpression.Block(directCall, LinqExpression.Constant(null, typeof(object)));

        LinqExpression guardedResult;
        if (method.ReturnType.IsValueType)
        {
            guardedResult = directCall;
        }
        else
        {
            var typedGuard = CompilerContext.GetGuardReflectionLeakTypedMethod(method.ReturnType);
            guardedResult = LinqExpression.Call(
                typedGuard,
                directCall,
                LinqExpression.Constant($"direct method call {method.Name}"));
        }

        return LinqExpression.Convert(guardedResult, typeof(object));
    }

    /// <summary>
    /// Compiles an OutArgExpr to create an OutArgMarker at runtime.
    /// The marker flows through MethodInvoker which recognizes it for ByRef parameter handling.
    /// </summary>
    internal LinqExpression CompileOutArg(OutArgExpr outArg)
    {
        return LinqExpression.Convert(
            LinqExpression.New(
                CompilerContext.OutArgMarkerCtor,
                LinqExpression.Constant(outArg.VariableName),
                LinqExpression.Constant(outArg.TypeName, typeof(string)),
                LinqExpression.Constant(outArg.IsDiscard)),
            typeof(object));
    }

    private LinqExpression CompileArgument(Expr arg)
    {
        if (arg is NamedArgumentExpr namedArg)
        {
            // Wrap named argument in NamedArg: new NamedArg(name, value)
            return LinqExpression.Convert(
                LinqExpression.New(
                    CompilerContext.NamedArgCtor,
                    LinqExpression.Constant(namedArg.Name.Lexeme),
                    Compile(namedArg.Value)),
                typeof(object));
        }
        return Compile(arg);
    }

    internal LinqExpression CompileLambda(LambdaExpr lambda)
    {
        var parameterNames = lambda.Parameters.Select(p => p.Name.Lexeme).ToList();

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
                LinqExpression.Call(closureParam, CompilerContext.CreateChildMethod))
        };

        // Define each parameter in the child context
        for (var i = 0; i < parameterNames.Count; i++)
        {
            statements.Add(LinqExpression.Call(childContextVar, CompilerContext.DefineMethod,
                LinqExpression.Constant(parameterNames[i]),
                LinqExpression.Call(CompilerContext.GetLambdaArgMethod, argsParam, LinqExpression.Constant(i))));
        }

        // Save the current context and create new return context for the lambda
        var savedContext = _ctx.CurrentContext;
        _ctx.CurrentContext = childContextVar;

        // Lambda needs its own return label for return statements in block bodies
        var lambdaReturnLabel = LinqExpression.Label(typeof(object), "lambdaReturn");
        var lambdaReturnValue = LinqExpression.Variable(typeof(object), "lambdaReturnValue");

        // Push new return context for the lambda body
        _ctx.PushReturnContext(lambdaReturnLabel, lambdaReturnValue);

        try
        {
            // Compile the lambda body
            var compiledBody = Compile(lambda.Body);

            // Assign the body result to the return value variable
            statements.Add(LinqExpression.Assign(lambdaReturnValue, compiledBody));
        }
        finally
        {
            _ctx.CurrentContext = savedContext;
            _ctx.PopReturnContext();
        }

        // Add the return label at the end - early returns jump here
        statements.Add(LinqExpression.Label(lambdaReturnLabel, lambdaReturnValue));

        var lambdaBody = LinqExpression.Block(
            typeof(object),
            [childContextVar, lambdaReturnValue],
            statements);

        // Create the delegate: Func<object?[], CsEvalContext, object?>
        var compiledDelegate = LinqExpression.Lambda<Func<object?[], CsEvalContext, object?>>(
            lambdaBody,
            argsParam,
            closureParam);

        // Create CompiledLambdaValue(parameters, compiledBody, closure)
        return LinqExpression.New(
            CompilerContext.CompiledLambdaValueCtor,
            listInit,
            compiledDelegate,
            _ctx.CurrentContext);
    }

    internal LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr)
    {
        var listVar = LinqExpression.Variable(typeof(List<object?>), "list");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(listVar, LinqExpression.New(CompilerContext.ListCtor))
        };

        foreach (var element in expr.Elements)
        {
            if (element is SpreadExpr spread)
            {
                var spreadValue = Compile(spread.Expression);
                statements.Add(LinqExpression.Call(CompilerContext.SpreadIntoListMethod, listVar, spreadValue));
            }
            else
            {
                statements.Add(LinqExpression.Call(listVar, CompilerContext.ListAddMethod, Compile(element)));
            }
        }

        statements.Add(LinqExpression.Call(CompilerContext.CreateTypedArrayMethod, listVar));
        return LinqExpression.Block(new[] { listVar }, statements);
    }

    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr) =>
        Extensions.ObjectLiteralCompiler.CompileObjectLiteral(expr, _ctx, Compile);

    internal LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr)
    {
        var sbVar = LinqExpression.Variable(typeof(StringBuilder), "sb");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(sbVar, LinqExpression.New(CompilerContext.StringBuilderCtor))
        };

        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod,
                        LinqExpression.Constant(text.Text)));
                    break;
                case ExpressionPart exprPart:
                    var value = Compile(exprPart.Expression);
                    if (exprPart.AlignmentSpecifier != null || exprPart.FormatSpecifier != null)
                    {
                        // Build format string like "{0,10:F2}" and call string.Format
                        var formatStr = "{0";
                        if (exprPart.AlignmentSpecifier != null) formatStr += "," + exprPart.AlignmentSpecifier;
                        if (exprPart.FormatSpecifier != null) formatStr += ":" + exprPart.FormatSpecifier;
                        formatStr += "}";
                        var formatted = LinqExpression.Call(
                            CompilerContext.StringFormatMethod,
                            LinqExpression.Constant(formatStr),
                            value);
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, formatted));
                    }
                    else
                    {
                        var valueAsString = LinqExpression.Condition(
                            LinqExpression.Equal(value, LinqExpression.Constant(null, typeof(object))),
                            LinqExpression.Constant(""),
                            LinqExpression.Call(value, CompilerContext.ObjectToStringMethod));
                        statements.Add(LinqExpression.Call(sbVar, CompilerContext.StringBuilderAppendMethod, valueAsString));
                    }
                    break;
            }
        }

        statements.Add(LinqExpression.Convert(
            LinqExpression.Call(sbVar, CompilerContext.StringBuilderToStringMethod),
            typeof(object)));
        return LinqExpression.Block(new[] { sbVar }, statements);
    }

    internal LinqExpression CompileMemberAssign(MemberAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var value = Compile(expr.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(CompilerContext.SetMemberMethod, target,
                LinqExpression.Constant(expr.Name.Lexeme), temp, _ctx.OptionsParam, _ctx.CurrentContext),
            temp);
    }

    internal LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr)
    {
        var name = expr.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(expr.Name));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var result = LinqExpression.Variable(typeof(object), "result");

        var newValue = Compile(expr.Value);

        return LinqExpression.Block(
            new[] { temp, result },
            LinqExpression.Call(CompilerContext.CheckNullCoalesceAssignAllowedMethod,
                LinqExpression.Constant(name), _ctx.CurrentContext),
            LinqExpression.Assign(temp, currentValue),
            LinqExpression.IfThenElse(
                LinqExpression.NotEqual(temp, LinqExpression.Constant(null, typeof(object))),
                LinqExpression.Assign(result, temp),
                LinqExpression.Block(
                    LinqExpression.Call(CompilerContext.CheckAllowAssignmentMethod, _ctx.OptionsParam,
                        LinqExpression.Constant($"{name} ??= ...")),
                    LinqExpression.Assign(result, newValue),
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.SetMethod,
                        LinqExpression.Constant(name), result))),
            result);
    }

    #region Polyglot Extended Features

    private static readonly MethodInfo InvokePipelineMethod =
        typeof(Runtime.Extensions.PipelineOperator).GetMethod(nameof(Runtime.Extensions.PipelineOperator.InvokePipeline))!;

    /// <summary>
    /// Compiles a pipeline expression (x |> f) to a call to
    /// PipelineOperator.InvokePipeline(leftValue, rightCallable, context, options, ct).
    /// </summary>
    internal LinqExpression CompilePipeline(PipelineExpr expr)
    {
        if (expr.Right is IdentifierExpr rightIdentifier)
        {
            // Compile identifier pipelines on the same code path as direct calls: x |> f  =>  f(x)
            return CompileCall(new CallExpr(rightIdentifier, [expr.Left]));
        }

        var left = Compile(expr.Left);
        var right = Compile(expr.Right);

        return LinqExpression.Call(
            InvokePipelineMethod,
            left,
            right,
            _ctx.CurrentContext,
            _ctx.OptionsParam,
            _ctx.CtParam);
    }

    private static readonly MethodInfo GenerateRangeMethod =
        typeof(Runtime.Extensions.RangeHelpers).GetMethod(nameof(Runtime.Extensions.RangeHelpers.GenerateRange))!;

    private static readonly MethodInfo ConvertToInt32Method =
        typeof(Convert).GetMethod(nameof(Convert.ToInt32), [typeof(object)])!;

    /// <summary>
    /// Compiles a range expression (start..end or start..&lt;end) to a call to
    /// RangeHelpers.GenerateRange(int start, int end, bool exclusiveEnd).
    /// Returns IEnumerable&lt;int&gt; boxed as object.
    /// </summary>
    internal LinqExpression CompileRange(RangeExpr expr)
    {
        var start = Compile(expr.Start);
        var end = Compile(expr.End);

        // Convert both operands to int via Convert.ToInt32(object)
        var startInt = LinqExpression.Call(ConvertToInt32Method, start);
        var endInt = LinqExpression.Call(ConvertToInt32Method, end);

        var call = LinqExpression.Call(
            GenerateRangeMethod,
            startInt,
            endInt,
            LinqExpression.Constant(expr.ExclusiveEnd));

        return LinqExpression.Convert(call, typeof(object));
    }

    private static readonly MethodInfo PerformComparisonMethod =
        typeof(Runtime.Extensions.ChainedComparisonHelper).GetMethod(nameof(Runtime.Extensions.ChainedComparisonHelper.PerformComparison))!;

    /// <summary>
    /// Compiles a chained comparison (0 &lt; x &lt; 10) with lazy evaluation and short-circuit.
    /// Each operand is evaluated exactly once; short-circuits on first false comparison.
    /// Emits a block that assigns each operand to a local variable and checks pairs sequentially.
    /// </summary>
    internal LinqExpression CompileChainedComparison(Parsing.ChainedComparisonExpr expr)
    {
        if (TryCompileDirectNumericChainedComparison(expr, out var direct))
            return direct;

        // We build a block expression:
        //   var v0 = compile(operand[0]);
        //   var v1 = compile(operand[1]);
        //   if (!PerformComparison(v0, v1, op0, options)) return (object)false;
        //   var v2 = compile(operand[2]);
        //   if (!PerformComparison(v1, v2, op1, options)) return (object)false;
        //   ...
        //   return (object)true;

        var resultLabel = LinqExpression.Label(typeof(object), "chainResult");
        var variables = new List<System.Linq.Expressions.ParameterExpression>();
        var body = new List<LinqExpression>();

        // Evaluate first operand
        var v0 = LinqExpression.Variable(typeof(object), "v0");
        variables.Add(v0);
        body.Add(LinqExpression.Assign(v0, Compile(expr.Operands[0])));

        for (int i = 0; i < expr.Operators.Count; i++)
        {
            // Evaluate next operand
            var vi = LinqExpression.Variable(typeof(object), $"v{i + 1}");
            variables.Add(vi);
            body.Add(LinqExpression.Assign(vi, Compile(expr.Operands[i + 1])));

            // Check comparison; short-circuit to false if it fails
            var prevVar = variables[i];
            var comparison = LinqExpression.Call(
                PerformComparisonMethod,
                prevVar,
                vi,
                LinqExpression.Constant(expr.Operators[i].Type),
                _ctx.OptionsParam);

            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(comparison),
                LinqExpression.Return(resultLabel, LinqExpression.Constant(false, typeof(object)))));
        }

        // All comparisons passed
        body.Add(LinqExpression.Label(resultLabel, LinqExpression.Constant(true, typeof(object))));

        return LinqExpression.Block(typeof(object), variables.ToArray(), body.ToArray());
    }

    private bool TryCompileDirectNumericChainedComparison(
        Parsing.ChainedComparisonExpr expr,
        out LinqExpression compiled)
    {
        compiled = null!;
        if (expr.Operands.Count < 2 || expr.Operators.Count != expr.Operands.Count - 1)
            return false;

        Type? operandType = null;
        var typedOperands = new List<LinqExpression>(expr.Operands.Count);

        foreach (var operand in expr.Operands)
        {
            var (typedExpression, knownType) = CompileTyped(operand);
            if (!IsDirectableNumericType(knownType))
                return false;

            operandType ??= knownType;
            if (knownType != operandType)
                return false;

            typedOperands.Add(ConvertToNumericType(typedExpression, knownType, operandType));
        }

        if (operandType == null)
            return false;

        var variables = new List<System.Linq.Expressions.ParameterExpression>(typedOperands.Count);
        var body = new List<LinqExpression>(typedOperands.Count + 1);

        for (var i = 0; i < typedOperands.Count; i++)
        {
            var local = LinqExpression.Variable(operandType, $"v{i}");
            variables.Add(local);
            body.Add(LinqExpression.Assign(local, typedOperands[i]));
        }

        LinqExpression? chain = null;
        for (var i = 0; i < expr.Operators.Count; i++)
        {
            var comparison = CreateNumericComparison(expr.Operators[i].Type, variables[i], variables[i + 1]);
            if (comparison == null)
                return false;

            chain = chain == null ? comparison : LinqExpression.AndAlso(chain, comparison);
        }

        if (chain == null)
            return false;

        body.Add(LinqExpression.Convert(chain, typeof(object)));
        compiled = LinqExpression.Block(typeof(object), variables, body);
        return true;
    }

    private static LinqExpression ConvertToNumericType(LinqExpression expression, Type sourceType, Type targetType)
    {
        LinqExpression converted = expression;
        if (converted.Type == typeof(object))
        {
            converted = LinqExpression.Convert(
                LinqExpression.Call(
                    CompilerContext.CoerceNumericMethod,
                    converted,
                    LinqExpression.Constant(targetType, typeof(Type))),
                targetType);
            return converted;
        }

        if (converted.Type != sourceType)
            converted = LinqExpression.Convert(converted, sourceType);

        if (sourceType != targetType)
            converted = LinqExpression.Convert(converted, targetType);

        return converted;
    }

    private static LinqExpression? CreateNumericComparison(TokenType opType, LinqExpression left, LinqExpression right)
    {
        return opType switch
        {
            TokenType.Less => LinqExpression.LessThan(left, right),
            TokenType.LessEqual => LinqExpression.LessThanOrEqual(left, right),
            TokenType.Greater => LinqExpression.GreaterThan(left, right),
            TokenType.GreaterEqual => LinqExpression.GreaterThanOrEqual(left, right),
            TokenType.EqualEqual => LinqExpression.Equal(left, right),
            TokenType.BangEqual => LinqExpression.NotEqual(left, right),
            _ => null
        };
    }

    private static LinqExpression CreateSpaceshipNumericResult(LinqExpression left, LinqExpression right)
    {
        var compareTo = left.Type.GetMethod(nameof(int.CompareTo), [right.Type]);
        if (compareTo != null && compareTo.ReturnType == typeof(int))
            return LinqExpression.Call(left, compareTo, right);

        return LinqExpression.Condition(
            LinqExpression.LessThan(left, right),
            LinqExpression.Constant(-1),
            LinqExpression.Condition(
                LinqExpression.GreaterThan(left, right),
                LinqExpression.Constant(1),
                LinqExpression.Constant(0)));
    }

    #endregion
}
