using CsEval.Parsing;

namespace CsEval.Evaluation;

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
                if ((bool)RuntimeHelpers.Equals(switchValue, caseValue, _options))
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
    /// </summary>
    private bool ExecuteCaseStatements(List<SwitchCaseExpr> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];

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
                return true; // break exits the switch
            }

            // If we reach here without break, fall through to next case
            // (if there are statements in the next case)
            if (i + 1 < cases.Count && cases[i + 1].Statements.Count == 0)
            {
                // Empty case, continue to next
                continue;
            }
        }

        return false;
    }
}
