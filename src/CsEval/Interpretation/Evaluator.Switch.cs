using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

public sealed partial class Evaluator
{
    public object? VisitSwitch(SwitchStatementExpr expr)
    {
        var switchValue = Evaluate(expr.Expression);
        var matched = false;
        var defaultCaseIndex = -1;

        // First, find if there's a default case and look for a matching case
        for (var i = 0; i < expr.Cases.Count; i++)
        {
            var switchCase = expr.Cases[i];

            if (switchCase.Pattern == null)
            {
                // This is the default case
                defaultCaseIndex = i;
                continue;
            }

            if (!matched)
            {
                var caseValue = Evaluate(switchCase.Pattern);
                if ((bool)Operators.Equals(switchValue, caseValue, _options))
                {
                    matched = true;
                    // Execute this case and potentially fall through
                    if (ExecuteCaseStatements(expr.Cases, i))
                        return null; // break was hit
                }
            }
        }

        // If no case matched and there's a default case, execute it
        if (!matched && defaultCaseIndex >= 0)
        {
            ExecuteCaseStatements(expr.Cases, defaultCaseIndex);
        }

        return null;
    }

    /// <summary>
    /// Executes case statements starting from the given index, supporting fall-through.
    /// Returns true if a break was encountered.
    /// C# semantics: only empty cases fall through; non-empty cases implicitly break.
    /// </summary>
    private bool ExecuteCaseStatements(List<SwitchCaseExpr> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];

            // Empty case: fall through to next
            if (switchCase.Statements.Count == 0)
                continue;

            try
            {
                foreach (var stmt in switchCase.Statements)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    Evaluate(stmt);
                }
            }
            catch (BreakException)
            {
                return true; // explicit break exits the switch
            }

            // Non-empty case without explicit break: implicit break (C# behavior)
            return false;
        }

        return false;
    }
}
