namespace Alder.Text;

/// <summary>
/// Wraps source text and provides lazy offset-to-line/column conversion via binary search.
/// </summary>
public sealed class SourceText
{
    private int[]? _lineStarts;

    /// <param name="text">The source text.</param>
    public SourceText(string text) => Text = text;

    /// <summary>The underlying source string.</summary>
    public string Text { get; }

    /// <summary>The length of the source text in characters.</summary>
    public int Length => Text.Length;

    private int[] LineStarts => _lineStarts ??= ComputeLineStarts();

    /// <summary>Converts an absolute character offset to a zero-based line position.</summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The corresponding <see cref="LinePosition"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative or beyond the text length.</exception>
    public LinePosition GetLinePosition(int offset)
    {
        if (offset < 0 || offset > Text.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        var lineStarts = LineStarts;
        var lineIndex = Array.BinarySearch(lineStarts, offset);

        if (lineIndex < 0)
            lineIndex = ~lineIndex - 1;

        return new LinePosition(lineIndex, offset - lineStarts[lineIndex]);
    }

    /// <summary>Converts a <see cref="TextSpan"/> to start and end line positions.</summary>
    public (LinePosition Start, LinePosition End) GetLinePositionSpan(TextSpan span)
    {
        return (GetLinePosition(span.Start), GetLinePosition(span.End));
    }

    private int[] ComputeLineStarts()
    {
        var starts = new List<int> { 0 };

        for (var i = 0; i < Text.Length; i++)
        {
            var c = Text[i];
            if (c == '\r')
            {
                if (i + 1 < Text.Length && Text[i + 1] == '\n')
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
