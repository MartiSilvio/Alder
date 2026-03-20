using CsEval.Test._Infrastructure;

namespace CsEval.Test.Extensions;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class DateArithmeticTests(CompilationMode mode)
{
    // NCalc issue #494:
    // https://github.com/ncalc/ncalc/issues/494
    [Test]
    public void NCalcIssue494_InvalidDateTimeAddition_DoesNotCrashRuntime()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended });
        engine.SetVariable("a", new DateTime(2024, 1, 1));
        engine.SetVariable("b", new DateTime(2024, 1, 2));
        var result = engine.Evaluate("a + b");
        Assert.That(result, Is.InstanceOf<IDictionary<string, object?>>());
    }

    [Test]
    public void DateArithmeticSugar_NumericUnitsAndDateOps_Work()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended });
        engine.SetVariable("date1", new DateTime(2026, 1, 31));
        engine.SetVariable("date2", new DateTime(2026, 1, 1));

        Assert.That(engine.Evaluate("30.days"), Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(engine.Evaluate("2.hours + 30.minutes"), Is.EqualTo(TimeSpan.FromMinutes(150)));
        Assert.That(engine.Evaluate("date1 - date2"), Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(engine.Evaluate("date2 + 30.days"), Is.EqualTo(new DateTime(2026, 1, 31)));
        Assert.That(engine.Evaluate("now()"), Is.TypeOf<DateTime>());
        Assert.That(engine.Evaluate("today()"), Is.TypeOf<DateTime>());
    }
}
