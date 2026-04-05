namespace Alder.Test.Core;

[TestFixture]
public class CustomRegistrationTests
{
    [Test]
    public void CustomFunction()
    {
        var engine = new AlderEngine(o => o.Functions.Register("twice", args => Convert.ToInt64(args[0]) * 2));

        var result = engine.Evaluate("twice(5)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void CustomProxy()
    {
        var engine = new AlderEngine(o => o.Modules.Register<GreetingProxy>("Custom", instance: new GreetingProxy()));

        var result = engine.Evaluate("""Custom.Greet("World") """);
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    private class GreetingProxy
    {
        public string Greet(string name) => $"Hello, {name}!";
    }
}
