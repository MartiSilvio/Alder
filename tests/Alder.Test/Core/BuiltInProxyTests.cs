namespace Alder.Test.Core;

[TestFixture]
public class BuiltInProxyTests
{
    [Test]
    public void MathProxy()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void DateTimeProxy()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("DateTime.Now");
        Assert.That(result, Is.InstanceOf<DateTime>());
    }

    [Test]
    public void GuidProxy()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("Guid.NewGuid()");
        Assert.That(result, Is.InstanceOf<Guid>());
    }
}
