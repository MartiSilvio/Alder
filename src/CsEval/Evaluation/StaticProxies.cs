namespace CsEval.Evaluation;

/// <summary>
/// Proxy for System.Math static methods
/// </summary>
public sealed class MathProxy
{
    public double Abs(double value) => Math.Abs(value);
    public double Floor(double value) => Math.Floor(value);
    public double Ceiling(double value) => Math.Ceiling(value);
    public double Round(double value) => Math.Round(value);
    public double Round(double value, int digits) => Math.Round(value, digits);
    public double Min(double a, double b) => Math.Min(a, b);
    public double Max(double a, double b) => Math.Max(a, b);
    public double Pow(double x, double y) => Math.Pow(x, y);
    public double Sqrt(double value) => Math.Sqrt(value);
    public double Sin(double value) => Math.Sin(value);
    public double Cos(double value) => Math.Cos(value);
    public double Tan(double value) => Math.Tan(value);
    public double Log(double value) => Math.Log(value);
    public double Log10(double value) => Math.Log10(value);
    public double Exp(double value) => Math.Exp(value);
    public double PI => Math.PI;
    public double E => Math.E;
}

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
    public bool TryParse(string s, out DateTime result) => DateTime.TryParse(s, out result);
}

/// <summary>
/// Proxy for System.Guid static members
/// </summary>
public sealed class GuidProxy
{
    public Guid NewGuid() => Guid.NewGuid();
    public Guid Empty => Guid.Empty;
    public Guid Parse(string s) => Guid.Parse(s);
    public bool TryParse(string s, out Guid result) => Guid.TryParse(s, out result);
}

/// <summary>
/// Proxy for System.Convert static methods
/// </summary>
public sealed class ConvertProxy
{
    public int ToInt32(object? value) => Convert.ToInt32(value);
    public long ToInt64(object? value) => Convert.ToInt64(value);
    public double ToDouble(object? value) => Convert.ToDouble(value);
    public bool ToBoolean(object? value) => Convert.ToBoolean(value);
    public string ToString(object? value) => Convert.ToString(value) ?? "";
    public decimal ToDecimal(object? value) => Convert.ToDecimal(value);
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
    public IEnumerable<T> Empty<T>() => [];
}