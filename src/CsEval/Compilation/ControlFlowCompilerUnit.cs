using System.Linq.Expressions;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Compiles control flow nodes (if, while, for, foreach, switch, try/catch/finally, etc.)
/// to Expression Trees. Receives shared state via CompilerContext.
/// </summary>
internal sealed class ControlFlowCompilerUnit
{
    private readonly CompilerContext _ctx;
    private readonly CompilerHelpers _helpers;

    // Lazily set reference for cross-unit dispatch
    private ExpressionCompilerUnit? _exprUnit;
    private PatternCompilerUnit? _patternUnit;

    internal ControlFlowCompilerUnit(CompilerContext ctx, CompilerHelpers helpers)
    {
        _ctx = ctx;
        _helpers = helpers;
    }

    internal void SetExpressionUnit(ExpressionCompilerUnit exprUnit)
    {
        _exprUnit = exprUnit;
    }

    internal void SetPatternUnit(PatternCompilerUnit patternUnit)
    {
        _patternUnit = patternUnit;
    }

    /// <summary>
    /// Compile a sub-expression by dispatching through the central Compile method.
    /// </summary>
    private LinqExpression Compile(Expr expr) => _exprUnit!.Compile(expr);

    internal LinqExpression CompileBlock(BlockExpr block)
    {
        // Create a child context for proper block scoping
        return _helpers.Scoped(() =>
        {
            var statements = new List<LinqExpression>();

            foreach (var stmt in block.Statements)
            {
                statements.Add(CompileConstraintCheck());
                statements.Add(Compile(stmt));
            }

            if (block.ReturnExpr != null)
                statements.Add(Compile(block.ReturnExpr));
            else
                statements.Add(LinqExpression.Constant(null, typeof(object)));

            return LinqExpression.Block(statements);
        });
    }

    internal LinqExpression CompileIf(IfStatementExpr ifStmt)
    {
        var condition = LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(ifStmt.Condition));

        // Then branch with scope
        var thenBlock = _helpers.Scoped(() =>
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
            elseBlock = _helpers.Scoped(() =>
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

    internal LinqExpression CompileWhile(WhileStatementExpr whileStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        _ctx.ControlStack.Push(new CompilerContext.ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

        var loopStatements = new List<LinqExpression>
        {
            CompileCancellationCheck(),
            // Condition check - break if false
            LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(whileStmt.Condition))),
                LinqExpression.Break(breakLabel)),
            CompileConstraintCheck(),
            // Body with scope
            _helpers.Scoped(() =>
            {
                var bodyStatements = new List<LinqExpression>();
                foreach (var stmt in whileStmt.Body)
                {
                    bodyStatements.Add(CompileCancellationCheck());
                    bodyStatements.Add(Compile(stmt));
                }
                return LinqExpression.Block(bodyStatements);
            }),
            // Continue label (after body, before loop back)
            LinqExpression.Label(continueLabel)
        };

        var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

        _ctx.ControlStack.Pop();

        return LinqExpression.Block(loop, LinqExpression.Constant(null, typeof(object)));
    }

