namespace Alder.Text;

/// <summary>
/// A position in source text expressed as a zero-based line and character offset.
/// </summary>
/// <param name="Line">The zero-based line number.</param>
/// <param name="Character">The zero-based character offset within the line.</param>
public readonly record struct LinePosition(int Line, int Character) : IComparable<LinePosition>
{
    /// <summary>Compares by line, then by character offset.</summary>
    public int CompareTo(LinePosition other)
    {
        var cmp = Line.CompareTo(other.Line);
        return cmp != 0 ? cmp : Character.CompareTo(other.Character);
    }

    /// <inheritdoc/>
    public override string ToString() => $"({Line},{Character})";
}
