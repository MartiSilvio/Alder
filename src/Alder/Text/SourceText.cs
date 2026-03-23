namespace Alder.Text;

/// <summary>
/// Wraps source text and provides lazy offset-to-line/column conversion.
/// Line start offsets are computed once on first access via binary search.
/// </summary>
public sealed class SourceText
{
    private readonly string _text;
    private int[]? _lineStarts;

    /// <summary>
    /// Creates a new <see cref="SourceText"/> wrapping the specified string.
    /// </summary>
    /// <param name="text">The source text.</param>
    public SourceText(string text) => _text = text;

    /// <summary>
    /// Gets the underlying source string.
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// Gets the length of the source text in characters.
    /// </summary>
    public int Length => _text.Length;

    private int[] LineStarts => _lineStarts ??= ComputeLineStarts();

    /// <summary>
    /// Converts an absolute character offset to a zero-based line position.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The corresponding <see cref="LinePosition"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative or beyond the text length.</exception>
    public LinePosition GetLinePosition(int offset)
    {
        if (offset < 0 || offset > _text.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        var lineStarts = LineStarts;
        var lineIndex = Array.BinarySearch(lineStarts, offset);

        if (lineIndex < 0)
            lineIndex = ~lineIndex - 1;

        return new LinePosition(lineIndex, offset - lineStarts[lineIndex]);
    }

    /// <summary>
    /// Converts a <see cref="TextSpan"/> to start and end line positions.
    /// </summary>
    /// <param name="span">The text span to convert.</param>
    /// <returns>A tuple of the start and end <see cref="LinePosition"/> values.</returns>
    public (LinePosition Start, LinePosition End) GetLinePositionSpan(TextSpan span)
    {
        return (GetLinePosition(span.Start), GetLinePosition(span.End));
    }

    private int[] ComputeLineStarts()
    {
        var starts = new List<int> { 0 };

        for (var i = 0; i < _text.Length; i++)
        {
            var c = _text[i];
            if (c == '\r')
            {
                if (i + 1 < _text.Length && _text[i + 1] == '\n')
                    i++;
                starts.Add(i + 1);
            }
            else if (c == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }
}
