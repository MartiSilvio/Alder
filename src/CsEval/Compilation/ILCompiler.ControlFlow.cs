using System.Linq.Expressions;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal sealed partial class ILCompiler
{
    #region Control Flow

    private LinqExpression CompileBlock(BlockExpr block)
    {
        // Create a child context for proper block scoping
        return Scoped(() =>
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
        });
    }

    private LinqExpression CompileIf(IfStatementExpr ifStmt)
    {
        var condition = LinqExpression.Call(RequireBooleanMethod, Compile(ifStmt.Condition));

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

        var loopStatements = new List<LinqExpression>
        {
            CompileCancellationCheck(),
            // Condition check - break if false
            LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, Compile(whileStmt.Condition))),
                LinqExpression.Break(breakLabel)),
            CompileIterationCheck(),
            // Body with scope
            Scoped(() =>
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

            var loopStatements = new List<LinqExpression> { CompileCancellationCheck() };

            // Condition check (if present)
            if (forStmt.Condition != null)
            {
                loopStatements.Add(LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, Compile(forStmt.Condition))),
                    LinqExpression.Break(breakLabel)));
            }

            loopStatements.Add(CompileIterationCheck());

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

        var loopStatements = new List<LinqExpression>
        {
            // Cancellation and iteration check
            CompileCancellationCheck(),
            CompileIterationCheck(),
            // Body with scope (executes first in do-while)
            Scoped(() =>
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
                LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, Compile(doWhile.Condition))),
                LinqExpression.Break(breakLabel))
        };

        var loop = LinqExpression.Loop(LinqExpression.Block(loopStatements), breakLabel);

        _controlStack.Pop();

        return LinqExpression.Block(loop, LinqExpression.Constant(null, typeof(object)));
    }

    private LinqExpression CompileForEach(ForEachStatementExpr forEach)
    {
        var loopId = _controlStack.Count; // Unique ID for nested foreach
        var breakLabel = LinqExpression.Label(typeof(void), $"break{loopId}");
        var continueLabel = LinqExpression.Label(typeof(void), $"continue{loopId}");

        var enumerator = LinqExpression.Variable(typeof(IEnumerator), $"enumerator{loopId}");
        var itemValue = LinqExpression.Variable(typeof(object), $"item{loopId}");

        // Get enumerator
        var getEnumerator = LinqExpression.Assign(
            enumerator,
            LinqExpression.Call(GetEnumeratorMethod, Compile(forEach.Collection)));

        // Enter foreach scope
        return Scoped(() =>
        {
            _controlStack.Push(new ControlFlowContext(breakLabel, continueLabel, IsLoop: true));

            var loopStatements = new List<LinqExpression>
            {
                CompileCancellationCheck(),
                // MoveNext - break if false
                LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(enumerator, MoveNextMethod)),
                    LinqExpression.Break(breakLabel)),
                CompileIterationCheck(),
                // Get Current value
                LinqExpression.Assign(
                    itemValue,
                    LinqExpression.Property(enumerator, nameof(IEnumerator.Current))),
                // C# 5+ behavior: create a fresh scope for EACH iteration
                // This ensures lambdas capture a per-iteration variable, not a shared one
                Scoped(() =>
                {
                    var iterStatements = new List<LinqExpression>
                    {
                        // Define loop variable in this per-iteration scope (with shadowing check)
                        LinqExpression.Call(_currentContext, DefineNewMethod,
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
            var statements = new List<LinqExpression> {
                // Assign switch value
                LinqExpression.Assign(switchVar, switchValue) };

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
                var condition = LinqExpression.Call(EqualsMethod, switchVar, patternVal);
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
            // C# semantics: empty cases fall through; non-empty cases MUST have explicit break/return
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

                    // Empty case: fall through to next label
                    if (c.Statements.Count == 0)
                        continue;

                    // Non-empty case: validate it ends with break/return/continue (C# CS0163)
                    var lastStmt = c.Statements.Last();
                    if (lastStmt is not BreakExpr && lastStmt is not ReturnExpr && lastStmt is not ContinueExpr)
                        throw new CsEvalException("CS0163: Control cannot fall through from one case label to another");

                    foreach (var stmt in c.Statements)
                    {
                        statements.Add(CompileCancellationCheck());
                        statements.Add(Compile(stmt));
                    }
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
            throw new CsEvalException("break statement outside of loop or switch");

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

        throw new CsEvalException("continue statement outside of loop");
    }

    private LinqExpression CompileReturn(ReturnExpr ret)
    {
        var value = ret.Value != null
            ? Compile(ret.Value)
            : LinqExpression.Constant(null, typeof(object));

        // Use Goto to jump to return label - Expression Trees handle try/finally correctly
        return LinqExpression.Return(_returnLabel, value);
    }

    /// <summary>
    /// Compiles try/catch/finally using the Expression Trees API.
    /// ECMA-334 section 13.11 -- The try statement.
    /// Expression Trees use labels for control flow (return/break/continue),
    /// not .NET exceptions, so no special handling is needed for control flow signals.
    /// </summary>
    private LinqExpression CompileTryCatchFinally(TryCatchFinallyExpr expr)
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
            // Resolve exception type
            var catchType = catchClause.ExceptionTypeName != null
                ? TypeHelpers.ResolveTypeByName(catchClause.ExceptionTypeName)
                : typeof(Exception);

            // Create ParameterExpression for the caught exception (typed to the catch type)
            ParameterExpression? exParam = catchClause.VariableName != null || catchClause.WhenGuard != null
                ? LinqExpression.Parameter(catchType, catchClause.VariableName?.Lexeme ?? "ex")
                : null;

            // Compile catch body with scoped variable binding
            LinqExpression catchBody;
            if (catchClause.VariableName != null)
            {
                catchBody = Scoped(() =>
                {
                    var bodyStatements = new List<LinqExpression>
                    {
                        // Bind the catch variable in the context
                        LinqExpression.Call(_currentContext, DefineNewMethod,
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
                        LinqExpression.Call(_currentContext, DefineMethod,
                            LinqExpression.Constant(catchClause.VariableName.Value.Lexeme),
                            LinqExpression.Convert(exParam!, typeof(object))),
                        LinqExpression.Call(RequireBooleanMethod, Compile(catchClause.WhenGuard)));
                }
                else
                {
                    filterExpr = LinqExpression.Call(RequireBooleanMethod, Compile(catchClause.WhenGuard));
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

    /// <summary>
    /// Compiles parameterless throw; (rethrow) using the Expression Trees rethrow instruction.
    /// ECMA-334 section 13.10.6 -- only valid inside a catch block body.
    /// </summary>
    private static LinqExpression CompileThrowStatement()
    {
        // Expression.Rethrow generates the IL rethrow instruction.
        // Must be typed to match the try/catch return type (typeof(object)).
        return LinqExpression.Rethrow(typeof(object));
    }

    #endregion
}
