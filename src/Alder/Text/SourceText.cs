namespace Alder.Text;

/// <summary>
/// Wraps source text and provides lazy offset-to-line/column conversion.
/// Line start offsets are computed once on first access via binary search.
/// </summary>
public sealed class SourceText
{
    private readonly string _text;
    private int[]? _lineStarts;

    public SourceText(string text) => _text = text;

    public string Text => _text;
    public int Length => _text.Length;

    private int[] LineStarts => _lineStarts ??= ComputeLineStarts();

    /// <summary>
    /// Converts an absolute character offset to a zero-based line position.
    /// </summary>
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
