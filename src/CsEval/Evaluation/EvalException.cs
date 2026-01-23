namespace CsEval.Evaluation;

public class EvalException(string message) : Exception(message);

/// <summary>
/// Used for early returns in block expressions.
/// </summary>
internal class ReturnValue(object? value) : Exception
{
    public object? Value { get; } = value;
}

/// <summary>
/// Used for break statements in loops.
/// </summary>
internal class BreakException : Exception;

/// <summary>
/// Used for continue statements in loops.
/// </summary>
internal class ContinueException : Exception;