using System.Linq.Expressions;
using CsEval.Extensions;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles CsEval AST to IL using Expression Trees for maximum performance and correctness.
/// Expression Trees handle all the complexity of IL generation (try/finally, Leave vs Br, etc.)
/// automatically, eliminating entire classes of bugs that plague raw IL emission.
/// </summary>
internal sealed partial class ILCompiler
{
    public delegate object? ILCompiledDelegate(
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Dictionary<string, Func<object?[], object?>> functions,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer);

    private readonly CsEvalContext _context;
    private readonly CsEvalOptions _options;

    // Parameters for the compiled lambda
    private readonly ParameterExpression _contextParam;
    private readonly ParameterExpression _optionsParam;
    private readonly ParameterExpression _ctParam;
    private readonly ParameterExpression _functionsParam;
    private readonly ParameterExpression _argumentTransformerParam;

    // Current context expression (may be child context in nested scopes)
    private LinqExpression _currentContext;

    // Stack of parent context variables for scope restoration
    private readonly Stack<ParameterExpression> _contextStack = new();

    // Loop/Switch stack
    private readonly Stack<ControlFlowContext> _controlStack = new();

    // Global iteration counter variable (long to avoid overflow issues with int.MaxValue limits)
    private readonly ParameterExpression _iterationCount;

    // Return handling
    private readonly LabelTarget _returnLabel;
    private readonly ParameterExpression _returnValue;

    // Recursion depth tracking to prevent stack overflow in Compile
    private int _compileDepth;
    private const int MaxCompileDepth = 500;

