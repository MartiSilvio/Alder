// ReSharper disable UnusedMember.Global

namespace CsEval.Modules;

/// <summary>
/// Proxy for System.DateTime static members
/// </summary>
public sealed class DateTimeProxy
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
    public DateTime MinValue => DateTime.MinValue;
    public DateTime MaxValue => DateTime.MaxValue;
    public DateTime Parse(string s) => DateTime.Parse(s);
    public DateTime? TryParse(string s) => DateTime.TryParse(s, out var result) ? result : null;
}

/// <summary>
/// Proxy for System.Guid static members
/// </summary>
public sealed class GuidProxy
{
    public Guid NewGuid() => Guid.NewGuid();
    public Guid Empty => Guid.Empty;
    public Guid Parse(string s) => Guid.Parse(s);
    public Guid? TryParse(string s) => Guid.TryParse(s, out var result) ? result : null;
}

/// <summary>
/// Proxy for System.String static methods
/// </summary>
public sealed class StringProxy
{
    public string Empty => string.Empty;
    public bool IsNullOrEmpty(string? value) => string.IsNullOrEmpty(value);
    public bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);
    public string Join(string separator, IEnumerable<object?> values) =>
        string.Join(separator, values.Select(v => v?.ToString() ?? ""));
    public string Concat(params object?[] values) =>
        string.Concat(values.Select(v => v?.ToString() ?? ""));
    public string Format(string format, params object?[] args) =>
        string.Format(format, args);
}

/// <summary>
/// Proxy for System.Linq.Enumerable static methods
/// </summary>
public sealed class EnumerableProxy
{
    public IEnumerable<int> Range(int start, int count) => Enumerable.Range(start, count);
    public IEnumerable<T> Repeat<T>(T element, int count) => Enumerable.Repeat(element, count);
    public IEnumerable<object?> Empty() => [];
}