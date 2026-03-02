using System.Text.RegularExpressions;

namespace CsEval.Runtime.Extensions;

/// <summary>
/// Regex match operators: =~ (match) and !~ (negated match).
/// Inspired by Ruby/Perl regex match syntax.
/// </summary>
public static class RegexMatchOperator
{
    /// <summary>
    /// Returns true if the string representation of <paramref name="left"/> matches the regex
    /// <paramref name="right"/>. Right operand must be a string pattern.
    /// </summary>
    public static bool IsMatch(object? left, object? right)
    {
        var str = left?.ToString()
            ?? throw new CsEvalException("Left operand of =~ cannot be null");
        var pattern = right as string
            ?? throw new CsEvalException("Right operand of =~ must be a string pattern");
        return Regex.IsMatch(str, pattern);
    }

    /// <summary>
    /// Returns the negation of <see cref="IsMatch"/>: true if <paramref name="left"/> does NOT
    /// match the regex <paramref name="right"/>.
    /// </summary>
    public static bool IsNotMatch(object? left, object? right)
    {
        return !IsMatch(left, right);
    }
}
