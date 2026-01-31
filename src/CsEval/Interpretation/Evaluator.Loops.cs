using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

public sealed partial class Evaluator
{
    public object? VisitBreak(BreakExpr expr)
    {
        throw new BreakException();
    }

    public object? VisitContinue(ContinueExpr expr)
    {
        throw new ContinueException();
    }

    public object? VisitWhile(WhileStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            // Create a child scope for each iteration's body
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
            finally
            {
                _context = previousContext;
            }
        }

        return null;
    }

    public object? VisitFor(ForStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        // Create a scope for the entire for loop (initializer variable lives here)
        var loopContext = _context;
        _context = _context.CreateChild();

        try
        {
            // Execute initializer (if present) - in the loop scope
            if (expr.Initializer != null)
            {
                Evaluate(expr.Initializer);
            }

            // Loop while condition is true (or forever if no condition)
            while (expr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(expr.Condition)))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (maxIterations > 0 && ++_iterationCount > maxIterations)
                    throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

                // Create a child scope for each iteration's body
                var iterationContext = _context;
                _context = _context.CreateChild();

                try
                {
                    foreach (var stmt in expr.Body)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        Evaluate(stmt);
                    }
                }
                catch (BreakException)
                {
                    break;
                }
                catch (ContinueException)
                {
                    // Continue skips to increment
                }
                finally
                {
                    _context = iterationContext;
                }

                // Execute increment (if present) - in the loop scope, not iteration scope
                if (expr.Increment != null)
                {
                    Evaluate(expr.Increment);
                }
            }
        }
        finally
        {
            _context = loopContext;
        }

        return null;
    }

    public object? VisitDoWhile(DoWhileStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        do
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            // Create a child scope for each iteration's body
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
            finally
            {
                _context = previousContext;
            }
        } while (TypeHelpers.RequireBoolean(Evaluate(expr.Condition)));

        return null;
    }

    public object? VisitForEach(ForEachStatementExpr expr)
    {
        var maxIterations = _options.MaxIterations;

        var collection = Evaluate(expr.Collection);

        if (collection is not IEnumerable enumerable)
        {
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");
        }

        foreach (var item in enumerable)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (maxIterations > 0 && ++_iterationCount > maxIterations)
                throw new CsEvalException($"Loop exceeded maximum iterations ({maxIterations}). Possible infinite loop.");

            // Create a child scope for each iteration (loop variable + body variables are scoped here)
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                // Set the loop variable for this iteration (in the child scope)
                _context.Define(expr.VariableName.Lexeme, item);

                foreach (var stmt in expr.Body)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
            finally
            {
                _context = previousContext;
            }
        }

        return null;
    }
}
