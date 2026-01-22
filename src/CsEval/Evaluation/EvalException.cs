namespace CsEval.Evaluation
{
    public class EvalException(string message) : Exception(message);

    /// <summary>
    /// Used for early returns in block expressions.
    /// </summary>
    internal class ReturnValue(object? value) : Exception
    {
        public object? Value { get; } = value;
    }
}
