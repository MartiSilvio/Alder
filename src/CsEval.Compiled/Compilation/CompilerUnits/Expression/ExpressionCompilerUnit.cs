using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation.CompilerUnits;

/// <summary>
/// Compiles expression nodes (literals, binary, unary, member access, calls, lambdas, etc.)
/// to Expression Trees. Receives shared state via CompilerReflectionCache.
/// </summary>
internal sealed class ExpressionCompilerUnit
{
    private readonly CompilerContext _ctx;
    private readonly PatternCompilerUnit _patternUnit;
    private readonly DirectEmitCompilerUnit _directEmit;
    private readonly ExtendedSyntaxCompilerUnit _extendedSyntax;
    private readonly ExpressionBinaryCompiler _binaryCompiler;
    private readonly ExpressionAssignmentCompiler _assignmentCompiler;
    private readonly ExpressionMemberCallCompiler _memberIndexCallCompiler;
    private readonly ExpressionLambdaCompiler _lambdaAndLiteralsCompiler;

    // Lazily set references for cross-unit dispatch
    private ControlFlowCompilerUnit? _controlUnit;

    internal ExpressionCompilerUnit(
        CompilerContext ctx,
        PatternCompilerUnit patternUnit,
        DirectEmitCompilerUnit directEmit,
        ExtendedSyntaxCompilerUnit extendedSyntax)
    {
        _ctx = ctx;
        _patternUnit = patternUnit;
        _directEmit = directEmit;
        _extendedSyntax = extendedSyntax;
        _binaryCompiler = new ExpressionBinaryCompiler(this);
        _assignmentCompiler = new ExpressionAssignmentCompiler(this, _binaryCompiler);
        _memberIndexCallCompiler = new ExpressionMemberCallCompiler(this);
        _lambdaAndLiteralsCompiler = new ExpressionLambdaCompiler(this);
    }

    internal CompilerContext Context => _ctx;
    internal DirectEmitCompilerUnit DirectEmit => _directEmit;
    internal ExtendedSyntaxCompilerUnit ExtendedSyntax => _extendedSyntax;

    internal void SetControlFlowUnit(ControlFlowCompilerUnit controlUnit)
    {
        _controlUnit = controlUnit;
    }

    /// <summary>
    /// Compile a sub-expression by dispatching through the central Compile method.
    /// </summary>
    internal LinqExpression Compile(Expr expr) =>
        CompilerContext.Compile(_ctx, expr, this, _controlUnit!, _patternUnit);

    internal (LinqExpression Expression, Type KnownType) CompileTyped(Expr expr)
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

