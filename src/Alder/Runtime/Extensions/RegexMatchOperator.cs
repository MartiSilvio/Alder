using System.Text.RegularExpressions;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Regex match operators: =~ (match) and !~ (negated match).
/// Inspired by Ruby/Perl regex match syntax.
/// </summary>
internal static class RegexMatchOperator
{
    /// <summary>
    /// Returns true if the string representation of <paramref name="left"/> matches the regex
    /// <paramref name="right"/>. Right operand must be a string pattern.
    /// </summary>
    public static bool IsMatch(object? left, object? right)
    {
        if (left is null)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.EqualTilde),
                TypeNameFormatter.Null,
                TypeNameFormatter.Of(right));
        if (right is not string pattern)
            throw new AlderException(
                DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.EqualTilde),
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        return Regex.IsMatch(left.ToString()!, pattern);
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
