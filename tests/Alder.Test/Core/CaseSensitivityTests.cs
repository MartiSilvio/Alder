namespace Alder.Test.Core;

[TestFixture]
public class CaseSensitivityTests
{
    [Test]
    public void CaseSensitive_ThrowsOnWrongCase()
    {
        var engine = new AlderEngine();
        engine.SetVariable("MyVar", 42);

        Assert.That(engine.Evaluate("MyVar"), Is.EqualTo(42));
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("myvar"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void CaseInsensitive_Variable()
    {
        var engine = new AlderEngine(new AlderOptions { IsCaseSensitive = false });
        engine.SetVariable("MyVar", 42);

        Assert.That(engine.Evaluate("MyVar"), Is.EqualTo(42));
        Assert.That(engine.Evaluate("myvar"), Is.EqualTo(42));
        Assert.That(engine.Evaluate("MYVAR"), Is.EqualTo(42));
    }

    [Test]
    public void CaseInsensitive_MemberAccess()
    {
        var engine = new AlderEngine(new AlderOptions { IsCaseSensitive = false });
        engine.SetVariable("obj", new TestObject { Name = "Test" });

        Assert.That(engine.Evaluate("obj.Name"), Is.EqualTo("Test"));
        Assert.That(engine.Evaluate("obj.name"), Is.EqualTo("Test"));
        Assert.That(engine.Evaluate("obj.NAME"), Is.EqualTo("Test"));
    }

    [Test]
    public void CaseInsensitive_Proxy()
    {
        var engine = new AlderEngine(new AlderOptions { IsCaseSensitive = false });

        Assert.That(engine.Evaluate("math.abs(-5)"), Is.EqualTo(5.0));
        Assert.That(engine.Evaluate("MATH.ABS(-5)"), Is.EqualTo(5.0));
    }

    private class TestObject
    {
        public string Name { get; set; } = "";
    }
}
