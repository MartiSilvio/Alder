namespace Alder.Text;

/// <summary>
/// Immutable span representing a range of characters in source text.
/// Offset-based (no line/column) — use <see cref="SourceText"/> to resolve line positions lazily.
/// </summary>
/// <param name="Start">The zero-based start offset in the source text.</param>
/// <param name="Length">The number of characters in the span.</param>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>
    /// Gets the exclusive end offset (<see cref="Start"/> + <see cref="Length"/>).
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Gets whether this span is empty (length is zero).
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Returns whether the specified position falls within this span.
    /// </summary>
    /// <param name="position">The zero-based character offset to test.</param>
    /// <returns><c>true</c> if the position is within [<see cref="Start"/>, <see cref="End"/>).</returns>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>
    /// Returns whether the specified span is entirely contained within this span.
    /// </summary>
    /// <param name="span">The span to test.</param>
    /// <returns><c>true</c> if <paramref name="span"/> is fully contained within this span.</returns>
    public bool Contains(TextSpan span) => span.Start >= Start && span.End <= End;

    /// <summary>
    /// Returns whether this span overlaps with the specified span.
    /// </summary>
    /// <param name="span">The span to test.</param>
    /// <returns><c>true</c> if the spans share at least one character position.</returns>
    public bool OverlapsWith(TextSpan span) => Start < span.End && span.Start < End;

    /// <summary>
    /// Creates a <see cref="TextSpan"/> from inclusive start and exclusive end offsets.
    /// </summary>
    /// <param name="start">The inclusive start offset.</param>
    /// <param name="end">The exclusive end offset.</param>
    /// <returns>A new <see cref="TextSpan"/>.</returns>
    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    /// <inheritdoc/>
    public override string ToString() => $"[{Start}..{End})";
}
