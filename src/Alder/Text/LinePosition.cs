namespace Alder.Text;

/// <summary>
/// A position in source text expressed as a zero-based line and character offset.
/// </summary>
/// <param name="Line">The zero-based line number.</param>
/// <param name="Character">The zero-based character offset within the line.</param>
public readonly record struct LinePosition(int Line, int Character) : IComparable<LinePosition>
{
    /// <summary>
    /// Compares this position to another by line, then by character offset.
    /// </summary>
    /// <param name="other">The position to compare to.</param>
    /// <returns>A negative value if this position precedes <paramref name="other"/>; zero if equal; a positive value if it follows.</returns>
    public int CompareTo(LinePosition other)
    {
        var cmp = Line.CompareTo(other.Line);
        return cmp != 0 ? cmp : Character.CompareTo(other.Character);
    }

    /// <inheritdoc/>
    public override string ToString() => $"({Line},{Character})";
}