    private bool TryCompileTypedIdentifier(Expr expr, Type knownType, out LinqExpression compiled)
    {
        compiled = null!;
        if (knownType == typeof(object) || expr is not IdentifierExpr id)
            return false;

        var name = id.Name.Lexeme;

        // Lambda parameters are accessed directly from args[] — skip typed context resolution
        if (_ctx.TryGetLambdaParam(name, out _))
            return false;

        var hasFunctionOrModuleShadow = _ctx.Context.Functions.ContainsKey(name) || _ctx.Context.Modules.ContainsKey(name);
        if (hasFunctionOrModuleShadow)
            return false;

        if (_ctx.Context.TryGetVariableType(name, out var variableType) &&
            variableType == knownType)
        {
            var typedVariableGetter = CompilerReflectionCache.GetVariableTypedMethodFor(knownType);
            var directRead = LinqExpression.Call(
                typedVariableGetter,
                LinqExpression.Constant(name),
                _ctx.CurrentContext);
            compiled = EmitLazyTypedIdentifierRead(name, knownType, directRead);
            return true;
        }

        var typedResolver = CompilerReflectionCache.GetResolveIdentifierTypedMethod(knownType);
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
        if (!_ctx.TryGetOrCreateLazyIdentifierSlot(name, valueType, directRead, out var valueVar))
            return directRead;

        return valueVar;
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
        if (_ctx.TryGetLambdaParam(id.Name.Lexeme, out var lambdaArg))
            return lambdaArg;

        return LinqExpression.Call(
            CompilerReflectionCache.ResolveIdentifierMethod,
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
                CompilerReflectionCache.ResolveTypeMethod,
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
            CompilerReflectionCache.ValidateThrowOperandMethod,
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

        // Resolve type via context's TypeResolver then call ConstructionRuntime.InvokeConstructor(type, args)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(expr.TypeName));

        LinqExpression result = LinqExpression.Call(
            CompilerReflectionCache.InvokeConstructorMethod,
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
                        CompilerReflectionCache.ApplyPropertyInitializerMethod,
                        objVar,
                        LinqExpression.Constant(entry.PropertyName),
                        value,
                        _ctx.OptionsParam,
                        _ctx.CurrentContext));
                }
                else
                {
                    statements.Add(LinqExpression.Call(
                        CompilerReflectionCache.ApplyCollectionInitializerMethod,
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

        // Resolve element type via context's TypeResolver then call ConstructionRuntime.CreateTypedArray(elementType, sizeValue)
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        return LinqExpression.Call(
            CompilerReflectionCache.CreateTypedArrayFromTypeNameMethod,
            resolvedType,
            size);
    }

    internal LinqExpression CompileMultiDimTypedArrayCreation(MultiDimTypedArrayCreationExpr expr)
    {
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        var sizesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Sizes.Select(s => Compile(s)));

        return LinqExpression.Call(
            CompilerReflectionCache.CreateMultiDimArrayMethod,
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
                    LinqExpression.Call(CompilerReflectionCache.MultiDimArrayGetMethod, targetVar, indicesArray)));
        }

