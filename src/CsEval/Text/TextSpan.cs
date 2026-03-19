namespace CsEval.Text;

/// <summary>
/// Immutable span representing a range of characters in source text.
/// Offset-based (no line/column) — use <see cref="SourceText"/> to resolve line positions lazily.
/// </summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;
    public bool IsEmpty => Length == 0;

    public bool Contains(int position) => position >= Start && position < End;
    public bool Contains(TextSpan span) => span.Start >= Start && span.End <= End;
    public bool OverlapsWith(TextSpan span) => Start < span.End && span.Start < End;

    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    public override string ToString() => $"[{Start}..{End})";
}
