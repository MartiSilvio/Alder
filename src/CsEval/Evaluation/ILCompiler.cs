using System.Linq.Expressions;
using System.Reflection;
using CsEval.Parsing;
using LinqExpression = System.Linq.Expressions.Expression;

namespace CsEval.Evaluation;

/// <summary>
/// Compiles CsEval AST to IL using Expression Trees for maximum performance and correctness.
/// Expression Trees handle all the complexity of IL generation (try/finally, Leave vs Br, etc.)
/// automatically, eliminating entire classes of bugs that plague raw IL emission.
/// </summary>
internal sealed class ILCompiler
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
    private static readonly MethodInfo IsTruthyMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.IsTruthy))!;
    private static readonly MethodInfo AddMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Add))!;
    private static readonly MethodInfo SubtractMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Subtract))!;
    private static readonly MethodInfo MultiplyMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Multiply))!;
    private static readonly MethodInfo DivideMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Divide))!;
    private static readonly MethodInfo ModuloMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Modulo))!;
    private static readonly MethodInfo EqualsMethod = typeof(CompilerHelpers).GetMethod("Equals", [typeof(object), typeof(object), typeof(CsEvalOptions)])!;
    private static readonly MethodInfo NotEqualsMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.NotEquals))!;
    private static readonly MethodInfo LessThanMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.LessThan))!;
    private static readonly MethodInfo LessThanOrEqualMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.LessThanOrEqual))!;
    private static readonly MethodInfo GreaterThanMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GreaterThan))!;
    private static readonly MethodInfo GreaterThanOrEqualMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GreaterThanOrEqual))!;
    private static readonly MethodInfo BitwiseAndMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.BitwiseAnd))!;
    private static readonly MethodInfo BitwiseOrMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.BitwiseOr))!;
    private static readonly MethodInfo BitwiseXorMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.BitwiseXor))!;
    private static readonly MethodInfo GetMemberMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GetMember))!;
    private static readonly MethodInfo GetIndexMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GetIndex))!;
    private static readonly MethodInfo SetIndexMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.SetIndex))!;
    private static readonly MethodInfo NegateMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Negate))!;
    private static readonly MethodInfo ThrowIfCancellationRequestedMethod = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
    private static readonly MethodInfo CheckIterationLimitMethod = typeof(ILCompilerHelpers).GetMethod(nameof(ILCompilerHelpers.CheckIterationLimit))!;
    private static readonly MethodInfo GetEnumeratorMethod = typeof(ILCompilerHelpers).GetMethod(nameof(ILCompilerHelpers.GetEnumerator))!;
    private static readonly MethodInfo MoveNextMethod = typeof(System.Collections.IEnumerator).GetMethod(nameof(System.Collections.IEnumerator.MoveNext))!;
    private static readonly MethodInfo GetCurrentProperty = typeof(System.Collections.IEnumerator).GetProperty(nameof(System.Collections.IEnumerator.Current))!.GetGetMethod()!;
    private static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!;
    private static readonly MethodInfo CheckAllowAssignmentMethod = typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.CheckAllowAssignment))!;

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
        TokenType.EqualEqual or TokenType.BangEqual or
        TokenType.EqualEqualEqual or TokenType.BangEqualEqual or
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

    #region Expression Compilation

    private LinqExpression CompileLiteral(LiteralExpr lit)
    {
        if (lit.Value == null)
            return LinqExpression.Constant(null, typeof(object));

        // Box value types to object
        return LinqExpression.Convert(
            LinqExpression.Constant(lit.Value, lit.Value.GetType()),
            typeof(object));
    }

    private LinqExpression CompileIdentifier(IdentifierExpr id)
    {
        return LinqExpression.Call(
            _currentContext,
            GetMethod,
            LinqExpression.Constant(id.Name.Lexeme));
    }

    private LinqExpression CompileUnary(UnaryExpr u)
    {
        var operand = Compile(u.Right);

        return u.Op.Type switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, operand),
            TokenType.Bang => LinqExpression.Convert(
                LinqExpression.Not(LinqExpression.Call(IsTruthyMethod, operand)),
                typeof(object)),
            _ => throw new NotSupportedException($"Unary operator {u.Op.Type}")
        };
    }

    private LinqExpression CompileBinary(BinaryExpr b)
    {
        var left = Compile(b.Left);
        var right = Compile(b.Right);

        var method = b.Op.Type switch
        {
            TokenType.Plus => AddMethod,
            TokenType.Minus => SubtractMethod,
            TokenType.Star => MultiplyMethod,
            TokenType.Slash => DivideMethod,
            TokenType.Percent => ModuloMethod,
            TokenType.EqualEqual or TokenType.EqualEqualEqual => EqualsMethod,
            TokenType.BangEqual or TokenType.BangEqualEqual => NotEqualsMethod,
            TokenType.Less => LessThanMethod,
            TokenType.LessEqual => LessThanOrEqualMethod,
            TokenType.Greater => GreaterThanMethod,
            TokenType.GreaterEqual => GreaterThanOrEqualMethod,
            TokenType.Amp => BitwiseAndMethod,
            TokenType.Pipe => BitwiseOrMethod,
            TokenType.Caret => BitwiseXorMethod,
            _ => throw new NotSupportedException($"Binary operator {b.Op.Type}")
        };

        return LinqExpression.Call(method, left, right, _optionsParam);
    }

    private LinqExpression CompileLogical(LogicalExpr l)
    {
        var left = Compile(l.Left);
        var right = Compile(l.Right);

        var leftTruthy = LinqExpression.Call(IsTruthyMethod, left);
        var rightTruthy = LinqExpression.Call(IsTruthyMethod, right);

        // Short-circuit evaluation
        LinqExpression result = l.Op.Type switch
        {
            TokenType.PipePipe or TokenType.Or => LinqExpression.OrElse(leftTruthy, rightTruthy),
            TokenType.AmpAmp or TokenType.And => LinqExpression.AndAlso(leftTruthy, rightTruthy),
            _ => throw new NotSupportedException($"Logical operator {l.Op.Type}")
        };

        return LinqExpression.Convert(result, typeof(object));
    }

    private LinqExpression CompileConditional(ConditionalExpr c)
    {
        var condition = LinqExpression.Call(IsTruthyMethod, Compile(c.Condition));
        var thenBranch = Compile(c.ThenBranch);
        var elseBranch = Compile(c.ElseBranch);

        return LinqExpression.Condition(condition, thenBranch, elseBranch);
    }

    private LinqExpression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = Compile(n.Left);
        var right = Compile(n.Right);

        return LinqExpression.Coalesce(left, right);
    }

    private LinqExpression CompileMemberAccess(MemberAccessExpr m)
    {
        var obj = Compile(m.Object);

        return LinqExpression.Call(
            GetMemberMethod,
            obj,
            LinqExpression.Constant(m.Name.Lexeme),
            _optionsParam,
            LinqExpression.Constant(m.NullSafe),
            _currentContext);
    }

    private LinqExpression CompileVariableDecl(VariableDeclExpr v)
    {
        var value = Compile(v.Initializer);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_currentContext, DefineMethod,
                LinqExpression.Constant(v.Name.Lexeme), temp),
            temp);
    }

    private LinqExpression CompileAssign(AssignExpr a)
    {
        var name = a.Name.Lexeme;
        var value = Compile(a.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        return LinqExpression.Block(
            new[] { temp },
            // Check sandbox allows assignment
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} = ...")),
            LinqExpression.Assign(temp, value),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileCompoundAssign(CompoundAssignExpr ca)
    {
        var name = ca.Name.Lexeme;
        var currentValue = CompileIdentifier(new IdentifierExpr(ca.Name));
        var rightValue = Compile(ca.Value);
        var temp = LinqExpression.Variable(typeof(object), "temp");

        var method = ca.Op.Type switch
        {
            TokenType.PlusEqual => AddMethod,
            TokenType.MinusEqual => SubtractMethod,
            TokenType.StarEqual => MultiplyMethod,
            TokenType.SlashEqual => DivideMethod,
            TokenType.PercentEqual => ModuloMethod,
            _ => throw new NotSupportedException($"Compound operator {ca.Op.Type}")
        };

        return LinqExpression.Block(
            new[] { temp },
            LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
                LinqExpression.Constant($"{name} {ca.Op.Lexeme} ...")),
            LinqExpression.Assign(temp, LinqExpression.Call(method, currentValue, rightValue, _optionsParam)),
            LinqExpression.Call(_currentContext, SetMethod,
                LinqExpression.Constant(name), temp),
            temp);
    }

    private LinqExpression CompileIncrementDecrement(IncrementDecrementExpr inc)
    {
        var name = inc.Name.Lexeme;
        var isIncrement = inc.Op.Type == TokenType.PlusPlus;
        var currentValue = CompileIdentifier(new IdentifierExpr(inc.Name));
        var one = LinqExpression.Convert(LinqExpression.Constant(1), typeof(object));
        var temp = LinqExpression.Variable(typeof(object), "temp");
        var original = LinqExpression.Variable(typeof(object), "original");

        var method = isIncrement ? AddMethod : SubtractMethod;

        // Check sandbox
        var checkExpr = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam,
            LinqExpression.Constant(isIncrement ? $"{name}++" : $"{name}--"));

        if (inc.IsPrefix)
        {
            // Prefix: return new value
            return LinqExpression.Block(
                new[] { temp },
                checkExpr,
                LinqExpression.Assign(temp, LinqExpression.Call(method, currentValue, one, _optionsParam)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                temp);
        }
        else
        {
            // Postfix: return original value
            return LinqExpression.Block(
                new[] { temp, original },
                checkExpr,
                LinqExpression.Assign(original, currentValue),
                LinqExpression.Assign(temp, LinqExpression.Call(method, original, one, _optionsParam)),
                LinqExpression.Call(_currentContext, SetMethod,
                    LinqExpression.Constant(name), temp),
                original);
        }
    }

    #endregion

    #region Control Flow

    private LinqExpression CompileBlock(BlockExpr block)
    {
        var statements = new List<LinqExpression>();

        foreach (var stmt in block.Statements)
        {
            statements.Add(CompileCancellationCheck());
            statements.Add(Compile(stmt));
        }

        if (block.ReturnExpr != null)
            statements.Add(Compile(block.ReturnExpr));
        else
            statements.Add(LinqExpression.Constant(null, typeof(object)));

        return LinqExpression.Block(statements);
    }



    private LinqExpression CompileIndexAccess(IndexAccessExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        return LinqExpression.Call(GetIndexMethod, target, index, _optionsParam);
    }



    private LinqExpression CompileIndexAssign(IndexAssignExpr expr)
    {
        var target = Compile(expr.Object);
        var index = Compile(expr.Index);
        var value = Compile(expr.Value);
        
        var checkStr = LinqExpression.Constant("Index assignment");
        var check = LinqExpression.Call(CheckAllowAssignmentMethod, _optionsParam, checkStr);

        var set = LinqExpression.Call(SetIndexMethod, target, index, value);
        
        return LinqExpression.Block(check, set, value);
    }

    private LinqExpression CompileIf(IfStatementExpr ifStmt)
    {
        var condition = LinqExpression.Call(IsTruthyMethod, Compile(ifStmt.Condition));

        // Then branch with scope
        var thenBlock = Scoped(() =>
        {
            var thenStatements = new List<LinqExpression>();
            foreach (var stmt in ifStmt.ThenStatements)
            {
                thenStatements.Add(CompileCancellationCheck());
                thenStatements.Add(Compile(stmt));
            }
            thenStatements.Add(LinqExpression.Constant(null, typeof(object)));
            return LinqExpression.Block(thenStatements);
        });

        // Else branch with scope (if present)
        LinqExpression elseBlock;
        if (ifStmt.ElseStatements != null)
        {
            elseBlock = Scoped(() =>
            {
                var elseStatements = new List<LinqExpression>();
                foreach (var stmt in ifStmt.ElseStatements)
                {
                    elseStatements.Add(CompileCancellationCheck());
                    elseStatements.Add(Compile(stmt));
                }
                elseStatements.Add(LinqExpression.Constant(null, typeof(object)));
                return LinqExpression.Block(elseStatements);
            });
        }
        else
        {
            elseBlock = LinqExpression.Constant(null, typeof(object));
        }

        return LinqExpression.Condition(condition, thenBlock, elseBlock);
    }

    private LinqExpression CompileWhile(WhileStatementExpr whileStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        _controlStack.Push(new ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

        var loopStatements = new List<LinqExpression>();

        // Cancellation and iteration check
        loopStatements.Add(CompileCancellationCheck());
        loopStatements.Add(CompileIterationCheck());

        // Condition check - break if false
        loopStatements.Add(LinqExpression.IfThen(
            LinqExpression.Not(LinqExpression.Call(IsTruthyMethod, Compile(whileStmt.Condition))),
            LinqExpression.Break(breakLabel)));

        // Body with scope
        loopStatements.Add(Scoped(() =>
        {
            var bodyStatements = new List<LinqExpression>();
            foreach (var stmt in whileStmt.Body)
            {
                bodyStatements.Add(CompileCancellationCheck());
                bodyStatements.Add(Compile(stmt));
            }
            return LinqExpression.Block(bodyStatements);
        }));

        // Continue label (after body, before loop back)
        loopStatements.Add(LinqExpression.Label(continueLabel));

        var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

        _controlStack.Pop();

        return LinqExpression.Block(loop, LinqExpression.Constant(null, typeof(object)));
    }

    private LinqExpression CompileFor(ForStatementExpr forStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        // For loop has its own outer scope for the initializer
        return Scoped(() =>
        {
            var outerStatements = new List<LinqExpression>();

            // Initializer
            if (forStmt.Initializer != null)
                outerStatements.Add(Compile(forStmt.Initializer));

            _controlStack.Push(new ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

            var loopStatements = new List<LinqExpression>();

            // Cancellation and iteration check
            loopStatements.Add(CompileCancellationCheck());
            loopStatements.Add(CompileIterationCheck());

            // Condition check (if present)
            if (forStmt.Condition != null)
            {
                loopStatements.Add(LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(IsTruthyMethod, Compile(forStmt.Condition))),
                    LinqExpression.Break(breakLabel)));
            }

            // Body with nested scope
            loopStatements.Add(Scoped(() =>
            {
                var bodyStatements = new List<LinqExpression>();
                foreach (var stmt in forStmt.Body)
                {
                    bodyStatements.Add(CompileCancellationCheck());
                    bodyStatements.Add(Compile(stmt));
                }
                return LinqExpression.Block(bodyStatements);
            }));

            // Continue label
            loopStatements.Add(LinqExpression.Label(continueLabel));

            // Increment
            if (forStmt.Increment != null)
                loopStatements.Add(Compile(forStmt.Increment));

            var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);
            outerStatements.Add(loop);

            _controlStack.Pop();

            outerStatements.Add(LinqExpression.Constant(null, typeof(object)));
            return LinqExpression.Block(outerStatements);
        });
    }

    private LinqExpression CompileDoWhile(DoWhileStatementExpr doWhile)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        _controlStack.Push(new ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

        var loopStatements = new List<LinqExpression>();

        // Cancellation and iteration check
        loopStatements.Add(CompileCancellationCheck());
        loopStatements.Add(CompileIterationCheck());

        // Body with scope (executes first in do-while)
        loopStatements.Add(Scoped(() =>
        {
            var bodyStatements = new List<LinqExpression>();
            foreach (var stmt in doWhile.Body)
            {
                bodyStatements.Add(CompileCancellationCheck());
                bodyStatements.Add(Compile(stmt));
            }
            return LinqExpression.Block(bodyStatements);
        }));

        // Continue label
        loopStatements.Add(LinqExpression.Label(continueLabel));

        // Condition check - break if false
        loopStatements.Add(LinqExpression.IfThen(
            LinqExpression.Not(LinqExpression.Call(IsTruthyMethod, Compile(doWhile.Condition))),
            LinqExpression.Break(breakLabel)));

        var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

        _controlStack.Pop();

        return LinqExpression.Block(loop, LinqExpression.Constant(null, typeof(object)));
    }

    private LinqExpression CompileForEach(ForEachStatementExpr forEach)
    {
        var loopId = _controlStack.Count; // Unique ID for nested foreach
        var breakLabel = LinqExpression.Label(typeof(void), $"break{loopId}");
        var continueLabel = LinqExpression.Label(typeof(void), $"continue{loopId}");

        var enumerator = LinqExpression.Variable(typeof(System.Collections.IEnumerator), $"enumerator{loopId}");
        var itemValue = LinqExpression.Variable(typeof(object), $"item{loopId}");

        // Get enumerator
        var getEnumerator = LinqExpression.Assign(
            enumerator,
            LinqExpression.Call(GetEnumeratorMethod, Compile(forEach.Collection)));

        // Enter foreach scope
        return Scoped(() =>
        {
            _controlStack.Push(new ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

            // Loop body
            var loopStatements = new List<LinqExpression>();

            // Cancellation and iteration check
            loopStatements.Add(CompileCancellationCheck());
            loopStatements.Add(CompileIterationCheck());

            // MoveNext - break if false
            loopStatements.Add(LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(enumerator, MoveNextMethod)),
                LinqExpression.Break(breakLabel)));

            // Get Current and define variable
            loopStatements.Add(LinqExpression.Assign(
                itemValue,
                LinqExpression.Property(enumerator, nameof(System.Collections.IEnumerator.Current))));

            loopStatements.Add(LinqExpression.Call(_currentContext, DefineMethod,
                LinqExpression.Constant(forEach.VariableName.Lexeme), itemValue));

            // Body with nested scope
            loopStatements.Add(Scoped(() =>
            {
                var bodyStatements = new List<LinqExpression>();
                foreach (var stmt in forEach.Body)
                {
                    bodyStatements.Add(CompileCancellationCheck());
                    bodyStatements.Add(Compile(stmt));
                }
                return LinqExpression.Block(bodyStatements);
            }));

            // Continue label
            loopStatements.Add(LinqExpression.Label(continueLabel));

            var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

            _controlStack.Pop();

            // Try-finally for disposal - Expression Trees handle this correctly!
            var disposeExpr = LinqExpression.IfThen(
                LinqExpression.TypeIs(enumerator, typeof(IDisposable)),
                LinqExpression.Call(
                    LinqExpression.Convert(enumerator, typeof(IDisposable)),
                    DisposeMethod));

            var tryFinally = LinqExpression.TryFinally(
                loop,
                disposeExpr);

            return LinqExpression.Block(
                new[] { enumerator, itemValue },
                getEnumerator,
                tryFinally,
                LinqExpression.Constant(null, typeof(object)));
        });
    }

    private LinqExpression CompileSwitch(SwitchStatementExpr switchStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "switch_break");
        
        // Switch pushes to control stack (for break) but acts as non-loop
        _controlStack.Push(new ControlFlowContext(breakLabel, null, IsLoop: false));

        // Evaluate switch value once
        var switchValue = Compile(switchStmt.Expression);
        var switchVar = LinqExpression.Variable(typeof(object), "switchValue");

        // Scoped for switch body
        return Scoped(() =>
        {
            var statements = new List<LinqExpression>();
            // Assign switch value
            statements.Add(LinqExpression.Assign(switchVar, switchValue));

            // Labels for each case
            var caseLabels = new List<(SwitchCaseExpr Case, LabelTarget Label)>();
            LabelTarget? defaultLabel = null;

            foreach (var c in switchStmt.Cases)
            {
                if (c.Pattern != null)
                    caseLabels.Add((c, LinqExpression.Label("case")));
                else
                    defaultLabel = LinqExpression.Label("default");
            }

            // Create dispatch logic (If-Else chain)
            // if (Eq(val, case1)) goto label1; ...
            foreach (var mapping in caseLabels)
            {
                var patternVal = Compile(mapping.Case.Pattern!);
                var condition = LinqExpression.Call(EqualsMethod, switchVar, patternVal, _optionsParam);
                statements.Add(LinqExpression.IfThen(
                    LinqExpression.Convert(condition, typeof(bool)),
                    LinqExpression.Goto(mapping.Label)));
            }

            // Goto default or break if no match
            if (defaultLabel != null)
                statements.Add(LinqExpression.Goto(defaultLabel));
            else
                statements.Add(LinqExpression.Goto(breakLabel));

            // Generate case bodies
            foreach (var c in switchStmt.Cases)
            {
                // Find label for this case
                LabelTarget? targetLabel = null;
                if (c.Pattern == null)
                    targetLabel = defaultLabel;
                else
                    targetLabel = caseLabels.First(x => x.Case == c).Label;
                
                if (targetLabel != null)
                {
                    statements.Add(LinqExpression.Label(targetLabel));
                    foreach (var stmt in c.Statements)
                    {
                        statements.Add(CompileCancellationCheck());
                        statements.Add(Compile(stmt));
                    }
                    // Fallthrough happpens automatically to next label
                }
            }

            statements.Add(LinqExpression.Label(breakLabel));
            
            _controlStack.Pop();

            return LinqExpression.Block(new[] { switchVar }, statements);
        });
    }

    private LinqExpression CompileBreak()
    {
        if (_controlStack.Count == 0)
            throw new EvalException("break statement outside of loop or switch");

        var context = _controlStack.Peek();
        return LinqExpression.Break(context.BreakTarget);
    }

    private LinqExpression CompileContinue()
    {
        // Search stack for nearest loop
        foreach (var context in _controlStack)
        {
            if (context.IsLoop && context.ContinueTarget != null)
                return LinqExpression.Continue(context.ContinueTarget);
        }

        throw new EvalException("continue statement outside of loop");
    }

    private LinqExpression CompileReturn(ReturnExpr ret)
    {
        var value = ret.Value != null
            ? Compile(ret.Value)
            : LinqExpression.Constant(null, typeof(object));

        // Use Goto to jump to return label - Expression Trees handle try/finally correctly
        return LinqExpression.Return(_returnLabel, value);
    }

    #endregion

    #region Helpers

    private LinqExpression CompileCancellationCheck()
    {
        return LinqExpression.Call(_ctParam, ThrowIfCancellationRequestedMethod);
    }

    private LinqExpression CompileIterationCheck()
    {
        // _iterationCount++; CheckIterationLimit(_iterationCount, options);
        return LinqExpression.Block(
            LinqExpression.PostIncrementAssign(_iterationCount),
            LinqExpression.Call(CheckIterationLimitMethod, _iterationCount, _optionsParam));
    }

    /// <summary>
    /// Wraps a block of code in a scope (TryFinally for cleanup).
    /// </summary>
    private LinqExpression Scoped(Func<LinqExpression> bodyFactory)
    {
        // 1. Enter scope (assigns new child context)
        var enterExpr = EnterScopeExpr(out var parentVar);

        // 2. Compile body (uses current context)
        var body = bodyFactory();

        // 3. Exit scope (restores parent context)
        // cleanup is guaranteed by TryFinally
        var exitExpr = ExitScopeExpr(parentVar);

        return LinqExpression.Block(
            new[] { parentVar },
            enterExpr,
            LinqExpression.TryFinally(
                body,
                exitExpr));
    }

    /// <summary>
    /// Create expressions to enter a new scope (child context).
    /// Returns the expression that performs the scope entry.
    /// The parentVar output parameter receives the variable that stores the parent context.
    /// </summary>
    private LinqExpression EnterScopeExpr(out ParameterExpression parentVar)
    {
        parentVar = LinqExpression.Variable(typeof(EvalContext), $"parent{_contextStack.Count}");
        _contextStack.Push(parentVar);

        var currentContextVar = _currentContext as ParameterExpression;
        if (currentContextVar == null)
        {
            // First scope - current context is the parameter
            currentContextVar = _contextParam;
        }

        // Save parent and create child
        var saveParent = LinqExpression.Assign(parentVar, _currentContext);
        var createChild = LinqExpression.Assign(
            _currentContext,
            LinqExpression.Call(_currentContext, CreateChildMethod));

        return LinqExpression.Block(saveParent, createChild);
    }

    /// <summary>
    /// Create expression to exit current scope (restore parent context).
    /// </summary>
    private LinqExpression ExitScopeExpr(ParameterExpression parentVar)
    {
        _contextStack.Pop();
        return LinqExpression.Assign(_currentContext, parentVar);
    }

    #endregion
}

/// <summary>
/// Helper methods called by IL-compiled code.
/// </summary>
public static class ILCompilerHelpers
{
    public static void CheckIterationLimit(long iterations, CsEvalOptions options)
    {
        if (options.MaxIterations > 0 && iterations > options.MaxIterations)
            throw new EvalException($"Loop exceeded maximum iterations ({options.MaxIterations}). Possible infinite loop.");
    }

    public static System.Collections.IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not System.Collections.IEnumerable enumerable)
            throw new EvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");

        return enumerable.GetEnumerator();
    }
}
