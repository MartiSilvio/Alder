namespace Alder.Text;

/// <summary>
/// Immutable span representing a range of characters in source text.
/// Offset-based (no line/column); use <see cref="SourceText"/> to resolve line positions lazily.
/// </summary>
/// <param name="Start">The zero-based start offset in the source text.</param>
/// <param name="Length">The number of characters in the span.</param>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>The exclusive end offset (<see cref="Start"/> + <see cref="Length"/>).</summary>
    public int End => Start + Length;

    /// <summary>Whether this span is empty (length is zero).</summary>
    public bool IsEmpty => Length == 0;

    /// <summary>Returns whether the specified position falls within this span.</summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>Returns whether the specified span is entirely contained within this span.</summary>
    public bool Contains(TextSpan span) => span.Start >= Start && span.End <= End;

    /// <summary>Returns whether this span overlaps with the specified span.</summary>
    public bool OverlapsWith(TextSpan span) => Start < span.End && span.Start < End;

    /// <summary>Creates a <see cref="TextSpan"/> from inclusive start and exclusive end offsets.</summary>
    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    /// <inheritdoc/>
    public override string ToString() => $"[{Start}..{End})";
}