    // Cached MethodInfo for helper methods
    private static readonly MethodInfo GetMethod = typeof(CsEvalContext).GetMethod("Get", [typeof(string)])!;
    private static readonly MethodInfo SetMethod = typeof(CsEvalContext).GetMethod("Set", [typeof(string), typeof(object)])!;
    private static readonly MethodInfo DefineMethod = typeof(CsEvalContext).GetMethod("Define", [typeof(string), typeof(object)])!;
    private static readonly MethodInfo DefineWithTypeMethod = typeof(CsEvalContext).GetMethod("Define", [typeof(string), typeof(object), typeof(Type)])!;
    private static readonly MethodInfo TryGetVariableTypeMethod = typeof(CsEvalContext).GetMethod("TryGetVariableType")!;
    private static readonly MethodInfo CreateChildMethod = typeof(CsEvalContext).GetMethod("CreateChild")!;
    private static readonly MethodInfo RequireBooleanMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.RequireBoolean))!;
    private static readonly MethodInfo ResolveTypeNameMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ResolveTypeName))!;
    private static readonly MethodInfo IsNullableTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsNullableType))!;
    private static readonly MethodInfo AddMethod = typeof(Operators).GetMethod(nameof(Operators.Add), [typeof(object), typeof(object), typeof(CsEvalOptions), typeof(CsEvalContext)])!;
    private static readonly MethodInfo SubtractMethod = typeof(Operators).GetMethod(nameof(Operators.Subtract))!;
    private static readonly MethodInfo MultiplyMethod = typeof(Operators).GetMethod(nameof(Operators.Multiply))!;
    private static readonly MethodInfo DivideMethod = typeof(Operators).GetMethod(nameof(Operators.Divide))!;
    private static readonly MethodInfo ModuloMethod = typeof(Operators).GetMethod(nameof(Operators.Modulo))!;
    private static readonly MethodInfo EqualsMethod = typeof(Operators).GetMethod("Equals", [typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    private static readonly MethodInfo NotEqualsMethod = typeof(Operators).GetMethod(nameof(Operators.NotEquals))!;
    private static readonly MethodInfo LessThanMethod = typeof(Operators).GetMethod(nameof(Operators.LessThan))!;
    private static readonly MethodInfo LessThanOrEqualMethod = typeof(Operators).GetMethod(nameof(Operators.LessThanOrEqual))!;
    private static readonly MethodInfo GreaterThanMethod = typeof(Operators).GetMethod(nameof(Operators.GreaterThan))!;
    private static readonly MethodInfo GreaterThanOrEqualMethod = typeof(Operators).GetMethod(nameof(Operators.GreaterThanOrEqual))!;
    private static readonly MethodInfo BitwiseAndMethod = typeof(Operators).GetMethod(nameof(Operators.BitwiseAnd))!;
    private static readonly MethodInfo BitwiseOrMethod = typeof(Operators).GetMethod(nameof(Operators.BitwiseOr))!;
    private static readonly MethodInfo BitwiseXorMethod = typeof(Operators).GetMethod(nameof(Operators.BitwiseXor))!;
    private static readonly MethodInfo BitwiseNotMethod = typeof(Operators).GetMethod(nameof(Operators.BitwiseNot))!;
    private static readonly MethodInfo LeftShiftMethod = typeof(Operators).GetMethod(nameof(Operators.LeftShift))!;
    private static readonly MethodInfo RightShiftMethod = typeof(Operators).GetMethod(nameof(Operators.RightShift))!;

    private readonly Dictionary<TokenType, ILBinaryOperator> _extensionILOperators;

    private Dictionary<TokenType, ILBinaryOperator> BuildExtensionOperators(CsEvalOptions options)
    {
        var result = new Dictionary<TokenType, ILBinaryOperator>();
        foreach (var ext in options.Extensions)
            foreach (var (tokenType, ilOp) in ext.ILBinaryOperators)
                result[tokenType] = ilOp;
        return result;
    }
    private static readonly MethodInfo GetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetMember))!;
    private static readonly MethodInfo GetIndexMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.GetIndex))!;
    private static readonly MethodInfo SetIndexMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetIndex))!;
    private static readonly MethodInfo SetMemberMethod = typeof(MemberAccess).GetMethod(nameof(MemberAccess.SetMember))!;
    private static readonly MethodInfo ListAddMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.Add))!;
    private static readonly MethodInfo ListAddRangeMethod = typeof(List<object?>).GetMethod(nameof(List<object?>.AddRange))!;
    private static readonly ConstructorInfo ListCtor = typeof(List<object?>).GetConstructor(Type.EmptyTypes)!;
    private static readonly ConstructorInfo ExpandoObjectCtor = typeof(System.Dynamic.ExpandoObject).GetConstructor(Type.EmptyTypes)!;
    private static readonly ConstructorInfo StringBuilderCtor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!;
    private static readonly MethodInfo StringBuilderAppendMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)])!;
    private static readonly MethodInfo StringBuilderToStringMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes)!;
    private static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;
    private static readonly MethodInfo SpreadIntoDictMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.SpreadIntoDict))!;
    private static readonly MethodInfo SpreadIntoListMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.SpreadIntoList))!;
    private static readonly MethodInfo CreateTypedListMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateTypedList))!;
    private static readonly MethodInfo NegateMethod = typeof(Operators).GetMethod(nameof(Operators.Negate))!;
    private static readonly MethodInfo ThrowIfCancellationRequestedMethod = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
    private static readonly MethodInfo CheckIterationLimitMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckIterationLimit))!;
    private static readonly MethodInfo GetEnumeratorMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetEnumerator))!;
    private static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
    private static readonly MethodInfo GetCurrentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetGetMethod()!;
    private static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;
    private static readonly MethodInfo CheckAllowAssignmentMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckAllowAssignment))!;
    private static readonly MethodInfo CheckAllowIndexSetMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckAllowIndexSet))!;
    private static readonly MethodInfo CheckNullCoalesceAssignAllowedMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckNullCoalesceAssignAllowed))!;
    private static readonly MethodInfo ValidateCompoundAssignmentMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ValidateCompoundAssignment))!;
    private static readonly MethodInfo ValidateAndCoerceTypeMethod = typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.ValidateAndCoerceType))!;
    private static readonly MethodInfo InvokeCallMethod = typeof(MethodInvoker).GetMethod(nameof(MethodInvoker.InvokeCall))!;
    private static readonly MethodInfo InvokeMemberCallMethod = typeof(MethodInvoker).GetMethod(nameof(MethodInvoker.InvokeMemberCall))!;
    private static readonly MethodInfo ResolveIdentifierMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ResolveIdentifier))!;
    private static readonly ConstructorInfo NamedArgCtor = typeof(Interpretation.NamedArg).GetConstructor([typeof(string), typeof(object)])!;

    private record struct ControlFlowContext(LabelTarget BreakTarget, LabelTarget? ContinueTarget, bool IsLoop);

    private ILCompiler(CsEvalContext context, CsEvalOptions options)
    {
        _context = context;
        _options = options;
        _extensionILOperators = BuildExtensionOperators(options);

        _contextParam = LinqExpression.Parameter(typeof(CsEvalContext), "context");
        _optionsParam = LinqExpression.Parameter(typeof(CsEvalOptions), "options");
        _ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");
        _functionsParam = LinqExpression.Parameter(typeof(Dictionary<string, Func<object?[], object?>>), "functions");
        _argumentTransformerParam = LinqExpression.Parameter(typeof(Func<MethodInfo, object?[], object?[]>), "argumentTransformer");

        // Current context starts as the parameter
        _currentContext = _contextParam;

        // Iteration counter (long to handle MaxIterations up to int.MaxValue without overflow)
        _iterationCount = LinqExpression.Variable(typeof(long), "iterationCount");

        // Return handling - we use a label at the end to handle early returns
        _returnLabel = LinqExpression.Label(typeof(object), "return");
        _returnValue = LinqExpression.Variable(typeof(object), "returnValue");
    }

    /// <summary>
    /// Attempt to compile an AST to IL. Returns (delegate, null) on success, or (null, reason) on failure.
    /// </summary>
    public static (ILCompiledDelegate? Delegate, string? FailureReason) TryCompile(Expr ast, CsEvalContext context, CsEvalOptions options)
    {
        var compiler = new ILCompiler(context, options);

        var canCompileResult = compiler.CanCompile(ast);
        if (canCompileResult != null)
            return (null, canCompileResult);

        try
        {
            var body = compiler.Compile(ast);

            // Wrap in a block that:
            // 1. Initializes iteration counter to 0
            // 2. Executes the body and stores result
            // 3. Returns via label (for early returns) or falls through with body result
            var fullBody = LinqExpression.Block(
                new[] { compiler._iterationCount, compiler._returnValue },
                LinqExpression.Assign(compiler._iterationCount, LinqExpression.Constant(0L)),
                // Store body result in returnValue so we can use it as default for label
                LinqExpression.Assign(compiler._returnValue, body),
                // Label with returnValue as default - early returns jump here, normal flow uses body result
                LinqExpression.Label(compiler._returnLabel, compiler._returnValue)
            );

            var lambda = LinqExpression.Lambda<ILCompiledDelegate>(
                fullBody,
                compiler._contextParam,
                compiler._optionsParam,
                compiler._ctParam,
                compiler._functionsParam,
                compiler._argumentTransformerParam);

            return (lambda.Compile(), null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Check if an AST can be IL-compiled.
    /// Uses iterative approach with explicit stack to avoid StackOverflowException on deep expressions.
    /// Returns null if compilable, or a failure reason string if not.
    /// </summary>
    private string? CanCompile(Expr expr)
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
                case IncrementDecrementExpr:
                case BreakExpr:
                case ContinueExpr:
                    // These are always compilable, no children to check
                    break;

                case GroupingExpr g:
                    stack.Push(g.Expression);
                    break;

                case UnaryExpr u when u.Op.Type is TokenType.Minus or TokenType.Bang or TokenType.Tilde:
                    stack.Push(u.Right);
                    break;

                case UnaryExpr u:
                    return $"Unsupported unary operator '{u.Op.Lexeme}'";

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
                        if (c.Pattern != null)
                            stack.Push(c.Pattern);
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
                    if (f.Initializer != null) stack.Push(f.Initializer);
                    if (f.Condition != null) stack.Push(f.Condition);
                    if (f.Increment != null) stack.Push(f.Increment);
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

                default:
                    return $"Unsupported expression type '{current.GetType().Name}'";
            }
        }

        return null; // All expressions are compilable
    }

    private static bool IsCompilableCompoundOp(TokenType op) => op is
        TokenType.PlusEqual or TokenType.MinusEqual or TokenType.StarEqual or
        TokenType.SlashEqual or TokenType.PercentEqual or
        TokenType.AmpEqual or TokenType.PipeEqual or TokenType.CaretEqual or
        TokenType.LessLessEqual or TokenType.GreaterGreaterEqual;

    private bool IsCompilableBinaryOp(TokenType op)
    {
        if (op is TokenType.Plus or TokenType.Minus or TokenType.Star or
            TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.EqualEqualEqual or
            TokenType.BangEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual or
            TokenType.Amp or TokenType.Pipe or TokenType.Caret or
            TokenType.LessLess or TokenType.GreaterGreater)
            return true;

        return _extensionILOperators.ContainsKey(op);
    }

    /// <summary>
    /// Compile an expression to an Expression Tree.
    /// </summary>
    private LinqExpression Compile(Expr expr)
    {
        _compileDepth++;
        if (_compileDepth > MaxCompileDepth)
            throw new InvalidOperationException("Expression too deeply nested for IL compilation");

        try
        {
            return expr switch
            {
                LiteralExpr lit => CompileLiteral(lit),
                IdentifierExpr id => CompileIdentifier(id),
                GroupingExpr g => Compile(g.Expression),
                UnaryExpr u => CompileUnary(u),
                BinaryExpr b => CompileBinary(b),
                LogicalExpr l => CompileLogical(l),
                ConditionalExpr c => CompileConditional(c),
                NullCoalesceExpr n => CompileNullCoalesce(n),
                MemberAccessExpr m => CompileMemberAccess(m),
                IndexAccessExpr idx => CompileIndexAccess(idx),
                VariableDeclExpr v => CompileVariableDecl(v),
                AssignExpr a => CompileAssign(a),
                CompoundAssignExpr ca => CompileCompoundAssign(ca),
                IndexAssignExpr ia => CompileIndexAssign(ia),
                IncrementDecrementExpr inc => CompileIncrementDecrement(inc),
                BlockExpr block => CompileBlock(block),
                IfStatementExpr ifStmt => CompileIf(ifStmt),
                SwitchStatementExpr switchStmt => CompileSwitch(switchStmt),
                WhileStatementExpr whileStmt => CompileWhile(whileStmt),
                ForStatementExpr forStmt => CompileFor(forStmt),
                DoWhileStatementExpr doWhile => CompileDoWhile(doWhile),
                ForEachStatementExpr forEach => CompileForEach(forEach),
                BreakExpr => CompileBreak(),
                ContinueExpr => CompileContinue(),
                ReturnExpr ret => CompileReturn(ret),
                CallExpr call => CompileCall(call),
                LambdaExpr lambda => CompileLambda(lambda),
                ArrayLiteralExpr arr => CompileArrayLiteral(arr),
                ObjectLiteralExpr obj => CompileObjectLiteral(obj),
                NewExpr newExpr => Compile(newExpr.Initializer),
                InterpolatedStringExpr interp => CompileInterpolatedString(interp),
                MemberAssignExpr ma => CompileMemberAssign(ma),
                NullCoalesceAssignExpr nca => CompileNullCoalesceAssign(nca),
                SpreadExpr => throw new CsEvalException("Spread operator can only be used in array or object literals"),
                NamedArgumentExpr => throw new CsEvalException("Named arguments can only be used in method calls"),
                _ => throw new NotSupportedException($"Cannot compile {expr.GetType().Name}")
            };
        }
        finally
        {
            _compileDepth--;
        }
    }
}
