using System.Collections.Concurrent;
using System.Linq.Expressions;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Delegate type for IL-compiled expressions.
/// </summary>
internal delegate object? ILCompiledDelegate(
    CsEvalContext context,
    CsEvalOptions options,
    CancellationToken ct);

/// <summary>
/// Shared compilation state passed to all compiler units via composition.
/// Holds parameters, context stack, control stack, return handling, and cached method lookups.
/// Also serves as the entry point for compilation via TryCompile.
/// </summary>
internal sealed class CompilerContext
{
    internal readonly CsEvalContext Context;
    internal readonly CsEvalOptions Options;
    internal readonly TypeInferrer TypeInferrer;

    // Parameters for the compiled lambda
    internal readonly ParameterExpression ContextParam;
    internal readonly ParameterExpression OptionsParam;
    internal readonly ParameterExpression CtParam;

    // Current context expression (may be child context in nested scopes)
    internal LinqExpression CurrentContext;

    // Stack of parent context variables for scope restoration
    internal readonly Stack<ParameterExpression> ContextStack = new();

    // Loop/Switch stack
    internal readonly Stack<ControlFlowContext> ControlStack = new();

    // Return handling
    internal readonly LabelTarget ReturnLabel;
    internal readonly ParameterExpression ReturnValue;

    // Stack for nested return contexts (e.g., lambdas with block bodies)
    private readonly Stack<(LabelTarget Label, ParameterExpression Value)> _returnStack = new();

    /// <summary>
    /// Expression that accesses TypeResolver from the current context: context.TypeResolver.
    /// Used by compilation units to emit type resolution calls at runtime.
    /// </summary>
    internal LinqExpression TypeResolverExpr =>
        LinqExpression.Call(CurrentContext, GetTypeResolverProperty);

    // Recursion depth tracking to prevent stack overflow in Compile
    internal int CompileDepth;

    // Checked/unchecked overflow context for arithmetic operations
    internal bool IsChecked;
    internal int CatchDepth;
    internal bool UseLazyTypedIdentifierReads;

    private readonly Dictionary<(string Name, Type Type), (ParameterExpression Value, ParameterExpression Initialized)> _lazyIdentifierSlots = new();
    internal readonly List<ParameterExpression> LazyIdentifierVariables = [];

    internal record struct ControlFlowContext(LabelTarget BreakTarget, LabelTarget? ContinueTarget, bool IsLoop);

    #region Cached MethodInfo

