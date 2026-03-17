using System.Collections;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase06Plan02VerificationTests(CompilationMode mode)
{
    private CsEvalEngine ExtendedEngine()
        => TestEngineFactory.Create(mode, new CsEvalOptions { LanguageMode = LanguageMode.Extended });

    private CsEvalEngine ExtendedEngine(CsEvalOptions options)
        => TestEngineFactory.Create(mode, options with { LanguageMode = LanguageMode.Extended });

    private CsEvalEngine StandardEngine()
        => TestEngineFactory.Create(mode);

    // ═══════════════════════════════════════════════════════════════
    // EXT-03: Syntax Sugar
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Unless_FalseCondition_ExecutesBody()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("var x = 0; unless (false) { x = 1; } x"), Is.EqualTo(1));
    }

    [Test]
    public void Unless_TrueCondition_SkipsBody()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("var x = 0; unless (true) { x = 1; } x"), Is.EqualTo(0));
    }

    [Test]
    public void Until_CountsToFive()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("var i = 0; until (i >= 5) { i++; } i"), Is.EqualTo(5));
    }

    [Test]
    public void LetIn_Simple_ScopedBinding()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("let x = 5 in x * x"), Is.EqualTo(25));
    }

    [Test]
    public void LetIn_Destructuring()
    {
        var engine = ExtendedEngine();
        engine.SetVariable("person", new { Name = "Alice", Age = 30 });
        Assert.That(engine.Evaluate("let {Name, Age} = person in Name"), Is.EqualTo("Alice"));
    }

    [Test]
    public void Comprehension_Basic()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("[x * x for x in new[] {1, 2, 3}]");
        Assert.That(result, Is.InstanceOf<IEnumerable>());
        Assert.That(((IEnumerable)result!).Cast<object>().Select(Convert.ToInt32), Is.EqualTo(new[] { 1, 4, 9 }));
    }

    [Test]
    public void Comprehension_Filtered()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("[x for x in new[] {1, 2, 3, 4, 5} if x > 3]");
        Assert.That(result, Is.InstanceOf<IEnumerable>());
        Assert.That(((IEnumerable)result!).Cast<object>().Select(Convert.ToInt32), Is.EqualTo(new[] { 4, 5 }));
    }

    [Test]
    public void ArrayLiteral_CreatesArray()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.InstanceOf<Array>());
        Assert.That(((IEnumerable)result!).Cast<object>().Select(Convert.ToInt32), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ArrayLiteral_Empty()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("[]");
        Assert.That(result, Is.InstanceOf<object[]>());
        Assert.That(((object[])result!).Length, Is.EqualTo(0));
    }

    [Test]
    public void Spread_Arrays()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("var a = new[] {1, 2}; var b = new[] {3, 4}; [..a, ..b]");
        Assert.That(result, Is.InstanceOf<Array>());
        Assert.That(((IEnumerable)result!).Cast<object>().Select(Convert.ToInt32), Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void IfExpression_TrueCondition()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("if (true) 1 else 2"), Is.EqualTo(1));
    }

    [Test]
    public void IfExpression_FalseCondition()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("if (false) 1 else 2"), Is.EqualTo(2));
    }

    [Test]
    public void StringRepetition_StringTimesInt()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("\"ab\" * 3"), Is.EqualTo("ababab"));
    }

    [Test]
    public void StringRepetition_IntTimesString()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("3 * \"ab\""), Is.EqualTo("ababab"));
    }

    [Test]
    public void Slicing_BasicRange()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3, 4, 5}[1:3]");
        Assert.That(result, Is.InstanceOf<int[]>());
        Assert.That(result, Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void Slicing_FromBeginning()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3, 4, 5}[:3]");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Slicing_ToEnd()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3, 4, 5}[2:]");
        Assert.That(result, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void Slicing_WithStep()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3, 4, 5}[::2]");
        Assert.That(result, Is.EqualTo(new[] { 1, 3, 5 }));
    }

    [Test]
    public void ArrayLiteral_StandardMode_Throws()
    {
        var engine = StandardEngine();
        Assert.That(() => engine.Evaluate("[1, 2, 3]"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-04: Built-in Functions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Math_Sin_Zero()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("sin(0)"), Is.EqualTo(0.0));
    }

    [Test]
    public void Math_Cos_Zero()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("cos(0)"), Is.EqualTo(1.0));
    }

    [Test]
    public void Math_Sqrt_Four()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("sqrt(4)"), Is.EqualTo(2.0));
    }

    [Test]
    public void Math_Abs_NegativeInt_PreservesType()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("abs(-5)");
        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.InstanceOf<int>());
    }

    [Test]
    public void Math_Round_TwoArgs()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("round(3.14159, 2)"), Is.EqualTo(3.14));
    }

    [Test]
    public void Math_Log_TwoArgs()
    {
        var engine = ExtendedEngine();
        Assert.That((double)engine.Evaluate("log(8, 2)")!, Is.EqualTo(3.0).Within(0.0001));
    }

    [Test]
    public void Math_Clamp()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("clamp(15, 0, 10)"), Is.EqualTo(10));
    }

    [Test]
    public void Math_Min_TwoArgs()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("min(3, 5)"), Is.EqualTo(3));
    }

    [Test]
    public void Math_Max_TwoArgs()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("max(3, 5)"), Is.EqualTo(5));
    }

    [Test]
    public void Constant_Pi()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("pi"), Is.EqualTo(Math.PI));
    }

    [Test]
    public void Constant_E()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("e"), Is.EqualTo(Math.E));
    }

    [Test]
    public void Constant_Tau()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("tau"), Is.EqualTo(Math.PI * 2.0));
    }

    [Test]
    public void Constant_Infinity()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("infinity"), Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void Constant_NaN()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("nan"), Is.EqualTo(double.NaN));
    }

    [Test]
    public void Date_Now_ReturnsDateTime()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("now()");
        Assert.That(result, Is.InstanceOf<DateTime>());
    }

    [Test]
    public void Date_Today_ReturnsMidnight()
    {
        var engine = ExtendedEngine();
        var result = (DateTime)engine.Evaluate("today()")!;
        Assert.That(result.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void DateSugar_Days()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("5.days"), Is.EqualTo(TimeSpan.FromDays(5)));
    }

    [Test]
    public void DateSugar_Hours()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("2.hours"), Is.EqualTo(TimeSpan.FromHours(2)));
    }

    [Test]
    public void DateSugar_Minutes()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("30.minutes"), Is.EqualTo(TimeSpan.FromMinutes(30)));
    }

    [Test]
    public void DateSugar_Seconds()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("10.seconds"), Is.EqualTo(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public void DateSugar_Milliseconds()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("500.milliseconds"), Is.EqualTo(TimeSpan.FromMilliseconds(500)));
    }

    [Test]
    public void DateSugar_Weeks()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("1.weeks"), Is.EqualTo(TimeSpan.FromDays(7)));
    }

    [Test]
    public void Aggregate_Sum()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("sum(new[] {1, 2, 3})"), Is.EqualTo(6));
    }

    [Test]
    public void Aggregate_Avg()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("avg(new[] {1.0, 2.0, 3.0})"), Is.EqualTo(2.0));
    }

    [Test]
    public void Aggregate_Count()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("count(new[] {1, 2, 3})"), Is.EqualTo(3));
    }

    [Test]
    public void Aggregate_Min()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("min(new[] {3, 1, 2})"), Is.EqualTo(1));
    }

    [Test]
    public void Aggregate_Max()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("max(new[] {3, 1, 2})"), Is.EqualTo(3));
    }

    [Test]
    public void ItPlaceholder_Where()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3, 4, 5}.Where(it > 3).ToArray()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5 }));
    }

    [Test]
    public void ItPlaceholder_Select()
    {
        var engine = ExtendedEngine();
        var result = engine.Evaluate("new[] {1, 2, 3}.Select(it * 10).ToArray()");
        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30 }));
    }

    [Test]
    public void ItPlaceholder_Any()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("new[] {1, 2, 3}.Any(it > 2)"), Is.EqualTo(true));
    }

    [Test]
    public void ItPlaceholder_All()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("new[] {1, 2, 3}.All(it > 0)"), Is.EqualTo(true));
    }

    [Test]
    public void Shadowing_UserVariable_OverridesPi()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("var pi = 3; pi"), Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // EXT-05: Negative Indexing
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NegativeIndex_LastElement()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("new[] {1, 2, 3}[-1]"), Is.EqualTo(3));
    }

    [Test]
    public void NegativeIndex_SecondToLast()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("new[] {1, 2, 3}[-2]"), Is.EqualTo(2));
    }

    [Test]
    public void NegativeIndex_FirstElement()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("new[] {1, 2, 3}[-3]"), Is.EqualTo(1));
    }

    [Test]
    public void NegativeIndex_OutOfBounds_Throws()
    {
        var engine = ExtendedEngine();
        Assert.That(() => engine.Evaluate("new[] {1, 2, 3}[-4]"),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NegativeIndex_String_LastChar()
    {
        var engine = ExtendedEngine();
        Assert.That(engine.Evaluate("\"hello\"[-1]"), Is.EqualTo('o'));
    }

    [Test]
    public void NegativeIndex_StandardMode_Throws()
    {
        var engine = StandardEngine();
        Assert.That(() => engine.Evaluate("new[] {1, 2, 3}[-1]"),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