        return LinqExpression.Call(CompilerReflectionCache.MultiDimArrayGetMethod, obj, indicesArray);
    }

    internal LinqExpression CompileMultiDimIndexAssign(MultiDimIndexAssignExpr expr)
    {
        var obj = Compile(expr.Object);
        var indicesArray = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Indices.Select(i => Compile(i)));
        var value = Compile(expr.Value);

        return LinqExpression.Call(CompilerReflectionCache.MultiDimArraySetMethod, obj, indicesArray, value);
    }

    internal LinqExpression CompileTypedArrayLiteral(TypedArrayLiteralExpr expr)
    {
        // Compile the array literal elements (returns a typed array from CreateTypedArray)
        var arrayLiteral = CompileArrayLiteral(expr.Elements);

        // Resolve the target element type
        var resolvedType = LinqExpression.Call(
            _ctx.TypeResolverExpr,
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(expr.ElementTypeName));

        // Convert the source array to the typed array T[]
        // ConvertArrayToTyped accepts object (any Array) so no cast to object?[] needed
        return LinqExpression.Call(
            CompilerReflectionCache.ConvertArrayToTypedMethod,
            arrayLiteral,
            resolvedType);
    }

    internal LinqExpression CompileTuple(TupleExpr expr)
    {
        // Compile each element expression into an object[] array
        var elementsInit = LinqExpression.NewArrayInit(
            typeof(object),
            expr.Elements.Select(e => Compile(e.Expression)));

        // Call ConstructionRuntime.CreateTuple(elements)
        return LinqExpression.Call(
            CompilerReflectionCache.CreateTupleMethod,
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

        // Call ConstructionRuntime.DeconstructTuple(value, variableNames, context)
        return LinqExpression.Call(
            CompilerReflectionCache.DeconstructTupleMethod,
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
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(def.TypeToken.Value.Lexeme));

        return LinqExpression.Call(
            CompilerReflectionCache.GetDefaultValueMethod,
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
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(cast.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerReflectionCache.ExplicitCastMethod,
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
            CompilerReflectionCache.ResolveTypeMethod,
            LinqExpression.Constant(asExpr.TargetType.Lexeme));

        return LinqExpression.Call(
            CompilerReflectionCache.TryAsMethod,
            value,
            resolvedType);
    }

    internal LinqExpression CompileBinary(BinaryExpr b) =>
        _binaryCompiler.CompileBinary(b);

    internal LinqExpression CompileLogical(LogicalExpr l) =>
        _binaryCompiler.CompileLogical(l);

    internal LinqExpression CompileConditional(ConditionalExpr c) =>
        _binaryCompiler.CompileConditional(c);

    internal LinqExpression CompileNullCoalesce(NullCoalesceExpr n) =>
        _binaryCompiler.CompileNullCoalesce(n);

    internal LinqExpression CompileVariableDecl(VariableDeclExpr v) =>
        _assignmentCompiler.CompileVariableDecl(v);

    internal LinqExpression CompileAssign(AssignExpr a) =>
        _assignmentCompiler.CompileAssign(a);

    internal LinqExpression CompileCompoundAssign(CompoundAssignExpr ca) =>
        _assignmentCompiler.CompileCompoundAssign(ca);

    internal LinqExpression CompileMemberCompoundAssign(MemberCompoundAssignExpr expr) =>
        _assignmentCompiler.CompileMemberCompoundAssign(expr);

    internal LinqExpression CompileIndexCompoundAssign(IndexCompoundAssignExpr expr) =>
        _assignmentCompiler.CompileIndexCompoundAssign(expr);

    internal LinqExpression CompileIndexAssign(IndexAssignExpr expr) =>
        _assignmentCompiler.CompileIndexAssign(expr);

    internal LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc) =>
        _assignmentCompiler.CompileIncrementDecrement(inc);

    internal LinqExpression CompileMemberNullCoalesceAssign(MemberNullCoalesceAssignExpr expr) =>
        _assignmentCompiler.CompileMemberNullCoalesceAssign(expr);

    internal LinqExpression CompileIndexNullCoalesceAssign(IndexNullCoalesceAssignExpr expr) =>
        _assignmentCompiler.CompileIndexNullCoalesceAssign(expr);

    internal LinqExpression CompileMemberIncrement(MemberIncrementExpr expr) =>
        _assignmentCompiler.CompileMemberIncrement(expr);

    internal LinqExpression CompileIndexIncrement(IndexIncrementExpr expr) =>
        _assignmentCompiler.CompileIndexIncrement(expr);

    internal LinqExpression CompileMemberAccess(MemberAccessExpr m) =>
        _memberIndexCallCompiler.CompileMemberAccess(m);

    internal LinqExpression CompileIndexAccess(IndexAccessExpr expr) =>
        _memberIndexCallCompiler.CompileIndexAccess(expr);

    internal LinqExpression CompileSlice(SliceExpr expr) =>
        _memberIndexCallCompiler.CompileSlice(expr);

    internal LinqExpression CompileCall(CallExpr call) =>
        _memberIndexCallCompiler.CompileCall(call);

    internal LinqExpression CompileOutArg(OutArgExpr outArg) =>
        _memberIndexCallCompiler.CompileOutArg(outArg);

    internal LinqExpression CompileLambda(LambdaExpr lambda) =>
        _lambdaAndLiteralsCompiler.CompileLambda(lambda);

    internal LinqExpression CompileArrayLiteral(ArrayLiteralExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileArrayLiteral(expr);

    internal LinqExpression CompileObjectLiteral(ObjectLiteralExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileObjectLiteral(expr);

    internal LinqExpression CompileInterpolatedString(InterpolatedStringExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileInterpolatedString(expr);

    internal LinqExpression CompileMemberAssign(MemberAssignExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileMemberAssign(expr);

    internal LinqExpression CompileNullCoalesceAssign(NullCoalesceAssignExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileNullCoalesceAssign(expr);

    internal LinqExpression CompilePipeline(PipelineExpr expr) =>
        _lambdaAndLiteralsCompiler.CompilePipeline(expr);

    internal LinqExpression CompileRange(RangeExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileRange(expr);

    internal LinqExpression CompileChainedComparison(Parsing.ChainedComparisonExpr expr) =>
        _lambdaAndLiteralsCompiler.CompileChainedComparison(expr);

}
