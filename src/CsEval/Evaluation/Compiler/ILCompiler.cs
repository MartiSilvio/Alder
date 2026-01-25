using System.Linq.Expressions;
using CsEval.Parsing;

namespace CsEval.Evaluation.Compiler;

/// <summary>
/// Compiles CsEval AST to IL using Expression Trees for maximum performance and correctness.
/// Expression Trees handle all the complexity of IL generation (try/finally, Leave vs Br, etc.)
/// automatically, eliminating entire classes of bugs that plague raw IL emission.
/// </summary>
internal sealed partial class ILCompiler
{
    // Delegate signature for IL-compiled expressions
    public delegate object? ILCompiledDelegate(EvalContext context, CsEvalOptions options, CancellationToken ct);

    private readonly EvalContext _context;
    private readonly CsEvalOptions _options;

    // Parameters for the compiled lambda
    private readonly ParameterExpression _contextParam;
    private readonly ParameterExpression _optionsParam;
    private readonly ParameterExpression _ctParam;

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
    private static readonly MethodInfo GetMethod = typeof(EvalContext).GetMethod("Get", [typeof(string)])!;
    private static readonly MethodInfo SetMethod = typeof(EvalContext).GetMethod("Set", [typeof(string), typeof(object)])!;
    private static readonly MethodInfo DefineMethod = typeof(EvalContext).GetMethod("Define", [typeof(string), typeof(object)])!;
    private static readonly MethodInfo CreateChildMethod = typeof(EvalContext).GetMethod("CreateChild")!;
    private static readonly MethodInfo IsTruthyMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsTruthy))!;
    private static readonly MethodInfo AddMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Add))!;
    private static readonly MethodInfo SubtractMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Subtract))!;
    private static readonly MethodInfo MultiplyMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Multiply))!;
    private static readonly MethodInfo DivideMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Divide))!;
    private static readonly MethodInfo ModuloMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Modulo))!;
    private static readonly MethodInfo EqualsMethod = typeof(RuntimeHelpers).GetMethod("Equals", [typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    private static readonly MethodInfo NotEqualsMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.NotEquals))!;
    private static readonly MethodInfo LessThanMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.LessThan))!;
    private static readonly MethodInfo LessThanOrEqualMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.LessThanOrEqual))!;
    private static readonly MethodInfo GreaterThanMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GreaterThan))!;
    private static readonly MethodInfo GreaterThanOrEqualMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GreaterThanOrEqual))!;
    private static readonly MethodInfo BitwiseAndMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.BitwiseAnd))!;
    private static readonly MethodInfo BitwiseOrMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.BitwiseOr))!;
    private static readonly MethodInfo BitwiseXorMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.BitwiseXor))!;
    private static readonly MethodInfo GetMemberMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetMember))!;
    private static readonly MethodInfo GetIndexMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetIndex))!;
    private static readonly MethodInfo SetIndexMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.SetIndex))!;
    private static readonly MethodInfo NegateMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.Negate))!;
    private static readonly MethodInfo ThrowIfCancellationRequestedMethod = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
    private static readonly MethodInfo CheckIterationLimitMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckIterationLimit))!;
    private static readonly MethodInfo GetEnumeratorMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetEnumerator))!;
    private static readonly MethodInfo MoveNextMethod = typeof(System.Collections.IEnumerator).GetMethod(nameof(System.Collections.IEnumerator.MoveNext))!;
    private static readonly MethodInfo GetCurrentProperty = typeof(System.Collections.IEnumerator).GetProperty(nameof(System.Collections.IEnumerator.Current))!.GetGetMethod()!;
    private static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;
    private static readonly MethodInfo CheckAllowAssignmentMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CheckAllowAssignment))!;

    private record struct ControlFlowContext(LabelTarget BreakTarget, LabelTarget? ContinueTarget, bool IsLoop);

    private ILCompiler(EvalContext context, CsEvalOptions options)
    {
        _context = context;
        _options = options;

        // Define parameters
        _contextParam = LinqExpression.Parameter(typeof(EvalContext), "context");
        _optionsParam = LinqExpression.Parameter(typeof(CsEvalOptions), "options");
        _ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

        // Current context starts as the parameter
        _currentContext = _contextParam;

        // Iteration counter (long to handle MaxIterations up to int.MaxValue without overflow)
        _iterationCount = LinqExpression.Variable(typeof(long), "iterationCount");

        // Return handling - we use a label at the end to handle early returns
        _returnLabel = LinqExpression.Label(typeof(object), "return");
        _returnValue = LinqExpression.Variable(typeof(object), "returnValue");
    }

    /// <summary>
    /// Attempt to compile an AST to IL. Returns null if the expression cannot be IL-compiled.
    /// </summary>
    public static ILCompiledDelegate? TryCompile(Expr ast, EvalContext context, CsEvalOptions options)
    {
        var compiler = new ILCompiler(context, options);

        if (!compiler.CanCompile(ast))
            return null;

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
                compiler._ctParam);

            return lambda.Compile();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if an AST can be IL-compiled.
    /// Uses iterative approach with explicit stack to avoid StackOverflowException on deep expressions.
    /// </summary>
    private bool CanCompile(Expr expr)
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

                case UnaryExpr u when u.Op.Type is TokenType.Minus or TokenType.Bang:
                    stack.Push(u.Right);
                    break;

                case UnaryExpr:
                    return false; // Unsupported unary operator

                case BinaryExpr b when IsCompilableBinaryOp(b.Op.Type):
                    stack.Push(b.Left);
                    stack.Push(b.Right);
                    break;

                case BinaryExpr:
                    return false; // Unsupported binary operator

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

                case CompoundAssignExpr ca:
                    stack.Push(ca.Value);
                    break;

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

                default:
                    return false; // Unknown expression type
            }
        }

        return true;
    }

    private static bool IsCompilableBinaryOp(TokenType op) => op is
        TokenType.Plus or TokenType.Minus or TokenType.Star or
        TokenType.Slash or TokenType.Percent or
        TokenType.EqualEqual or TokenType.EqualEqualEqual or
        TokenType.BangEqual or TokenType.BangEqualEqual or
        TokenType.Less or TokenType.LessEqual or
        TokenType.Greater or TokenType.GreaterEqual or
        TokenType.Amp or TokenType.Pipe or TokenType.Caret;

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
                _ => throw new NotSupportedException($"Cannot compile {expr.GetType().Name}")
            };
        }
        finally
        {
            _compileDepth--;
        }
    }
}
