namespace CsEval.Text;

/// <summary>
/// A position in source text expressed as a zero-based line and character offset.
/// </summary>
public readonly record struct LinePosition(int Line, int Character) : IComparable<LinePosition>
{
    public int CompareTo(LinePosition other)
    {
        var cmp = Line.CompareTo(other.Line);
        return cmp != 0 ? cmp : Character.CompareTo(other.Character);
    }

    public override string ToString() => $"({Line},{Character})";
}
