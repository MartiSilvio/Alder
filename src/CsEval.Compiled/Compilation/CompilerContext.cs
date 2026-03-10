using System.Linq.Expressions;
using CsEval.Compiled.Compilation.CompilerUnits;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

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
        LinqExpression.Call(CurrentContext, CompilerReflectionCache.GetTypeResolverProperty);

    // Recursion depth tracking to prevent stack overflow in Compile
    internal int CompileDepth;

    // Checked/unchecked overflow context for arithmetic operations
    internal bool IsChecked;
    internal int CatchDepth;
    internal bool UseLazyTypedIdentifierReads;

    // Lambda parameter direct-access: maps parameter names to their args[i] expressions
    // Stack supports nested lambdas. When non-empty, CompileIdentifier checks here first.
    private readonly Stack<Dictionary<string, LinqExpression>> _lambdaParamStack = new();

    internal bool TryGetLambdaParam(string name, out LinqExpression argAccess)
    {
        foreach (var scope in _lambdaParamStack)
        {
            if (scope.TryGetValue(name, out argAccess!))
                return true;
        }
        argAccess = null!;
        return false;
    }

    internal void PushLambdaParams(Dictionary<string, LinqExpression> paramMap) =>
        _lambdaParamStack.Push(paramMap);

    internal void PopLambdaParams() => _lambdaParamStack.Pop();

    private readonly Dictionary<(string Name, Type Type), ParameterExpression> _lazyIdentifierSlots = new();
    internal readonly List<ParameterExpression> LazyIdentifierVariables = [];
    internal readonly List<LinqExpression> LazyIdentifierInitializers = [];

    internal record struct ControlFlowContext(LabelTarget BreakTarget, LabelTarget? ContinueTarget, bool IsLoop);
    internal bool TryGetOrCreateLazyIdentifierSlot(
        string name,
        Type valueType,
        LinqExpression initializer,
        out ParameterExpression valueVar)
    {
        valueVar = null!;

        if (!UseLazyTypedIdentifierReads)
            return false;

        var key = (name, valueType);
        if (_lazyIdentifierSlots.TryGetValue(key, out var existing))
        {
            valueVar = existing;
            return true;
        }

        var suffix = _lazyIdentifierSlots.Count;
        valueVar = LinqExpression.Variable(valueType, $"idCacheValue_{suffix}");

        _lazyIdentifierSlots[key] = valueVar;
        LazyIdentifierVariables.Add(valueVar);
        LazyIdentifierInitializers.Add(LinqExpression.Assign(valueVar, initializer));
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
            var directEmitUnit = new DirectEmitCompilerUnit(ctx);
            var extendedSyntaxUnit = new ExtendedSyntaxCompilerUnit(ctx, directEmitUnit);
            var expressionUnit = new ExpressionCompilerUnit(ctx, patternUnit, directEmitUnit, extendedSyntaxUnit);
            var controlFlowUnit = new ControlFlowCompilerUnit(ctx, helpers);

            // Wire cross-references
            patternUnit.SetExpressionUnit(expressionUnit);
            directEmitUnit.SetExpressionUnit(expressionUnit);
            extendedSyntaxUnit.SetExpressionUnit(expressionUnit);
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

            var blockBody = new List<LinqExpression>(ctx.LazyIdentifierInitializers.Count + 2);
            if (ctx.LazyIdentifierInitializers.Count > 0)
                blockBody.AddRange(ctx.LazyIdentifierInitializers);

            // Store body result in returnValue so we can use it as default for label
            blockBody.Add(LinqExpression.Assign(ctx.ReturnValue, body));
            // Label with returnValue as default - early returns jump here, normal flow uses body result
            blockBody.Add(LinqExpression.Label(ctx.ReturnLabel, ctx.ReturnValue));

            var fullBody = LinqExpression.Block(blockVariables, blockBody);

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
            var canCompileResult = CompileGuard.CanCompile(ast);
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

                case BinaryExpr binary when CompileGuard.IsCompilableBinaryOp(binary.Op.Type):
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

}
