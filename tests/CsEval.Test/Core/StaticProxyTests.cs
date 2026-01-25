namespace CsEval.Test.Core;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class StaticProxyTests(CompilationMode mode) : TestBase
{
    private CsEvalEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _engine = CreateEngine(mode);
    }

    #region Math

    [Test]
    public void Math_Abs()
    {
        Assert.That(_engine.Evaluate("Math.Abs(-5)"), Is.EqualTo(5.0));
        Assert.That(_engine.Evaluate("Math.Abs(5)"), Is.EqualTo(5.0));
        Assert.That(_engine.Evaluate("Math.Abs(-3.14)"), Is.EqualTo(3.14));
    }

    [Test]
    public void Math_Floor()
    {
        Assert.That(_engine.Evaluate("Math.Floor(3.7)"), Is.EqualTo(3.0));
        Assert.That(_engine.Evaluate("Math.Floor(-3.7)"), Is.EqualTo(-4.0));
        Assert.That(_engine.Evaluate("Math.Floor(5.0)"), Is.EqualTo(5.0));
    }

    [Test]
    public void Math_Ceiling()
    {
        Assert.That(_engine.Evaluate("Math.Ceiling(3.2)"), Is.EqualTo(4.0));
        Assert.That(_engine.Evaluate("Math.Ceiling(-3.2)"), Is.EqualTo(-3.0));
        Assert.That(_engine.Evaluate("Math.Ceiling(5.0)"), Is.EqualTo(5.0));
    }

    [Test]
    public void Math_Round()
    {
        Assert.That(_engine.Evaluate("Math.Round(3.5)"), Is.EqualTo(4.0));
        Assert.That(_engine.Evaluate("Math.Round(3.4)"), Is.EqualTo(3.0));
    }

    [Test]
    public void Math_Round_WithDigits()
    {
        // Two-parameter overload
        Assert.That(_engine.Evaluate("Math.Round(3.14159, 2)"), Is.EqualTo(3.14));
        Assert.That(_engine.Evaluate("Math.Round(2.567, 1)"), Is.EqualTo(2.6));
    }

    [Test]
    public void Math_MinMax()
    {
        Assert.That(_engine.Evaluate("Math.Min(3, 7)"), Is.EqualTo(3.0));
        Assert.That(_engine.Evaluate("Math.Max(3, 7)"), Is.EqualTo(7.0));
        Assert.That(_engine.Evaluate("Math.Min(-5, -2)"), Is.EqualTo(-5.0));
    }

    [Test]
    public void Math_Pow()
    {
        Assert.That(_engine.Evaluate("Math.Pow(2, 3)"), Is.EqualTo(8.0));
        Assert.That(_engine.Evaluate("Math.Pow(10, 2)"), Is.EqualTo(100.0));
        Assert.That(_engine.Evaluate("Math.Pow(4, 0.5)"), Is.EqualTo(2.0));
    }

    [Test]
    public void Math_Sqrt()
    {
        Assert.That(_engine.Evaluate("Math.Sqrt(16)"), Is.EqualTo(4.0));
        Assert.That(_engine.Evaluate("Math.Sqrt(2)"), Is.EqualTo(Math.Sqrt(2)));
    }

    [Test]
    public void Math_Trigonometry()
    {
        Assert.That(_engine.Evaluate("Math.Sin(0)"), Is.EqualTo(0.0));
        Assert.That(_engine.Evaluate("Math.Cos(0)"), Is.EqualTo(1.0));
        Assert.That(_engine.Evaluate("Math.Tan(0)"), Is.EqualTo(0.0));
    }

    [Test]
    public void Math_Logarithms()
    {
        Assert.That(_engine.Evaluate("Math.Log(Math.E)"), Is.EqualTo(1.0).Within(0.0001));
        Assert.That(_engine.Evaluate("Math.Log10(100)"), Is.EqualTo(2.0));
        Assert.That(_engine.Evaluate("Math.Exp(1)"), Is.EqualTo(Math.E).Within(0.0001));
    }

    [Test]
    public void Math_Constants()
    {
        Assert.That(_engine.Evaluate("Math.PI"), Is.EqualTo(Math.PI));
        Assert.That(_engine.Evaluate("Math.E"), Is.EqualTo(Math.E));
    }

    #endregion

    #region DateTime

    [Test]
    public void DateTime_Now()
    {
        var before = DateTime.Now;
        var result = (DateTime)_engine.Evaluate("DateTime.Now")!;
        var after = DateTime.Now;

        Assert.That(result, Is.InRange(before, after));
    }

    [Test]
    public void DateTime_UtcNow()
    {
        var before = DateTime.UtcNow;
        var result = (DateTime)_engine.Evaluate("DateTime.UtcNow")!;
        var after = DateTime.UtcNow;

        Assert.That(result, Is.InRange(before, after));
    }

    [Test]
    public void DateTime_Today()
    {
        var result = (DateTime)_engine.Evaluate("DateTime.Today")!;
        Assert.That(result.Date, Is.EqualTo(DateTime.Today));
        Assert.That(result.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void DateTime_MinMaxValue()
    {
        Assert.That(_engine.Evaluate("DateTime.MinValue"), Is.EqualTo(DateTime.MinValue));
        Assert.That(_engine.Evaluate("DateTime.MaxValue"), Is.EqualTo(DateTime.MaxValue));
    }

    [Test]
    public void DateTime_Parse()
    {
        var result = (DateTime)_engine.Evaluate("DateTime.Parse(\"2024-01-15\")")!;
        Assert.That(result.Year, Is.EqualTo(2024));
        Assert.That(result.Month, Is.EqualTo(1));
        Assert.That(result.Day, Is.EqualTo(15));
    }

    [Test]
    public void DateTime_PropertyAccess()
    {
        _engine.SetVariable("dt", new DateTime(2024, 6, 15, 10, 30, 45));

        Assert.That(_engine.Evaluate("dt.Year"), Is.EqualTo(2024));
        Assert.That(_engine.Evaluate("dt.Month"), Is.EqualTo(6));
        Assert.That(_engine.Evaluate("dt.Day"), Is.EqualTo(15));
        Assert.That(_engine.Evaluate("dt.Hour"), Is.EqualTo(10));
        Assert.That(_engine.Evaluate("dt.Minute"), Is.EqualTo(30));
        Assert.That(_engine.Evaluate("dt.Second"), Is.EqualTo(45));
    }

    [Test]
    public void DateTime_TryParse()
    {
        var result = (DateTime?)_engine.Evaluate("DateTime.TryParse(\"2024-06-15\")");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Year, Is.EqualTo(2024));
        Assert.That(result.Value.Month, Is.EqualTo(6));
        Assert.That(result.Value.Day, Is.EqualTo(15));
    }

    [Test]
    public void DateTime_TryParse_Invalid()
    {
        var result = _engine.Evaluate("DateTime.TryParse(\"not-a-date\")");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Guid

    [Test]
    public void Guid_NewGuid()
    {
        var result1 = (Guid)_engine.Evaluate("Guid.NewGuid()")!;
        var result2 = (Guid)_engine.Evaluate("Guid.NewGuid()")!;

        Assert.That(result1, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result2, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void Guid_Empty()
    {
        Assert.That(_engine.Evaluate("Guid.Empty"), Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void Guid_Parse()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        _engine.SetVariable("guidStr", guidString);

        var result = (Guid)_engine.Evaluate("Guid.Parse(guidStr)")!;
        Assert.That(result, Is.EqualTo(Guid.Parse(guidString)));
    }

    [Test]
    public void Guid_TryParse()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        _engine.SetVariable("guidStr", guidString);

        var result = (Guid?)_engine.Evaluate("Guid.TryParse(guidStr)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(Guid.Parse(guidString)));
    }

    [Test]
    public void Guid_TryParse_Invalid()
    {
        var result = _engine.Evaluate("Guid.TryParse(\"not-a-guid\")");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Convert

    [Test]
    public void Convert_ToInt32()
    {
        Assert.That(_engine.Evaluate("Convert.ToInt32(\"42\")"), Is.EqualTo(42));
        Assert.That(_engine.Evaluate("Convert.ToInt32(3.7)"), Is.EqualTo(4));
        Assert.That(_engine.Evaluate("Convert.ToInt32(true)"), Is.EqualTo(1));
    }

    [Test]
    public void Convert_ToInt64()
    {
        Assert.That(_engine.Evaluate("Convert.ToInt64(\"9999999999\")"), Is.EqualTo(9999999999L));
        Assert.That(_engine.Evaluate("Convert.ToInt64(42)"), Is.EqualTo(42L));
    }

    [Test]
    public void Convert_ToDouble()
    {
        Assert.That(_engine.Evaluate("Convert.ToDouble(\"3.14\")"), Is.EqualTo(3.14));
        Assert.That(_engine.Evaluate("Convert.ToDouble(42)"), Is.EqualTo(42.0));
    }

    [Test]
    public void Convert_ToBoolean()
    {
        Assert.That(_engine.Evaluate("Convert.ToBoolean(1)"), Is.True);
        Assert.That(_engine.Evaluate("Convert.ToBoolean(0)"), Is.False);
        Assert.That(_engine.Evaluate("Convert.ToBoolean(\"true\")"), Is.True);
        Assert.That(_engine.Evaluate("Convert.ToBoolean(\"false\")"), Is.False);
    }

    [Test]
    public void Convert_ToString()
    {
        Assert.That(_engine.Evaluate("Convert.ToString(42)"), Is.EqualTo("42"));
        Assert.That(_engine.Evaluate("Convert.ToString(3.14)"), Is.EqualTo("3.14"));
        Assert.That(_engine.Evaluate("Convert.ToString(true)"), Is.EqualTo("True"));
    }

    [Test]
    public void Convert_ToDecimal()
    {
        Assert.That(_engine.Evaluate("Convert.ToDecimal(\"123.45\")"), Is.EqualTo(123.45m));
        Assert.That(_engine.Evaluate("Convert.ToDecimal(42)"), Is.EqualTo(42m));
    }

    #endregion

    #region String

    [Test]
    public void String_Empty()
    {
        Assert.That(_engine.Evaluate("String.Empty"), Is.EqualTo(""));
    }

    [Test]
    public void String_IsNullOrEmpty()
    {
        Assert.That(_engine.Evaluate("String.IsNullOrEmpty(\"\")"), Is.True);
        Assert.That(_engine.Evaluate("String.IsNullOrEmpty(null)"), Is.True);
        Assert.That(_engine.Evaluate("String.IsNullOrEmpty(\"hello\")"), Is.False);
    }

    [Test]
    public void String_IsNullOrWhiteSpace()
    {
        Assert.That(_engine.Evaluate("String.IsNullOrWhiteSpace(\"\")"), Is.True);
        Assert.That(_engine.Evaluate("String.IsNullOrWhiteSpace(\"   \")"), Is.True);
        Assert.That(_engine.Evaluate("String.IsNullOrWhiteSpace(null)"), Is.True);
        Assert.That(_engine.Evaluate("String.IsNullOrWhiteSpace(\"hello\")"), Is.False);
    }

    [Test]
    public void String_Join()
    {
        _engine.SetVariable("items", new List<string> { "a", "b", "c" });
        Assert.That(_engine.Evaluate("String.Join(\", \", items)"), Is.EqualTo("a, b, c"));
        Assert.That(_engine.Evaluate("String.Join(\"-\", items)"), Is.EqualTo("a-b-c"));
    }

    [Test]
    public void String_Concat()
    {
        Assert.That(_engine.Evaluate("String.Concat(\"hello\", \" \", \"world\")"), Is.EqualTo("hello world"));
        Assert.That(_engine.Evaluate("String.Concat(1, 2, 3)"), Is.EqualTo("123"));
    }

    [Test]
    public void String_Format()
    {
        Assert.That(_engine.Evaluate("String.Format(\"Hello, {0}!\", \"World\")"), Is.EqualTo("Hello, World!"));
        Assert.That(_engine.Evaluate("String.Format(\"{0} + {1} = {2}\", 1, 2, 3)"), Is.EqualTo("1 + 2 = 3"));
    }

    #endregion

    #region Enumerable

    [Test]
    public void Enumerable_Range()
    {
        var result = _engine.Evaluate("Enumerable.Range(1, 5).ToList()") as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Enumerable_Range_WithOperations()
    {
        Assert.That(_engine.Evaluate("Enumerable.Range(1, 5).Sum()"), Is.EqualTo(15));
        Assert.That(_engine.Evaluate("Enumerable.Range(0, 10).Where(x => x % 2 == 0).Count()"), Is.EqualTo(5));
    }

    [Test]
    public void Enumerable_Repeat()
    {
        var result = _engine.Evaluate("Enumerable.Repeat(\"x\", 3).ToList()") as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "x", "x", "x" }));
    }

    [Test]
    public void Enumerable_Repeat_WithSum()
    {
        Assert.That(_engine.Evaluate("Enumerable.Repeat(5, 4).Sum()"), Is.EqualTo(20));
    }

    [Test]
    public void Enumerable_Empty()
    {
        var result = _engine.Evaluate("Enumerable.Empty().ToList()") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Chained Operations

    [Test]
    public void Math_Chained()
    {
        Assert.That(_engine.Evaluate("Math.Round(Math.Sqrt(Math.Pow(3, 2) + Math.Pow(4, 2)))"), Is.EqualTo(5.0));
    }

    [Test]
    public void DateTime_MethodChaining()
    {
        _engine.SetVariable("dt", new DateTime(2024, 1, 15));
        Assert.That(_engine.Evaluate("dt.AddDays(10).Day"), Is.EqualTo(25));
        Assert.That(_engine.Evaluate("dt.AddMonths(2).Month"), Is.EqualTo(3));
    }

    [Test]
    public void String_WithLinq()
    {
        _engine.SetVariable("words", new List<string> { "hello", "world", "test" });
        Assert.That(
            _engine.Evaluate("String.Join(\", \", words.Where(w => w.Length > 4))"),
            Is.EqualTo("hello, world"));
    }

    [Test]
    public void Enumerable_Complex()
    {
        var result = _engine.Evaluate(@"
            Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => x * x)
                .Sum()");
        Assert.That(result, Is.EqualTo(4 + 16 + 36 + 64 + 100)); // 220
    }

    #endregion
}