    internal static readonly MethodInfo GetMethod = typeof(CsEvalContext).GetMethod("Get", [typeof(string)])!;
    internal static readonly MethodInfo SetMethod = typeof(CsEvalContext).GetMethod("Set", [typeof(string), typeof(object)])!;
    internal static readonly MethodInfo DefineMethod = typeof(CsEvalContext).GetMethod("Define", [typeof(string), typeof(object)])!;
    internal static readonly MethodInfo DefineWithTypeMethod = typeof(CsEvalContext).GetMethod("Define", [typeof(string), typeof(object), typeof(Type)])!;
    internal static readonly MethodInfo DefineNewMethod = typeof(CsEvalContext).GetMethod("DefineNew", [typeof(string), typeof(object), typeof(Type)])!;
    internal static readonly MethodInfo TryGetVariableTypeMethod = typeof(CsEvalContext).GetMethod("TryGetVariableType")!;
    internal static readonly MethodInfo CreateChildMethod = typeof(CsEvalContext).GetMethod("CreateChild")!;
    internal static readonly MethodInfo RequireBooleanMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBoolean))!;
    internal static readonly MethodInfo RequireBooleanForLogicalOperatorMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBooleanForLogicalOperator))!;
    internal static readonly MethodInfo GetTypeResolverProperty = typeof(CsEvalContext).GetProperty(nameof(CsEvalContext.TypeResolver), BindingFlags.NonPublic | BindingFlags.Instance)!.GetGetMethod(true)!;
    internal static readonly MethodInfo ResolveTypeMethod = typeof(TypeResolver).GetMethod(nameof(TypeResolver.ResolveType))!;
    internal static readonly MethodInfo InvokeConstructorMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.InvokeConstructor))!;
    internal static readonly MethodInfo CreateTypedArrayFromTypeNameMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateTypedArray))!;
    internal static readonly MethodInfo ConvertArrayToTypedMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ConvertArrayToTyped))!;
    internal static readonly MethodInfo CreateTupleMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateTuple))!;
    internal static readonly MethodInfo DeconstructTupleMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.DeconstructTuple))!;
    internal static readonly MethodInfo GetDefaultValueMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GetDefaultValue), [typeof(Type)])!;
    internal static readonly MethodInfo IsNullableTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsNullableType))!;
    internal static readonly MethodInfo GetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetMember))!;
    internal static readonly MethodInfo GetIndexMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetIndex))!;
    internal static readonly MethodInfo GetSliceMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    internal static readonly MethodInfo GetSliceStepMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetSlice), [typeof(object), typeof(object), typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    internal static readonly MethodInfo SetIndexMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetIndex), [typeof(object), typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    internal static readonly MethodInfo SetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetMember))!;
    internal static readonly MethodInfo ListAddMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.Add))!;
    internal static readonly MethodInfo ListAddRangeMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.AddRange))!;
    internal static readonly ConstructorInfo ListCtor = typeof(List<object?>).GetConstructor(Type.EmptyTypes)!;
    internal static readonly ConstructorInfo ExpandoObjectCtor = typeof(System.Dynamic.ExpandoObject).GetConstructor(Type.EmptyTypes)!;
    internal static readonly ConstructorInfo StringBuilderCtor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!;
    internal static readonly MethodInfo StringBuilderAppendMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)])!;
    internal static readonly MethodInfo StringBuilderToStringMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes)!;
    internal static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;
    // Spread and collection literal helpers
    internal static readonly MethodInfo SpreadIntoDictMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.SpreadIntoDict))!;
    internal static readonly MethodInfo SpreadIntoListMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.SpreadIntoList))!;
    internal static readonly MethodInfo CreateTypedArrayMethod = typeof(SpreadHelpers).GetMethod(nameof(SpreadHelpers.CreateTypedArray))!;
    internal static readonly MethodInfo ThrowIfCancellationRequestedMethod = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
    internal static readonly MethodInfo ApplyPropertyInitializerMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ApplyPropertyInitializer))!;
    internal static readonly MethodInfo ApplyCollectionInitializerMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ApplyCollectionInitializer))!;
    internal static readonly MethodInfo CreateMultiDimArrayMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateMultiDimArray))!;
    internal static readonly MethodInfo MultiDimArrayGetMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.MultiDimArrayGet))!;
    internal static readonly MethodInfo MultiDimArraySetMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.MultiDimArraySet))!;
    internal static readonly MethodInfo CheckExecutionConstraintsMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckExecutionConstraints))!;
    internal static readonly MethodInfo GetConstraintStateProperty = typeof(CsEvalContext).GetProperty(nameof(CsEvalContext.ConstraintState), BindingFlags.NonPublic | BindingFlags.Instance)!.GetGetMethod(true)!;
    internal static readonly MethodInfo GetConstraintsProperty = typeof(CsEvalOptions).GetProperty(nameof(CsEvalOptions.Constraints))!.GetGetMethod()!;
    internal static readonly MethodInfo GetEnumeratorMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetEnumerator))!;
    internal static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
    internal static readonly MethodInfo GetCurrentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!;
    internal static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;
    internal static readonly MethodInfo CheckAllowAssignmentMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckAllowAssignment))!;
    internal static readonly MethodInfo CheckAllowIndexSetMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckAllowIndexSet))!;
    internal static readonly MethodInfo CheckNullCoalesceAssignAllowedMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckNullCoalesceAssignAllowed))!;
    internal static readonly MethodInfo DisposeResourceMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.DisposeResource))!;
    internal static readonly MethodInfo ValidateLockObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ValidateLockObject))!;
    internal static readonly MethodInfo ValidateThrowOperandMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ValidateThrowOperand))!;
    internal static readonly MethodInfo ValidateCompoundAssignmentMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ValidateCompoundAssignment))!;
    internal static readonly MethodInfo EvaluateCatchWhenGuardMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EvaluateCatchWhenGuard))!;
    internal static readonly MethodInfo ValidateAndCoerceTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ValidateAndCoerceType), [typeof(Type), typeof(object), typeof(string)])!;
    internal static readonly MethodInfo ExplicitCastMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ExplicitCast), [typeof(object), typeof(Type), typeof(Type), typeof(bool)])!;
    internal static readonly MethodInfo IsTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsType), [typeof(object), typeof(Type)])!;
    internal static readonly MethodInfo TryAsMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.TryAs), [typeof(object), typeof(Type)])!;
    internal static readonly MethodInfo GuardReflectionLeakMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GuardReflectionLeak), [typeof(object), typeof(string)])!;
    internal static readonly MethodInfo GuardReflectionLeakTypedMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.GuardReflectionLeakTyped))!;
    internal static readonly MethodInfo CoerceNumericMethod = typeof(TypeHelpers).GetMethod("CoerceNumeric", BindingFlags.NonPublic | BindingFlags.Static)!;
    internal static readonly MethodInfo InvokeCallMethod = typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeCall))!;
    internal static readonly MethodInfo InvokeMemberCallMethod = typeof(Runtime.MethodInvoker).GetMethod(nameof(Runtime.MethodInvoker.InvokeMemberCall))!;
    internal static readonly MethodInfo GetVariableTypedMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetVariableTyped))!;
    internal static readonly MethodInfo ResolveIdentifierMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ResolveIdentifier))!;
    internal static readonly MethodInfo ResolveIdentifierTypedMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ResolveIdentifierTyped))!;
    internal static readonly MethodInfo InvokeIdentifierCallMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.InvokeIdentifierCall))!;
    internal static readonly MethodInfo ConditionalTypePromotionMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ConditionalTypePromotion))!;
    internal static readonly ConstructorInfo NamedArgCtor = typeof(NamedArg).GetConstructor([typeof(string), typeof(object)])!;
    internal static readonly ConstructorInfo CompiledLambdaValueCtor =
        typeof(CompiledLambdaValue).GetConstructor([
            typeof(List<string>),
            typeof(Func<object?[], CsEvalContext, object?>),
            typeof(CsEvalContext)
        ])!;
    internal static readonly MethodInfo GetLambdaArgMethod =
        typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetLambdaArg))!;
    internal static readonly MethodInfo StringFormatMethod =
        typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object)])!;

    internal static readonly ConstructorInfo OutArgMarkerCtor =
        typeof(OutArgMarker).GetConstructor([typeof(string), typeof(string), typeof(bool)])!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> ResolveIdentifierTypedMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> GetVariableTypedMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> GuardReflectionLeakTypedMethodCache = new();

    #endregion

    internal static MethodInfo GetResolveIdentifierTypedMethod(Type valueType) =>
        ResolveIdentifierTypedMethodCache.GetOrAdd(
            valueType,
            static t => ResolveIdentifierTypedMethod.MakeGenericMethod(t));

    internal static MethodInfo GetVariableTypedMethodFor(Type valueType) =>
        GetVariableTypedMethodCache.GetOrAdd(
            valueType,
            static t => GetVariableTypedMethod.MakeGenericMethod(t));

    internal static MethodInfo GetGuardReflectionLeakTypedMethod(Type valueType) =>
        GuardReflectionLeakTypedMethodCache.GetOrAdd(
            valueType,
            static t => GuardReflectionLeakTypedMethod.MakeGenericMethod(t));

    internal bool TryGetOrCreateLazyIdentifierSlot(
        string name,
        Type valueType,
        out ParameterExpression valueVar,
        out ParameterExpression initializedVar)
    {
        valueVar = null!;
        initializedVar = null!;

        if (!UseLazyTypedIdentifierReads)
            return false;

        var key = (name, valueType);
        if (_lazyIdentifierSlots.TryGetValue(key, out var existing))
        {
            valueVar = existing.Value;
            initializedVar = existing.Initialized;
            return true;
        }

        var suffix = _lazyIdentifierSlots.Count;
        valueVar = LinqExpression.Variable(valueType, $"idCacheValue_{suffix}");
        initializedVar = LinqExpression.Variable(typeof(bool), $"idCacheReady_{suffix}");

        _lazyIdentifierSlots[key] = (valueVar, initializedVar);
        LazyIdentifierVariables.Add(valueVar);
        LazyIdentifierVariables.Add(initializedVar);
        return true;
    }

    private CompilerContext(CsEvalContext context, CsEvalOptions options)
    {
        Context = context;
        Options = options;
        TypeInferrer = new TypeInferrer(context, options.MaxExpressionDepth);

        ContextParam = LinqExpression.Parameter(typeof(CsEvalContext), "context");
        OptionsParam = LinqExpression.Parameter(typeof(CsEvalOptions), "options");
        CtParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

        // Current context starts as the parameter
        CurrentContext = ContextParam;

        // Return handling - we use a label at the end to handle early returns
        ReturnLabel = LinqExpression.Label(typeof(object), "return");
        ReturnValue = LinqExpression.Variable(typeof(object), "returnValue");
    }

    /// <summary>
    /// Pushes a new return context for nested scopes (e.g., lambda bodies).
    /// </summary>
    internal void PushReturnContext(LabelTarget label, ParameterExpression value)
    {
        _returnStack.Push((label, value));
    }

    /// <summary>
    /// Pops the current return context, restoring the previous one.
    /// </summary>
    internal void PopReturnContext()
    {
        _returnStack.Pop();
    }

    /// <summary>
    /// Gets the current active return label (from stack if present, otherwise the default).
    /// </summary>
    internal LabelTarget CurrentReturnLabel => _returnStack.Count > 0 ? _returnStack.Peek().Label : ReturnLabel;

    /// <summary>
    /// Gets the current active return value variable (from stack if present, otherwise the default).
    /// </summary>
    internal ParameterExpression CurrentReturnValue => _returnStack.Count > 0 ? _returnStack.Peek().Value : ReturnValue;

    /// <summary>
    /// Attempt to compile an AST to IL. Returns (delegate, null) on success, or (null, reason) on failure.
    /// </summary>
    public static (ILCompiledDelegate? Delegate, string? FailureReason, Exception? FailureException) TryCompile(Expr ast, CsEvalContext context, CsEvalOptions options)
    {
        var ctx = new CompilerContext(context, options);

        try
        {
            // Pre-infer types for the entire AST so variable types are known during compilation
            ctx.TypeInferrer.InferAll(ast);
            ctx.UseLazyTypedIdentifierReads = CanUseLazyTypedIdentifierReads(ast);

            // Create compilation units
            var helpers = new CompilerHelpers(ctx);
            var patternUnit = new PatternCompilerUnit(ctx);
            var expressionUnit = new ExpressionCompilerUnit(ctx, patternUnit);
            var controlFlowUnit = new ControlFlowCompilerUnit(ctx, helpers);

            // Wire cross-references
            patternUnit.SetExpressionUnit(expressionUnit);
            controlFlowUnit.SetExpressionUnit(expressionUnit);
            controlFlowUnit.SetPatternUnit(patternUnit);
            expressionUnit.SetControlFlowUnit(controlFlowUnit);

            var body = Compile(ctx, ast, expressionUnit, controlFlowUnit, patternUnit);
            if (body.Type != typeof(object))
                body = LinqExpression.Convert(body, typeof(object));

            // Wrap in a block that:
            // 1. Executes the body and stores result
            // 2. Returns via label (for early returns) or falls through with body result
            var blockVariables = new List<ParameterExpression>(1 + ctx.LazyIdentifierVariables.Count)
            {
                ctx.ReturnValue
            };
            if (ctx.LazyIdentifierVariables.Count > 0)
                blockVariables.AddRange(ctx.LazyIdentifierVariables);

            var fullBody = LinqExpression.Block(
                blockVariables,
                // Store body result in returnValue so we can use it as default for label
                LinqExpression.Assign(ctx.ReturnValue, body),
                // Label with returnValue as default - early returns jump here, normal flow uses body result
                LinqExpression.Label(ctx.ReturnLabel, ctx.ReturnValue)
            );

            var lambda = LinqExpression.Lambda<ILCompiledDelegate>(
                fullBody,
                ctx.ContextParam,
                ctx.OptionsParam,
                ctx.CtParam);

            return (options.ExpressionCompiler.Compile(lambda), null, null);
        }
        catch (CsEvalDepthException)
        {
            throw; // Depth limits are recoverable — let them propagate so callers can surface them
        }
        catch (Exception ex)
        {
            var canCompileResult = CanCompile(ast);
            return (null, canCompileResult ?? ex.Message, ex);
        }
    }

    private static bool CanUseLazyTypedIdentifierReads(Expr ast)
    {
        // Enable only for side-effect-free expression trees so lazy read-caching cannot
        // hide writes or external side effects between identifier reads.
        var stack = new Stack<Expr>();
        stack.Push(ast);

        while (stack.Count > 0)
        {
            switch (stack.Pop())
            {
                case LiteralExpr:
                case IdentifierExpr:
                case TypeReferenceExpr:
                case NameofExpr:
                case DefaultExpr:
                case TypeofExpr:
                case SizeofExpr:
                    continue;

                case UnaryExpr { Op.Type: TokenType.Minus or TokenType.Plus or TokenType.Bang or TokenType.Tilde } unary:
                    stack.Push(unary.Right);
                    continue;

                case BinaryExpr binary when IsCompilableBinaryOp(binary.Op.Type):
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    continue;

                case LogicalExpr logical:
                    stack.Push(logical.Left);
                    stack.Push(logical.Right);
                    continue;

                case ConditionalExpr conditional:
                    stack.Push(conditional.Condition);
                    stack.Push(conditional.ThenBranch);
                    stack.Push(conditional.ElseBranch);
                    continue;

                case NullCoalesceExpr coalesce:
                    stack.Push(coalesce.Left);
                    stack.Push(coalesce.Right);
                    continue;

                case CastExpr cast:
                    stack.Push(cast.Expression);
                    continue;

                case CheckedExpr checkedExpr:
                    stack.Push(checkedExpr.Expression);
                    continue;

                case ChainedComparisonExpr chain:
                    for (var i = 0; i < chain.Operands.Count; i++)
                        stack.Push(chain.Operands[i]);
                    continue;

                case RangeExpr range:
                    stack.Push(range.Start);
                    stack.Push(range.End);
                    continue;

                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Compile an expression to an Expression Tree by dispatching to the correct compilation unit.
    /// </summary>
    internal static LinqExpression Compile(
        CompilerContext ctx,
        Expr expr,
        ExpressionCompilerUnit exprUnit,
        ControlFlowCompilerUnit controlUnit,
        PatternCompilerUnit patternUnit)
    {
        ctx.CompileDepth++;
        if (ctx.CompileDepth > ctx.Options.MaxExpressionDepth)
            throw new CsEvalDepthException("compilation", ctx.Options.MaxExpressionDepth);

        try
        {
            return expr switch
            {
                // Expression nodes
                LiteralExpr lit => exprUnit.CompileLiteral(lit),
                IdentifierExpr id => exprUnit.CompileIdentifier(id),
                TypeReferenceExpr typeRef => exprUnit.CompileTypeReference(typeRef),
                DefaultExpr def => exprUnit.CompileDefault(def),
                NameofExpr nameofExpr => LinqExpression.Constant(nameofExpr.Name, typeof(object)),
                TypeofExpr typeofExpr => exprUnit.CompileTypeof(typeofExpr),
                SizeofExpr sizeofExpr => exprUnit.CompileSizeof(sizeofExpr),
                ObjectCreationExpr oc => exprUnit.CompileObjectCreation(oc),
                TypedArrayCreationExpr tac => exprUnit.CompileTypedArrayCreation(tac),
                TypedArrayLiteralExpr tal => exprUnit.CompileTypedArrayLiteral(tal),
                TupleExpr tuple => exprUnit.CompileTuple(tuple),
                DeconstructionExpr deconstruction => exprUnit.CompileDeconstruction(deconstruction),
                ThrowExpr throwExpr => exprUnit.CompileThrow(throwExpr),
                UnaryExpr u => exprUnit.CompileUnary(u),
                CastExpr cast => exprUnit.CompileCast(cast),
                AsExpr asExpr => exprUnit.CompileAs(asExpr),
                BinaryExpr b => exprUnit.CompileBinary(b),
                LogicalExpr l => exprUnit.CompileLogical(l),
                ConditionalExpr c => exprUnit.CompileConditional(c),
                NullCoalesceExpr n => exprUnit.CompileNullCoalesce(n),
                MemberAccessExpr m => exprUnit.CompileMemberAccess(m),
                IndexAccessExpr idx => exprUnit.CompileIndexAccess(idx),
                SliceExpr slice => exprUnit.CompileSlice(slice),
                VariableDeclExpr v => exprUnit.CompileVariableDecl(v),
                AssignExpr a => exprUnit.CompileAssign(a),
                CompoundAssignExpr ca => exprUnit.CompileCompoundAssign(ca),
                MemberCompoundAssignExpr mca => exprUnit.CompileMemberCompoundAssign(mca),
                IndexCompoundAssignExpr ica => exprUnit.CompileIndexCompoundAssign(ica),
                IndexAssignExpr ia => exprUnit.CompileIndexAssign(ia),
                IncrementDecrementExpr inc => exprUnit.CompileIncrementDecrement(inc),
                MemberIncrementExpr mi => exprUnit.CompileMemberIncrement(mi),
                IndexIncrementExpr ii => exprUnit.CompileIndexIncrement(ii),
                CallExpr call => exprUnit.CompileCall(call),
                LambdaExpr lambda => exprUnit.CompileLambda(lambda),
                ArrayLiteralExpr arr => exprUnit.CompileArrayLiteral(arr),
                ObjectLiteralExpr obj => exprUnit.CompileObjectLiteral(obj),
                NewExpr newExpr => Compile(ctx, newExpr.Initializer, exprUnit, controlUnit, patternUnit),
                InterpolatedStringExpr interp => exprUnit.CompileInterpolatedString(interp),
                MemberAssignExpr ma => exprUnit.CompileMemberAssign(ma),
                NullCoalesceAssignExpr nca => exprUnit.CompileNullCoalesceAssign(nca),
                MemberNullCoalesceAssignExpr mnca => exprUnit.CompileMemberNullCoalesceAssign(mnca),
                IndexNullCoalesceAssignExpr inca => exprUnit.CompileIndexNullCoalesceAssign(inca),

                // Pattern nodes
                IsPatternExpr isExpr => patternUnit.CompileIsPattern(isExpr),
                SwitchExpressionExpr se => patternUnit.CompileSwitchExpression(se),

                // Control flow nodes
                TryCatchFinallyExpr tcf => controlUnit.CompileTryCatchFinally(tcf),
                UsingStatementExpr usingStmt => controlUnit.CompileUsing(usingStmt),
                LockStatementExpr lockStmt => controlUnit.CompileLock(lockStmt),
                ThrowStatementExpr => exprUnit.CompileThrowStatement(),
                BlockExpr block => controlUnit.CompileBlock(block),
                IfStatementExpr ifStmt => controlUnit.CompileIf(ifStmt),
                SwitchStatementExpr switchStmt => controlUnit.CompileSwitch(switchStmt),
                WhileStatementExpr whileStmt => controlUnit.CompileWhile(whileStmt),
                ForStatementExpr forStmt => controlUnit.CompileFor(forStmt),
                DoWhileStatementExpr doWhile => controlUnit.CompileDoWhile(doWhile),
                ForEachStatementExpr forEach => controlUnit.CompileForEach(forEach),
                BreakExpr => controlUnit.CompileBreak(),
                ContinueExpr => controlUnit.CompileContinue(),
                ReturnExpr ret => controlUnit.CompileReturn(ret),

                // Multi-dimensional array operations
                MultiDimIndexAccessExpr mdia => exprUnit.CompileMultiDimIndexAccess(mdia),
                MultiDimTypedArrayCreationExpr mdtac => exprUnit.CompileMultiDimTypedArrayCreation(mdtac),
                MultiDimIndexAssignExpr mdiassign => exprUnit.CompileMultiDimIndexAssign(mdiassign),

                // Out argument (compiled to OutArgMarker for MethodInvoker)
                OutArgExpr outArg => exprUnit.CompileOutArg(outArg),

                // Checked/Unchecked
                CheckedExpr checkedExpr => CompileChecked(ctx, checkedExpr, exprUnit, controlUnit, patternUnit),

                // Polyglot Extended Features
                RangeExpr range => exprUnit.CompileRange(range),
                PipelineExpr pipeline => exprUnit.CompilePipeline(pipeline),
                ChainedComparisonExpr chain => exprUnit.CompileChainedComparison(chain),

                // Error cases
                SpreadExpr => throw new CsEvalException("Spread operator can only be used in array or object literals"),
                NamedArgumentExpr => throw new CsEvalException("Named arguments can only be used in method calls"),
                _ => throw new NotSupportedException($"Cannot compile {expr.GetType().Name}")
            };
        }
        finally
        {
            ctx.CompileDepth--;
        }
    }

    private static LinqExpression CompileChecked(
        CompilerContext ctx, CheckedExpr checkedExpr,
        ExpressionCompilerUnit exprUnit, ControlFlowCompilerUnit controlUnit, PatternCompilerUnit patternUnit)
    {
        var previous = ctx.IsChecked;
        ctx.IsChecked = checkedExpr.IsChecked;
        try
        {
            return Compile(ctx, checkedExpr.Expression, exprUnit, controlUnit, patternUnit);
        }
        finally
        {
            ctx.IsChecked = previous;
        }
    }

    /// <summary>
    /// Check if an AST can be IL-compiled.
    /// Uses iterative approach with explicit stack to avoid StackOverflowException on deep expressions.
    /// Returns null if compilable, or a failure reason string if not.
    /// </summary>
    private static string? CanCompile(Expr expr)
    {
        var stack = new Stack<Expr>();
        stack.Push(expr);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case LiteralExpr:
                case IdentifierExpr:
                case TypeReferenceExpr:
                case IncrementDecrementExpr:
                case BreakExpr:
                case ContinueExpr:
                case DefaultExpr:
                case NameofExpr:
                case TypeofExpr:
                case SizeofExpr:
                    // These are always compilable, no children to check
                    break;

                case MemberIncrementExpr mi:
                    stack.Push(mi.Object);
                    break;

                case IndexIncrementExpr ii:
                    stack.Push(ii.Object);
                    stack.Push(ii.Index);
                    break;

                case MemberNullCoalesceAssignExpr mnca:
                    stack.Push(mnca.Object);
                    stack.Push(mnca.Value);
                    break;

                case IndexNullCoalesceAssignExpr inca:
                    stack.Push(inca.Object);
                    stack.Push(inca.Index);
                    stack.Push(inca.Value);
                    break;

                case ObjectCreationExpr oc:
                    foreach (var arg in oc.Arguments)
                        stack.Push(arg);
                    if (oc.Initializer != null)
                        foreach (var entry in oc.Initializer.Entries)
                            stack.Push(entry.Value);
                    break;

                case MultiDimIndexAccessExpr mdia:
                    stack.Push(mdia.Object);
                    foreach (var idx in mdia.Indices) stack.Push(idx);
                    break;

                case MultiDimTypedArrayCreationExpr mdtac:
                    foreach (var size in mdtac.Sizes) stack.Push(size);
                    break;

                case MultiDimIndexAssignExpr mdiassign:
                    stack.Push(mdiassign.Object);
                    foreach (var idx in mdiassign.Indices) stack.Push(idx);
                    stack.Push(mdiassign.Value);
                    break;

                case TypedArrayCreationExpr tac:
                    stack.Push(tac.Size);
                    break;

                case TypedArrayLiteralExpr tal:
                    stack.Push(tal.Elements);
                    break;

                case TupleExpr tuple:
                    foreach (var element in tuple.Elements)
                        stack.Push(element.Expression);
                    break;

                case DeconstructionExpr deconstruction:
                    stack.Push(deconstruction.ValueExpression);
                    break;

                case ThrowExpr throwExpr:
                    stack.Push(throwExpr.Expression);
                    break;

                case TryCatchFinallyExpr tcf:
                    foreach (var stmt in tcf.TryBody)
                        stack.Push(stmt);
                    foreach (var clause in tcf.CatchClauses)
                    {
                        foreach (var stmt in clause.Body)
                            stack.Push(stmt);
                        if (clause.WhenGuard != null)
                            stack.Push(clause.WhenGuard);
                    }
                    if (tcf.FinallyBody != null)
                        foreach (var stmt in tcf.FinallyBody)
                            stack.Push(stmt);
                    break;

                case UsingStatementExpr usingStmt:
                    stack.Push(usingStmt.ResourceDeclaration);
                    stack.Push(usingStmt.Body);
                    break;

                case LockStatementExpr lockStmt:
                    stack.Push(lockStmt.LockObject);
                    stack.Push(lockStmt.Body);
                    break;

                case ThrowStatementExpr:
                    break;

                case UnaryExpr { Op.Type: TokenType.Minus or TokenType.Plus or TokenType.Bang or TokenType.Tilde } u:
                    stack.Push(u.Right);
                    break;

                case UnaryExpr u:
                    return $"Unsupported unary operator '{u.Op.Lexeme}'";

                case CastExpr cast:
                    stack.Push(cast.Expression);
                    break;

                case IsPatternExpr isExpr:
                {
                    stack.Push(isExpr.Expression);
                    var patternReason = CanCompilePattern(isExpr.Pattern);
                    if (patternReason != null)
                        return patternReason;
                    break;
                }

                case SwitchExpressionExpr se:
                    stack.Push(se.Expression);
                    foreach (var arm in se.Arms)
                    {
                        var patternReason = CanCompilePattern(arm.Pattern);
                        if (patternReason != null)
                            return patternReason;
                        if (arm.WhenGuard != null)
                            stack.Push(arm.WhenGuard);
                        stack.Push(arm.Value);
                    }
                    break;

                case AsExpr asExpr:
                    stack.Push(asExpr.Expression);
                    break;

                case BinaryExpr b when IsCompilableBinaryOp(b.Op.Type):
                    stack.Push(b.Left);
                    stack.Push(b.Right);
                    break;

                case BinaryExpr b:
                    return $"Unsupported binary operator '{b.Op.Lexeme}'";

                case LogicalExpr l:
                    stack.Push(l.Left);
                    stack.Push(l.Right);
                    break;

                case ConditionalExpr c:
                    stack.Push(c.Condition);
                    stack.Push(c.ThenBranch);
                    stack.Push(c.ElseBranch);
                    break;

                case NullCoalesceExpr n:
                    stack.Push(n.Left);
                    stack.Push(n.Right);
                    break;

                case MemberAccessExpr m:
                    stack.Push(m.Object);
                    break;

                case IndexAccessExpr idx:
                    stack.Push(idx.Object);
                    stack.Push(idx.Index);
                    break;

                case SliceExpr slice:
                    stack.Push(slice.Target);
                    if (slice.Start != null) stack.Push(slice.Start);
                    if (slice.End != null) stack.Push(slice.End);
                    break;

                case VariableDeclExpr v:
                    stack.Push(v.Initializer);
                    break;

                case AssignExpr a:
                    stack.Push(a.Value);
                    break;

                case CompoundAssignExpr ca when IsCompilableCompoundOp(ca.Op.Type):
                    stack.Push(ca.Value);
                    break;

                case CompoundAssignExpr ca:
                    return $"Unsupported compound operator '{ca.Op.Lexeme}'";

                case MemberCompoundAssignExpr mca when IsCompilableCompoundOp(mca.Operator):
                    stack.Push(mca.Object);
                    stack.Push(mca.Value);
                    break;

                case MemberCompoundAssignExpr:
                    return "Unsupported compound operator on member access";

                case IndexCompoundAssignExpr ica when IsCompilableCompoundOp(ica.Operator):
                    stack.Push(ica.Object);
                    stack.Push(ica.Index);
                    stack.Push(ica.Value);
                    break;

                case IndexCompoundAssignExpr:
                    return "Unsupported compound operator on index access";

                case IndexAssignExpr ia:
                    stack.Push(ia.Object);
                    stack.Push(ia.Index);
                    stack.Push(ia.Value);
                    break;

                case BlockExpr b:
                    foreach (var stmt in b.Statements)
                        stack.Push(stmt);
                    if (b.ReturnExpr != null)
                        stack.Push(b.ReturnExpr);
                    break;

                case IfStatementExpr i:
                    stack.Push(i.Condition);
                    foreach (var stmt in i.ThenStatements)
                        stack.Push(stmt);
                    if (i.ElseStatements != null)
                        foreach (var stmt in i.ElseStatements)
                            stack.Push(stmt);
                    break;

                case SwitchStatementExpr s:
                    stack.Push(s.Expression);
                    foreach (var c in s.Cases)
                    {
                        if (c.CasePattern != null)
                        {
                            var patternReason = CanCompilePattern(c.CasePattern);
                            if (patternReason != null)
                                return patternReason;
                        }
                        if (c.WhenGuard != null)
                            stack.Push(c.WhenGuard);
                        foreach (var stmt in c.Statements)
                            stack.Push(stmt);
                    }
                    break;

                case WhileStatementExpr w:
                    stack.Push(w.Condition);
                    foreach (var stmt in w.Body)
                        stack.Push(stmt);
                    break;

                case ForStatementExpr f:
                    foreach (var init in f.Initializers) stack.Push(init);
                    if (f.Condition != null) stack.Push(f.Condition);
                    foreach (var inc in f.Increments) stack.Push(inc);
                    foreach (var stmt in f.Body)
                        stack.Push(stmt);
                    break;

                case DoWhileStatementExpr d:
                    stack.Push(d.Condition);
                    foreach (var stmt in d.Body)
                        stack.Push(stmt);
                    break;

                case ForEachStatementExpr fe:
                    stack.Push(fe.Collection);
                    foreach (var stmt in fe.Body)
                        stack.Push(stmt);
                    break;

                case ReturnExpr r:
                    if (r.Value != null)
                        stack.Push(r.Value);
                    break;

                case CallExpr call:
                    stack.Push(call.Callee);
                    foreach (var arg in call.Arguments)
                    {
                        if (arg is NamedArgumentExpr namedArg)
                            stack.Push(namedArg.Value);
                        else if (arg is OutArgExpr)
                            { } // OutArgExpr is a leaf node, handled at compile time
                        else
                            stack.Push(arg);
                    }
                    break;

                case LambdaExpr lambda:
                    stack.Push(lambda.Body);
                    break;

                case ArrayLiteralExpr arr:
                    foreach (var elem in arr.Elements)
                        stack.Push(elem);
                    break;

                case ObjectLiteralExpr obj:
                    foreach (var (_, value) in obj.Properties)
                        stack.Push(value);
                    break;

                case NewExpr newExpr:
                    stack.Push(newExpr.Initializer);
                    break;

                case InterpolatedStringExpr interp:
                    foreach (var part in interp.Parts)
                        if (part is ExpressionPart ep)
                            stack.Push(ep.Expression);
                    break;

                case MemberAssignExpr ma:
                    stack.Push(ma.Object);
                    stack.Push(ma.Value);
                    break;

                case NullCoalesceAssignExpr nca:
                    stack.Push(nca.Value);
                    break;

                case SpreadExpr spread:
                    stack.Push(spread.Expression);
                    break;

                case NamedArgumentExpr namedArg:
                    stack.Push(namedArg.Value);
                    break;

                case OutArgExpr:
                    // OutArgExpr as standalone is invalid; inside CallExpr it's handled above
                    break;

                case CheckedExpr checkedExpr:
                    stack.Push(checkedExpr.Expression);
                    break;

                case RangeExpr range:
                    stack.Push(range.Start);
                    stack.Push(range.End);
                    break;

                case PipelineExpr pipeline:
                    stack.Push(pipeline.Left);
                    stack.Push(pipeline.Right);
                    break;

                case ChainedComparisonExpr chain:
                    foreach (var operand in chain.Operands)
                        stack.Push(operand);
                    break;

                default:
                    return $"Unsupported expression type '{current.GetType().Name}'";
            }
        }

        return null; // All expressions are compilable
    }

    /// <summary>
    /// Check if a pattern can be IL-compiled.
    /// Returns null if compilable, or a failure reason string if not.
    /// </summary>
    private static string? CanCompilePattern(Pattern pattern)
    {
        switch (pattern)
        {
            case ConstantPattern:
            case TypePattern:
            case VarPattern:
            case DiscardPattern:
                return null; // always compilable

            case NotPattern np:
                return CanCompilePattern(np.Operand);

            case AndPattern ap:
                return CanCompilePattern(ap.Left) ?? CanCompilePattern(ap.Right);

            case OrPattern op:
                return CanCompilePattern(op.Left) ?? CanCompilePattern(op.Right);

            case ParenthesizedPattern pp:
                return CanCompilePattern(pp.Inner);

            case RelationalPattern:
                return null; // compilable

            case PropertyPattern pp:
                foreach (var (_, subPattern) in pp.Properties)
                {
                    var subResult = CanCompilePattern(subPattern);
                    if (subResult != null)
                        return subResult;
                }
                return null;

            default:
                return $"Unsupported pattern type '{pattern.GetType().Name}'";
        }
    }

    private static bool IsCompilableCompoundOp(TokenType op) => op is
        TokenType.PlusEqual or TokenType.MinusEqual or TokenType.StarEqual or
        TokenType.SlashEqual or TokenType.PercentEqual or
        TokenType.AmpEqual or TokenType.PipeEqual or TokenType.CaretEqual or
        TokenType.LessLessEqual or TokenType.GreaterGreaterEqual or
        TokenType.GreaterGreaterGreaterEqual or TokenType.StarStarEqual;

    private static bool IsCompilableBinaryOp(TokenType op)
    {
        if (op is TokenType.Plus or TokenType.Minus or TokenType.Star or
            TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.EqualEqualEqual or
            TokenType.BangEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual or
            TokenType.Amp or TokenType.Pipe or TokenType.Caret or
            TokenType.LessLess or TokenType.GreaterGreater or
            TokenType.GreaterGreaterGreater or TokenType.StarStar or
            TokenType.In or TokenType.NotIn or
            TokenType.Like or TokenType.NotLike or
            TokenType.EqualTilde or TokenType.BangTilde or
            TokenType.LessEqualGreater)
            return true;

        return false;
    }
}
