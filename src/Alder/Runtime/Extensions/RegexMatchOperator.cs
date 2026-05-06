using System.Text.RegularExpressions;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Runtime.Extensions;

/// <summary>
/// Implements the extended regex match operators <c>=~</c> and <c>!~</c>.
/// </summary>
internal static class RegexMatchOperator
{
    /// <summary>
    /// Returns whether the string representation of <paramref name="left"/> matches the regex pattern in <paramref name="right"/>.
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
        return Regex.IsMatch(left.ToString()!, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Returns the logical negation of <see cref="IsMatch"/>.
    /// </summary>
    public static bool IsNotMatch(object? left, object? right)
    {
        return !IsMatch(left, right);
    }
}