    internal LinqExpression CompileFor(ForStatementExpr forStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        // For loop has its own outer scope for the initializer
        return _helpers.Scoped(() =>
        {
            var outerStatements = new List<LinqExpression>();

            // Initializers
            foreach (var init in forStmt.Initializers)
                outerStatements.Add(Compile(init));

            _ctx.ControlStack.Push(new CompilerContext.ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

            var loopStatements = new List<LinqExpression> { CompileCancellationCheck() };

            // Condition check (if present)
            if (forStmt.Condition != null)
            {
                loopStatements.Add(LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(forStmt.Condition))),
                    LinqExpression.Break(breakLabel)));
            }

            loopStatements.Add(CompileConstraintCheck());

            // Body with nested scope
            loopStatements.Add(_helpers.Scoped(() =>
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

            // Increments
            foreach (var inc in forStmt.Increments)
                loopStatements.Add(Compile(inc));

            var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);
            outerStatements.Add(loop);

            _ctx.ControlStack.Pop();

            outerStatements.Add(LinqExpression.Constant(null, typeof(object)));
            return LinqExpression.Block(outerStatements);
        });
    }

    internal LinqExpression CompileDoWhile(DoWhileStatementExpr doWhile)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "break");
        var continueLabel = LinqExpression.Label(typeof(void), "continue");

        _ctx.ControlStack.Push(new CompilerContext.ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

        var loopStatements = new List<LinqExpression>
        {
            // Cancellation and iteration check
            CompileCancellationCheck(),
            CompileConstraintCheck(),
            // Body with scope (executes first in do-while)
            _helpers.Scoped(() =>
            {
                var bodyStatements = new List<LinqExpression>();
                foreach (var stmt in doWhile.Body)
                {
                    bodyStatements.Add(CompileCancellationCheck());
                    bodyStatements.Add(Compile(stmt));
                }
                return LinqExpression.Block(bodyStatements);
            }),
            // Continue label
            LinqExpression.Label(continueLabel),
            // Condition check - break if false
            LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(doWhile.Condition))),
                LinqExpression.Break(breakLabel))
        };

        var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

        _ctx.ControlStack.Pop();

        return LinqExpression.Block(loop, LinqExpression.Constant(null, typeof(object)));
    }

    internal LinqExpression CompileForEach(ForEachStatementExpr forEach)
    {
        var loopId = _ctx.ControlStack.Count; // Unique ID for nested foreach
        var breakLabel = LinqExpression.Label(typeof(void), $"break{loopId}");
        var continueLabel = LinqExpression.Label(typeof(void), $"continue{loopId}");

        var enumerator = LinqExpression.Variable(typeof(IEnumerator), $"enumerator{loopId}");
        var itemValue = LinqExpression.Variable(typeof(object), $"item{loopId}");

        // Get enumerator
        var getEnumerator = LinqExpression.Assign(
            enumerator,
            LinqExpression.Call(CompilerContext.GetEnumeratorMethod, Compile(forEach.Collection)));

        // Enter foreach scope
        return _helpers.Scoped(() =>
        {
            _ctx.ControlStack.Push(new CompilerContext.ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

            var loopStatements = new List<LinqExpression>
            {
                CompileCancellationCheck(),
                // MoveNext - break if false
                LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(enumerator, CompilerContext.MoveNextMethod)),
                    LinqExpression.Break(breakLabel)),
                CompileConstraintCheck(),
                // Get Current value
                LinqExpression.Assign(
                    itemValue,
                    LinqExpression.Property(enumerator, nameof(IEnumerator.Current))),
                // C# 5+ behavior: create a fresh scope for EACH iteration
                // This ensures lambdas capture a per-iteration variable, not a shared one
                _helpers.Scoped(() =>
                {
                    var iterStatements = new List<LinqExpression>
                    {
                        // Define loop variable in this per-iteration scope (with shadowing check)
                        LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineNewMethod,
                            LinqExpression.Constant(forEach.VariableName.Lexeme), itemValue,
                            LinqExpression.Constant(typeof(object), typeof(Type)))
                    };

                    // Body statements in the same per-iteration scope
                    foreach (var stmt in forEach.Body)
                    {
                        iterStatements.Add(CompileCancellationCheck());
                        iterStatements.Add(Compile(stmt));
                    }

                    return LinqExpression.Block(iterStatements);
                }),
                // Continue label
                LinqExpression.Label(continueLabel)
            };

            var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

            _ctx.ControlStack.Pop();

            // Try-finally for disposal - Expression Trees handle this correctly!
            var disposeExpr = LinqExpression.IfThen(
                LinqExpression.TypeIs(enumerator, typeof(IDisposable)),
                LinqExpression.Call(
                    LinqExpression.Convert(enumerator, typeof(IDisposable)),
                    CompilerContext.DisposeMethod));

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

    internal LinqExpression CompileSwitch(SwitchStatementExpr switchStmt)
    {
        var breakLabel = LinqExpression.Label(typeof(void), "switch_break");

        // Switch pushes to control stack (for break) but acts as non-loop
        _ctx.ControlStack.Push(new CompilerContext.ControlFlowContext(breakLabel, null, IsLoop: false));

        // Evaluate switch value once
        var switchValue = Compile(switchStmt.Expression);
        var switchVar = LinqExpression.Variable(typeof(object), "switchValue");

        // Scoped for switch body
        return _helpers.Scoped(() =>
        {
            var statements = new List<LinqExpression> {
                // Assign switch value
                LinqExpression.Assign(switchVar, switchValue) };

            // Labels for each case
            var caseLabels = new List<(SwitchCaseExpr Case, LabelTarget Label)>();
            LabelTarget? defaultLabel = null;

            foreach (var c in switchStmt.Cases)
            {
                if (c.CasePattern != null)
                    caseLabels.Add((c, LinqExpression.Label("case")));
                else
                    defaultLabel = LinqExpression.Label("default");
            }

            // Create dispatch logic using pattern matching (If-Else chain)
            // Each case gets its own scope for pattern variable bindings
            var parentContextVar = LinqExpression.Variable(typeof(CsEvalContext), "parentCtx");

            foreach (var mapping in caseLabels)
            {
                // Save context, create child for pattern bindings
                var saveContext = LinqExpression.Assign(parentContextVar, _ctx.CurrentContext);
                var createChild = LinqExpression.Assign(_ctx.CurrentContext,
                    LinqExpression.Call(_ctx.CurrentContext, CompilerContext.CreateChildMethod));

                // Compile pattern match
                var patternMatch = _patternUnit!.CompilePatternMatch(switchVar, mapping.Case.CasePattern!);
                var matchBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, patternMatch);

                // Build condition with optional when guard
                LinqExpression condition = matchBool;
                if (mapping.Case.WhenGuard != null)
                {
                    var guardResult = Compile(mapping.Case.WhenGuard);
                    var guardBool = LinqExpression.Call(CompilerContext.RequireBooleanMethod, guardResult);
                    condition = LinqExpression.AndAlso(matchBool, guardBool);
                }

                // If pattern matches: goto case label (keep child context for bindings)
                // If no match: restore parent context
                statements.Add(saveContext);
                statements.Add(createChild);
                statements.Add(LinqExpression.IfThenElse(
                    condition,
                    LinqExpression.Goto(mapping.Label),
                    LinqExpression.Assign(_ctx.CurrentContext, parentContextVar)));
            }

            // Goto default or break if no match
            if (defaultLabel != null)
                statements.Add(LinqExpression.Goto(defaultLabel));
            else
                statements.Add(LinqExpression.Goto(breakLabel));

            // Generate case bodies
            // C# semantics: empty cases fall through; non-empty cases MUST have explicit break/return
            foreach (var c in switchStmt.Cases)
            {
                // Find label for this case
                LabelTarget? targetLabel = null;
                if (c.CasePattern == null)
                    targetLabel = defaultLabel;
                else
                    targetLabel = caseLabels.First(x => x.Case == c).Label;

                if (targetLabel != null)
                {
                    statements.Add(LinqExpression.Label(targetLabel));

                    // Empty case: fall through to next label
                    if (c.Statements.Count == 0)
                        continue;

                    // Non-empty case: validate it ends with break/return/continue (C# CS0163)
                    if (!TerminatesControlFlow(c.Statements.Last()))
                        throw new CsEvalException(DiagnosticDescriptors.CaseFallThrough);

                    foreach (var stmt in c.Statements)
                    {
                        statements.Add(CompileCancellationCheck());
                        statements.Add(Compile(stmt));
                    }
                }
            }

            statements.Add(LinqExpression.Label(breakLabel));

            _ctx.ControlStack.Pop();

            return LinqExpression.Block(new[] { switchVar, parentContextVar }, statements);
        });
    }

    /// <summary>
    /// Checks whether an expression unconditionally terminates control flow,
    /// unwrapping block expressions to inspect their last statement.
    /// </summary>
    private static bool TerminatesControlFlow(Expr expr) => expr switch
    {
        BreakExpr => true,
        ReturnExpr => true,
        ContinueExpr => true,
        ThrowExpr => true,
        BlockExpr b when b.Statements.Count > 0 => TerminatesControlFlow(b.Statements.Last()),
        _ => false
    };

    internal LinqExpression CompileBreak()
    {
        if (_ctx.ControlStack.Count == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        var context = _ctx.ControlStack.Peek();
        return LinqExpression.Break(context.BreakTarget);
    }

    internal LinqExpression CompileContinue()
    {
        // Search stack for nearest loop
        foreach (var context in _ctx.ControlStack)
        {
            if (context is { IsLoop: true, ContinueTarget: not null })
                return LinqExpression.Continue(context.ContinueTarget);
        }

        throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);
    }

    internal LinqExpression CompileReturn(ReturnExpr ret)
    {
        var value = ret.Value != null
            ? Compile(ret.Value)
            : LinqExpression.Constant(null, typeof(object));

        // Use Goto to jump to return label - Expression Trees handle try/finally correctly
        // Use CurrentReturnLabel to support nested scopes (lambdas)
        return LinqExpression.Return(_ctx.CurrentReturnLabel, value);
    }

    /// <summary>
    /// Compiles try/catch/finally using the Expression Trees API.
    /// ECMA-334 §13.11 -- The try statement.
    /// Expression Trees use labels for control flow (return/break/continue),
    /// not .NET exceptions, so no special handling is needed for control flow signals.
    /// </summary>
    internal LinqExpression CompileUsing(UsingStatementExpr expr)
    {
        // Compile resource declaration
        var resource = Compile(expr.ResourceDeclaration);
        var resourceVar = LinqExpression.Variable(typeof(object), "usingResource");

        // Compile body
        var body = Compile(expr.Body);

        // Build try/finally with Dispose call
        var disposeMethod = typeof(IDisposable).GetMethod("Dispose")!;
        var disposeCall = LinqExpression.IfThen(
            LinqExpression.TypeIs(resourceVar, typeof(IDisposable)),
            LinqExpression.Call(
                LinqExpression.Convert(resourceVar, typeof(IDisposable)),
                disposeMethod));

        var tryFinally = LinqExpression.TryFinally(body, disposeCall);

        return LinqExpression.Block(
            new[] { resourceVar },
            LinqExpression.Assign(resourceVar, resource),
            tryFinally);
    }

    internal LinqExpression CompileLock(LockStatementExpr expr)
    {
        // Compile lock object
        var lockObj = Compile(expr.LockObject);
        var lockVar = LinqExpression.Variable(typeof(object), "lockObj");
        var lockTaken = LinqExpression.Variable(typeof(bool), "lockTaken");

        // Compile body
        var body = Compile(expr.Body);

        // Build Monitor.Enter/Exit pattern
        var enterMethod = typeof(System.Threading.Monitor).GetMethod("Enter", new[] { typeof(object), typeof(bool).MakeByRefType() })!;
        var exitMethod = typeof(System.Threading.Monitor).GetMethod("Exit", new[] { typeof(object) })!;

        var monitorEnter = LinqExpression.Call(
            enterMethod,
            lockVar,
            lockTaken);

        var monitorExit = LinqExpression.IfThen(
            lockTaken,
            LinqExpression.Call(exitMethod, lockVar));

        var tryFinally = LinqExpression.TryFinally(body, monitorExit);

        return LinqExpression.Block(
            new[] { lockVar, lockTaken },
            LinqExpression.Assign(lockVar, lockObj),
            LinqExpression.Assign(lockTaken, LinqExpression.Constant(false)),
            monitorEnter,
            tryFinally);
    }

    internal LinqExpression CompileTryCatchFinally(TryCatchFinallyExpr expr)
    {
        // Compile try body into a block returning typeof(object)
        var tryStatements = new List<LinqExpression>();
        foreach (var stmt in expr.TryBody)
        {
            tryStatements.Add(CompileCancellationCheck());
            tryStatements.Add(Compile(stmt));
        }
        tryStatements.Add(LinqExpression.Default(typeof(object)));
        var tryBody = LinqExpression.Block(typeof(object), tryStatements);

        // Compile catch blocks
        var catchBlocks = new List<CatchBlock>();
        foreach (var catchClause in expr.CatchClauses)
        {
            // Resolve exception type via context's TypeResolver at compile time
            var catchType = catchClause.ExceptionTypeName != null
                ? _ctx.Context.TypeResolver.ResolveType(catchClause.ExceptionTypeName)
                : typeof(Exception);

            // Create ParameterExpression for the caught exception (typed to the catch type)
            ParameterExpression? exParam = catchClause.VariableName != null || catchClause.WhenGuard != null
                ? LinqExpression.Parameter(catchType, catchClause.VariableName?.Lexeme ?? "ex")
                : null;

            // Compile catch body with scoped variable binding.
            // Use Define (not DefineNew) because the when-guard filter may have already
            // registered the variable in the parent context for guard evaluation.
            LinqExpression catchBody;
            if (catchClause.VariableName != null)
            {
                catchBody = _helpers.Scoped(() =>
                {
                    var bodyStatements = new List<LinqExpression>
                    {
                        // Bind the catch variable in the child context
                        LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineWithTypeMethod,
                            LinqExpression.Constant(catchClause.VariableName.Value.Lexeme),
                            LinqExpression.Convert(exParam!, typeof(object)),
                            LinqExpression.Constant(catchType, typeof(Type)))
                    };

                    foreach (var stmt in catchClause.Body)
                    {
                        bodyStatements.Add(CompileCancellationCheck());
                        bodyStatements.Add(Compile(stmt));
                    }
                    bodyStatements.Add(LinqExpression.Default(typeof(object)));
                    return LinqExpression.Block(typeof(object), bodyStatements);
                });
            }
            else
            {
                var bodyStatements = new List<LinqExpression>();
                foreach (var stmt in catchClause.Body)
                {
                    bodyStatements.Add(CompileCancellationCheck());
                    bodyStatements.Add(Compile(stmt));
                }
                bodyStatements.Add(LinqExpression.Default(typeof(object)));
                catchBody = LinqExpression.Block(typeof(object), bodyStatements);
            }

            // Compile when guard filter (if present)
            LinqExpression? filterExpr = null;
            if (catchClause.WhenGuard != null)
            {
                // The when guard may reference the catch variable, which is available via exParam.
                // We need to bind the variable in context before evaluating the guard.
                if (catchClause.VariableName != null)
                {
                    // In a filter, we need the variable bound for the guard to access it.
                    // Use a block that temporarily defines the variable and evaluates the guard.
                    filterExpr = LinqExpression.Block(
                        LinqExpression.Call(_ctx.CurrentContext, CompilerContext.DefineMethod,
                            LinqExpression.Constant(catchClause.VariableName.Value.Lexeme),
                            LinqExpression.Convert(exParam!, typeof(object))),
                        LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(catchClause.WhenGuard)));
                }
                else
                {
                    filterExpr = LinqExpression.Call(CompilerContext.RequireBooleanMethod, Compile(catchClause.WhenGuard));
                }
            }

            catchBlocks.Add(LinqExpression.MakeCatchBlock(catchType, exParam, catchBody, filterExpr));
        }

        // Compile finally body (if present)
        LinqExpression? finallyBody = null;
        if (expr.FinallyBody != null)
        {
            var finallyStatements = new List<LinqExpression>();
            foreach (var stmt in expr.FinallyBody)
            {
                finallyStatements.Add(CompileCancellationCheck());
                finallyStatements.Add(Compile(stmt));
            }
            finallyBody = LinqExpression.Block(finallyStatements);
        }

        // Assemble the appropriate try expression
        LinqExpression tryExpr;
        if (catchBlocks.Count > 0 && finallyBody != null)
            tryExpr = LinqExpression.TryCatchFinally(tryBody, finallyBody, catchBlocks.ToArray());
        else if (catchBlocks.Count > 0)
            tryExpr = LinqExpression.TryCatch(tryBody, catchBlocks.ToArray());
        else
            tryExpr = LinqExpression.TryFinally(tryBody, finallyBody!);

        return tryExpr;
    }

    private LinqExpression CompileCancellationCheck()
    {
        return LinqExpression.Call(_ctx.CtParam, CompilerContext.ThrowIfCancellationRequestedMethod);
    }

    private LinqExpression CompileConstraintCheck()
    {
        // CheckExecutionConstraints(context.ConstraintState, options.Constraints, ct);
        return LinqExpression.Call(
            CompilerContext.CheckExecutionConstraintsMethod,
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.GetConstraintStateProperty),
            LinqExpression.Call(_ctx.OptionsParam, CompilerContext.GetConstraintsProperty),
            _ctx.CtParam);
    }
}
