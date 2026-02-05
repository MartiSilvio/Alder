using System.Linq.Expressions;
using CsEval.Parsing;

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

        var loopStatements = new List<LinqExpression>();

        loopStatements.Add(CompileCancellationCheck());

        // Condition check - break if false
        loopStatements.Add(LinqExpression.IfThen(
            LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, Compile(whileStmt.Condition))),
            LinqExpression.Break(breakLabel)));

        loopStatements.Add(CompileIterationCheck());

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

            loopStatements.Add(CompileCancellationCheck());

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
            LinqExpression.Not(LinqExpression.Call(RequireBooleanMethod, Compile(doWhile.Condition))),
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

            var loopStatements = new List<LinqExpression>();

            loopStatements.Add(CompileCancellationCheck());

            // MoveNext - break if false
            loopStatements.Add(LinqExpression.IfThen(
                LinqExpression.Not(LinqExpression.Call(enumerator, MoveNextMethod)),
                LinqExpression.Break(breakLabel)));

            loopStatements.Add(CompileIterationCheck());

            // Get Current value
            loopStatements.Add(LinqExpression.Assign(
                itemValue,
                LinqExpression.Property(enumerator, nameof(IEnumerator.Current))));

            // C# 5+ behavior: create a fresh scope for EACH iteration
            // This ensures lambdas capture a per-iteration variable, not a shared one
            loopStatements.Add(Scoped(() =>
            {
                var iterStatements = new List<LinqExpression>();

                // Define loop variable in this per-iteration scope (with shadowing check)
                iterStatements.Add(LinqExpression.Call(_currentContext, DefineNewMethod,
                    LinqExpression.Constant(forEach.VariableName.Lexeme), itemValue,
                    LinqExpression.Constant(typeof(object), typeof(Type))));

                // Body statements in the same per-iteration scope
                foreach (var stmt in forEach.Body)
                {
                    iterStatements.Add(CompileCancellationCheck());
                    iterStatements.Add(Compile(stmt));
                }

                return LinqExpression.Block(iterStatements);
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

    #endregion
}
